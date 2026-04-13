import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { setActivePinia, createPinia } from 'pinia';
import { useAuthStore } from '@/stores/auth';
import * as authApi from '@/api/auth';
import * as clientModule from '@/api/client';

jest.mock('@/api/auth');
jest.mock('@/api/client', () => ({
  setAuthTokens: jest.fn(),
  clearAuthTokens: jest.fn(),
  getAccessToken: jest.fn(),
}));

const mockTokenResponse = {
  accessToken: 'access-token-123',
  refreshToken: 'refresh-token-456',
  expiresAt: '2026-12-31T00:00:00Z',
};

describe('useAuthStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    localStorage.clear();
    jest.clearAllMocks();
  });

  describe('WhenUserLogsInWithValidCredentials', () => {
    it('ThenTokensAreStoredInStateAndLocalStorage', async () => {
      jest.mocked(authApi.login).mockResolvedValue(mockTokenResponse);

      const store = useAuthStore();
      await store.login('user@example.com', 'password123');

      expect(store.isAuthenticated).toBe(true);
      expect(store.accessToken).toBe(mockTokenResponse.accessToken);
      expect(store.refreshToken).toBe(mockTokenResponse.refreshToken);
      expect(store.expiresAt).toBe(mockTokenResponse.expiresAt);
      expect(localStorage.getItem('mic_access_token')).toBe(mockTokenResponse.accessToken);
      expect(localStorage.getItem('mic_refresh_token')).toBe(mockTokenResponse.refreshToken);
      expect(localStorage.getItem('mic_expires_at')).toBe(mockTokenResponse.expiresAt);
    });

    it('ThenApiClientTokensAreUpdated', async () => {
      jest.mocked(authApi.login).mockResolvedValue(mockTokenResponse);

      const store = useAuthStore();
      await store.login('user@example.com', 'password123');

      expect(clientModule.setAuthTokens).toHaveBeenCalledWith(
        mockTokenResponse.accessToken,
        mockTokenResponse.refreshToken,
      );
    });
  });

  describe('WhenUserLogsInWithInvalidCredentials', () => {
    it('ThenAuthStateRemainsUnauthenticated', async () => {
      jest.mocked(authApi.login).mockRejectedValue(new Error('Unauthorized'));

      const store = useAuthStore();
      await expect(store.login('bad@example.com', 'wrong')).rejects.toThrow('Unauthorized');

      expect(store.isAuthenticated).toBe(false);
      expect(store.accessToken).toBeNull();
      expect(localStorage.getItem('mic_access_token')).toBeNull();
    });
  });

  describe('WhenUserRegistersSuccessfully', () => {
    it('ThenTokensAreStoredAndUserIsAuthenticated', async () => {
      jest.mocked(authApi.register).mockResolvedValue(mockTokenResponse);

      const store = useAuthStore();
      await store.register('user@example.com', 'password123', 'Jane', 'Doe', 'Acme Corp');

      expect(store.isAuthenticated).toBe(true);
      expect(store.accessToken).toBe(mockTokenResponse.accessToken);
    });

    it('ThenRegisterApiIsCalledWithCorrectPayload', async () => {
      jest.mocked(authApi.register).mockResolvedValue(mockTokenResponse);

      const store = useAuthStore();
      await store.register('user@example.com', 'password123', 'Jane', 'Doe', 'Acme Corp');

      expect(authApi.register).toHaveBeenCalledWith({
        email: 'user@example.com',
        password: 'password123',
        firstName: 'Jane',
        lastName: 'Doe',
        organizationName: 'Acme Corp',
      });
    });
  });

  describe('WhenUserLogsOut', () => {
    it('ThenTokensAreRemovedFromStateAndLocalStorage', async () => {
      jest.mocked(authApi.login).mockResolvedValue(mockTokenResponse);
      jest.mocked(authApi.logout).mockResolvedValue(undefined);

      const store = useAuthStore();
      await store.login('user@example.com', 'password123');
      await store.logout();

      expect(store.isAuthenticated).toBe(false);
      expect(store.accessToken).toBeNull();
      expect(store.refreshToken).toBeNull();
      expect(localStorage.getItem('mic_access_token')).toBeNull();
      expect(localStorage.getItem('mic_refresh_token')).toBeNull();
    });

    it('ThenApiClientTokensAreCleared', async () => {
      jest.mocked(authApi.login).mockResolvedValue(mockTokenResponse);
      jest.mocked(authApi.logout).mockResolvedValue(undefined);

      const store = useAuthStore();
      await store.login('user@example.com', 'password123');
      jest.clearAllMocks();

      await store.logout();

      expect(clientModule.clearAuthTokens).toHaveBeenCalled();
    });

    it('ThenLocalLogoutCompletesEvenWhenServerCallFails', async () => {
      jest.mocked(authApi.login).mockResolvedValue(mockTokenResponse);
      jest.mocked(authApi.logout).mockRejectedValue(new Error('Network error'));

      const store = useAuthStore();
      await store.login('user@example.com', 'password123');
      await store.logout();

      expect(store.isAuthenticated).toBe(false);
      expect(localStorage.getItem('mic_access_token')).toBeNull();
    });
  });

  describe('WhenLoadFromStorageIsCalledWithSavedTokens', () => {
    it('ThenAuthStateIsRestoredFromLocalStorage', () => {
      localStorage.setItem('mic_access_token', 'stored-access');
      localStorage.setItem('mic_refresh_token', 'stored-refresh');
      localStorage.setItem('mic_expires_at', '2026-12-31T00:00:00Z');

      const store = useAuthStore();
      store.loadFromStorage();

      expect(store.isAuthenticated).toBe(true);
      expect(store.accessToken).toBe('stored-access');
      expect(store.refreshToken).toBe('stored-refresh');
    });

    it('ThenApiClientTokensAreSet', () => {
      localStorage.setItem('mic_access_token', 'stored-access');
      localStorage.setItem('mic_refresh_token', 'stored-refresh');

      const store = useAuthStore();
      store.loadFromStorage();

      expect(clientModule.setAuthTokens).toHaveBeenCalledWith('stored-access', 'stored-refresh');
    });
  });

  describe('WhenLoadFromStorageIsCalledWithNoSavedTokens', () => {
    it('ThenUserRemainsUnauthenticated', () => {
      const store = useAuthStore();
      store.loadFromStorage();

      expect(store.isAuthenticated).toBe(false);
      expect(store.accessToken).toBeNull();
    });
  });
});
