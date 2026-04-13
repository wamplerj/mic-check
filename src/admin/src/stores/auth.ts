import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import { setAuthTokens, clearAuthTokens } from '@/api/client';
import * as authApi from '@/api/auth';
import type { LoginRequest, RegisterRequest } from '@/types/api';

const STORAGE_KEYS = {
  accessToken: 'mic_access_token',
  refreshToken: 'mic_refresh_token',
  expiresAt: 'mic_expires_at',
} as const;

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(null);
  const refreshToken = ref<string | null>(null);
  const expiresAt = ref<string | null>(null);

  const isAuthenticated = computed(
    () => accessToken.value !== null && accessToken.value !== '',
  );

  function persistTokens(access: string, refresh: string, expiry: string): void {
    accessToken.value = access;
    refreshToken.value = refresh;
    expiresAt.value = expiry;
    localStorage.setItem(STORAGE_KEYS.accessToken, access);
    localStorage.setItem(STORAGE_KEYS.refreshToken, refresh);
    localStorage.setItem(STORAGE_KEYS.expiresAt, expiry);
    setAuthTokens(access, refresh);
  }

  function clearPersistedTokens(): void {
    accessToken.value = null;
    refreshToken.value = null;
    expiresAt.value = null;
    localStorage.removeItem(STORAGE_KEYS.accessToken);
    localStorage.removeItem(STORAGE_KEYS.refreshToken);
    localStorage.removeItem(STORAGE_KEYS.expiresAt);
    clearAuthTokens();
  }

  function loadFromStorage(): void {
    const access = localStorage.getItem(STORAGE_KEYS.accessToken);
    const refresh = localStorage.getItem(STORAGE_KEYS.refreshToken);
    const expiry = localStorage.getItem(STORAGE_KEYS.expiresAt);

    if (access && refresh) {
      accessToken.value = access;
      refreshToken.value = refresh;
      expiresAt.value = expiry;
      setAuthTokens(access, refresh);
    }
  }

  async function login(email: string, password: string): Promise<void> {
    const request: LoginRequest = { email, password };
    const response = await authApi.login(request);
    persistTokens(response.accessToken, response.refreshToken, response.expiresAt);
  }

  async function register(
    email: string,
    password: string,
    firstName: string,
    lastName: string,
    organizationName: string,
  ): Promise<void> {
    const request: RegisterRequest = { email, password, firstName, lastName, organizationName };
    const response = await authApi.register(request);
    persistTokens(response.accessToken, response.refreshToken, response.expiresAt);
  }

  async function logout(): Promise<void> {
    if (refreshToken.value) {
      try {
        await authApi.logout({ token: refreshToken.value });
      } catch {
        // Proceed with local logout even if the server call fails
      }
    }
    clearPersistedTokens();
  }

  return {
    accessToken,
    refreshToken,
    expiresAt,
    isAuthenticated,
    loadFromStorage,
    login,
    register,
    logout,
  };
});
