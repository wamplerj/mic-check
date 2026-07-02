import { describe, it, expect, beforeEach, afterEach, jest } from '@jest/globals';
import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';
import apiClient, { setAuthTokens, clearAuthTokens, getAccessToken } from '@/api/client';
import type { TokenResponse } from '@/types/api';

// The response interceptor drives its retry/refresh flow entirely through the
// apiClient axios instance, so we replace its transport (`adapter`) with a
// fake one we control. This exercises the real interceptor chain end-to-end
// (request auth header, 401 detection, refresh, retry, queueing) rather than
// just the token-storage helpers. The refresh call itself goes through the
// raw `axios.post` (not the apiClient instance, to avoid interceptor
// recursion), so that's mocked separately via jest.spyOn.

function unauthorizedError(config: InternalAxiosRequestConfig): AxiosError {
  return new AxiosError(
    'Request failed with status code 401',
    'ERR_BAD_REQUEST',
    config,
    undefined,
    { status: 401, statusText: 'Unauthorized', data: {}, headers: {}, config },
  );
}

function serverError(config: InternalAxiosRequestConfig): AxiosError {
  return new AxiosError(
    'Request failed with status code 500',
    'ERR_BAD_RESPONSE',
    config,
    undefined,
    { status: 500, statusText: 'Internal Server Error', data: {}, headers: {}, config },
  );
}

const tokenResponse: TokenResponse = {
  accessToken: 'new-access-token',
  refreshToken: 'new-refresh-token',
  expiresAt: '2026-01-01T01:00:00Z',
};

describe('ApiClient', () => {
  let originalLocation: Location;

  beforeEach(() => {
    clearAuthTokens();
    localStorage.clear();
    jest.restoreAllMocks();

    originalLocation = window.location;
    // jsdom's window.location.href setter throws "not implemented" - stub it
    // out so the redirect-on-refresh-failure path can be asserted on.
    delete (window as any).location;
    window.location = { ...originalLocation, href: '' } as Location;
  });

  afterEach(() => {
    clearAuthTokens();
    window.location = originalLocation;
  });

  describe('Token management', () => {
    describe('WhenTokensAreSet', () => {
      it('ThenGetAccessTokenReturnsTheAccessToken', () => {
        setAuthTokens('access-abc', 'refresh-xyz');

        expect(getAccessToken()).toBe('access-abc');
      });
    });

    describe('WhenTokensAreCleared', () => {
      it('ThenGetAccessTokenReturnsNull', () => {
        setAuthTokens('access-abc', 'refresh-xyz');
        clearAuthTokens();

        expect(getAccessToken()).toBeNull();
      });
    });

    describe('WhenSetAuthTokensIsCalledWithNull', () => {
      it('ThenGetAccessTokenReturnsNull', () => {
        setAuthTokens(null, null);

        expect(getAccessToken()).toBeNull();
      });
    });

    describe('WhenTokensAreUpdated', () => {
      it('ThenGetAccessTokenReturnsTheLatestToken', () => {
        setAuthTokens('first-token', 'first-refresh');
        setAuthTokens('second-token', 'second-refresh');

        expect(getAccessToken()).toBe('second-token');
      });
    });
  });

  describe('Request interceptor', () => {
    describe('WhenAnAccessTokenIsSet', () => {
      it('ThenTheRequestCarriesABearerAuthorizationHeader', async () => {
        setAuthTokens('access-abc', 'refresh-xyz');
        const adapter = jest.fn(async (config: InternalAxiosRequestConfig) => ({
          data: {}, status: 200, statusText: 'OK', headers: {}, config,
        }));
        apiClient.defaults.adapter = adapter;

        await apiClient.get('/v1/whatever');

        expect(adapter.mock.calls[0][0].headers.Authorization).toBe('Bearer access-abc');
      });
    });

    describe('WhenNoAccessTokenIsSet', () => {
      it('ThenTheRequestHasNoAuthorizationHeader', async () => {
        const adapter = jest.fn(async (config: InternalAxiosRequestConfig) => ({
          data: {}, status: 200, statusText: 'OK', headers: {}, config,
        }));
        apiClient.defaults.adapter = adapter;

        await apiClient.get('/v1/whatever');

        expect(adapter.mock.calls[0][0].headers.Authorization).toBeUndefined();
      });
    });
  });

  describe('Response interceptor - 401 refresh flow', () => {
    describe('WhenARequestFailsWith401AndARefreshTokenExists', () => {
      it('ThenTheSessionIsRefreshedAndTheOriginalRequestIsRetried', async () => {
        setAuthTokens('expired-access-token', 'valid-refresh-token');

        const adapter = jest.fn(async (config: InternalAxiosRequestConfig) => {
          if (config.headers.Authorization === 'Bearer new-access-token') {
            return { data: { ok: true }, status: 200, statusText: 'OK', headers: {}, config };
          }
          throw unauthorizedError(config);
        });
        apiClient.defaults.adapter = adapter;

        const postSpy = jest.spyOn(axios, 'post').mockResolvedValue({ data: tokenResponse });

        const response = await apiClient.get('/v1/protected');

        expect(postSpy).toHaveBeenCalledWith(
          expect.stringContaining('/v1/auth/refresh'),
          { token: 'valid-refresh-token' },
          expect.anything(),
        );
        expect(response.data).toEqual({ ok: true });
        expect(adapter).toHaveBeenCalledTimes(2);
        expect(getAccessToken()).toBe('new-access-token');
        expect(localStorage.getItem('mic_access_token')).toBe('new-access-token');
        expect(localStorage.getItem('mic_refresh_token')).toBe('new-refresh-token');
        expect(localStorage.getItem('mic_expires_at')).toBe(tokenResponse.expiresAt);
      });
    });

    describe('WhenTheRefreshRequestItselfFails', () => {
      it('ThenTokensAreClearedAndTheUserIsRedirectedToLogin', async () => {
        setAuthTokens('expired-access-token', 'invalid-refresh-token');
        localStorage.setItem('mic_access_token', 'expired-access-token');
        localStorage.setItem('mic_refresh_token', 'invalid-refresh-token');
        localStorage.setItem('mic_expires_at', '2026-01-01T00:00:00Z');

        const adapter = jest.fn(async (config: InternalAxiosRequestConfig) => {
          throw unauthorizedError(config);
        });
        apiClient.defaults.adapter = adapter;

        jest.spyOn(axios, 'post').mockRejectedValue(new Error('refresh endpoint unreachable'));

        await expect(apiClient.get('/v1/protected')).rejects.toThrow();

        expect(getAccessToken()).toBeNull();
        expect(localStorage.getItem('mic_access_token')).toBeNull();
        expect(localStorage.getItem('mic_refresh_token')).toBeNull();
        expect(localStorage.getItem('mic_expires_at')).toBeNull();
        expect(window.location.href).toBe('/login');
      });
    });

    describe('WhenThereIsNoRefreshTokenAvailable', () => {
      it('ThenTheOriginalErrorIsRejectedWithoutAttemptingRefresh', async () => {
        setAuthTokens(null, null);

        const adapter = jest.fn(async (config: InternalAxiosRequestConfig) => {
          throw unauthorizedError(config);
        });
        apiClient.defaults.adapter = adapter;

        const postSpy = jest.spyOn(axios, 'post');

        await expect(apiClient.get('/v1/protected')).rejects.toThrow();

        expect(postSpy).not.toHaveBeenCalled();
        expect(adapter).toHaveBeenCalledTimes(1);
      });
    });

    describe('WhenARequestFailsWithANonAuthError', () => {
      it('ThenTheErrorIsRejectedWithoutAttemptingRefresh', async () => {
        setAuthTokens('access-abc', 'refresh-xyz');

        const adapter = jest.fn(async (config: InternalAxiosRequestConfig) => {
          throw serverError(config);
        });
        apiClient.defaults.adapter = adapter;

        const postSpy = jest.spyOn(axios, 'post');

        await expect(apiClient.get('/v1/whatever')).rejects.toThrow();

        expect(postSpy).not.toHaveBeenCalled();
        expect(adapter).toHaveBeenCalledTimes(1);
      });
    });

    describe('WhenARetriedRequestFailsWith401Again', () => {
      it('ThenTheErrorIsRejectedWithoutLoopingForever', async () => {
        setAuthTokens('expired-access-token', 'valid-refresh-token');

        const adapter = jest.fn(async (config: InternalAxiosRequestConfig & { _retried?: boolean }) => {
          if (config._retried) {
            throw unauthorizedError(config);
          }
          throw unauthorizedError(config);
        });
        apiClient.defaults.adapter = adapter;

        jest.spyOn(axios, 'post').mockResolvedValue({ data: tokenResponse });

        await expect(apiClient.get('/v1/protected')).rejects.toThrow();

        // One failed attempt + one retry after refresh; the retry itself
        // is marked _retried so a second 401 does not trigger another refresh.
        expect(adapter).toHaveBeenCalledTimes(2);
      });
    });

    describe('WhenMultipleRequestsFailWith401WhileARefreshIsAlreadyInFlight', () => {
      it('ThenAllRequestsAreQueuedAndRetriedWithTheRefreshedToken', async () => {
        setAuthTokens('expired-access-token', 'valid-refresh-token');

        let resolveRefresh!: (value: { data: TokenResponse }) => void;
        const refreshPromise = new Promise<{ data: TokenResponse }>((resolve) => {
          resolveRefresh = resolve;
        });
        jest.spyOn(axios, 'post').mockReturnValue(refreshPromise as any);

        const adapter = jest.fn(async (config: InternalAxiosRequestConfig) => {
          if (config.headers.Authorization === 'Bearer new-access-token') {
            return { data: { url: config.url }, status: 200, statusText: 'OK', headers: {}, config };
          }
          throw unauthorizedError(config);
        });
        apiClient.defaults.adapter = adapter;

        const first = apiClient.get('/v1/first');
        const second = apiClient.get('/v1/second');

        // Let both initial 401s resolve before the refresh completes.
        await Promise.resolve();
        await Promise.resolve();
        await Promise.resolve();

        resolveRefresh({ data: tokenResponse });

        const [firstResponse, secondResponse] = await Promise.all([first, second]);

        expect(firstResponse.data).toEqual({ url: '/v1/first' });
        expect(secondResponse.data).toEqual({ url: '/v1/second' });
        expect(axios.post).toHaveBeenCalledTimes(1);
      });
    });
  });
});
