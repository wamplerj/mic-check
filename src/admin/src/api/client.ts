import axios, { AxiosInstance, AxiosError, InternalAxiosRequestConfig } from 'axios';
import type { TokenResponse, RefreshRequest } from '@/types/api';

// ─── Token State ──────────────────────────────────────────────────────────────
// Stored at module level so the client never imports the auth store,
// avoiding circular dependencies. The auth store calls setAuthTokens()
// after login/logout/loadFromStorage.

let _accessToken: string | null = null;
let _refreshToken: string | null = null;
let _isRefreshing = false;
let _refreshQueue: Array<(token: string) => void> = [];

export function setAuthTokens(access: string | null, refresh: string | null): void {
  _accessToken = access;
  _refreshToken = refresh;
}

export function clearAuthTokens(): void {
  _accessToken = null;
  _refreshToken = null;
}

export function getAccessToken(): string | null {
  return _accessToken;
}

// ─── Axios Instance ───────────────────────────────────────────────────────────

const API_BASE_URL =
  typeof process !== 'undefined' && process.env.API_BASE_URL
    ? process.env.API_BASE_URL
    : '/api';

const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
  timeout: 15000,
});

// ─── Request Interceptor ──────────────────────────────────────────────────────

apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    if (_accessToken) {
      config.headers.Authorization = `Bearer ${_accessToken}`;
    }
    return config;
  },
  (error) => Promise.reject(error),
);

// ─── Response Interceptor (401 Refresh) ───────────────────────────────────────

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retried?: boolean };

    if (error.response?.status !== 401 || originalRequest._retried || !_refreshToken) {
      return Promise.reject(error);
    }

    if (_isRefreshing) {
      // Queue requests while a refresh is in flight
      return new Promise((resolve, reject) => {
        _refreshQueue.push((newToken: string) => {
          originalRequest.headers.Authorization = `Bearer ${newToken}`;
          resolve(apiClient(originalRequest));
        });
        // Store reject so the queue can be drained on failure
        void reject;
      });
    }

    originalRequest._retried = true;
    _isRefreshing = true;

    try {
      const payload: RefreshRequest = { token: _refreshToken! };
      const { data } = await axios.post<TokenResponse>(
        `${API_BASE_URL}/v1/auth/refresh`,
        payload,
        { headers: { 'Content-Type': 'application/json' } },
      );

      setAuthTokens(data.accessToken, data.refreshToken);

      // Persist updated tokens to localStorage so auth store stays in sync
      localStorage.setItem('mic_access_token', data.accessToken);
      localStorage.setItem('mic_refresh_token', data.refreshToken);
      localStorage.setItem('mic_expires_at', data.expiresAt);

      // Drain the queue
      _refreshQueue.forEach((cb) => cb(data.accessToken));
      _refreshQueue = [];

      // Retry the original request
      originalRequest.headers.Authorization = `Bearer ${data.accessToken}`;
      return apiClient(originalRequest);
    } catch {
      // Refresh failed — clear tokens and redirect to login
      clearAuthTokens();
      localStorage.removeItem('mic_access_token');
      localStorage.removeItem('mic_refresh_token');
      localStorage.removeItem('mic_expires_at');
      _refreshQueue = [];

      // Redirect to login without importing the router (avoids circular deps)
      if (typeof window !== 'undefined') {
        window.location.href = '/login';
      }
      return Promise.reject(error);
    } finally {
      _isRefreshing = false;
    }
  },
);

export default apiClient;
