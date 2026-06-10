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

interface JwtPayload {
  sub?: string;
  email?: string;
  given_name?: string;
  family_name?: string;
}

function decodeJwtPayload(token: string): JwtPayload {
  try {
    const payload = token.split('.')[1];
    return JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')));
  } catch {
    return {};
  }
}

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(null);
  const refreshToken = ref<string | null>(null);
  const expiresAt = ref<string | null>(null);

  const firstName = ref<string>('');
  const lastName = ref<string>('');
  const email = ref<string>('');

  const isAuthenticated = computed(
    () => accessToken.value !== null && accessToken.value !== '',
  );

  const displayName = computed(() => {
    const full = `${firstName.value} ${lastName.value}`.trim();
    return full || email.value || 'Account';
  });

  function applyTokenClaims(token: string): void {
    const payload = decodeJwtPayload(token);
    firstName.value = payload.given_name ?? '';
    lastName.value = payload.family_name ?? '';
    email.value = payload.email ?? '';
  }

  function persistTokens(access: string, refresh: string, expiry: string): void {
    accessToken.value = access;
    refreshToken.value = refresh;
    expiresAt.value = expiry;
    localStorage.setItem(STORAGE_KEYS.accessToken, access);
    localStorage.setItem(STORAGE_KEYS.refreshToken, refresh);
    localStorage.setItem(STORAGE_KEYS.expiresAt, expiry);
    setAuthTokens(access, refresh);
    applyTokenClaims(access);
  }

  function clearPersistedTokens(): void {
    accessToken.value = null;
    refreshToken.value = null;
    expiresAt.value = null;
    firstName.value = '';
    lastName.value = '';
    email.value = '';
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
      applyTokenClaims(access);
    }
  }

  function updateProfile(newFirstName: string, newLastName: string, newEmail: string): void {
    firstName.value = newFirstName;
    lastName.value = newLastName;
    email.value = newEmail;
  }

  async function login(userEmail: string, password: string): Promise<void> {
    const request: LoginRequest = { email: userEmail, password };
    const response = await authApi.login(request);
    persistTokens(response.accessToken, response.refreshToken, response.expiresAt);
  }

  async function register(
    userEmail: string,
    password: string,
    userFirstName: string,
    userLastName: string,
    organizationName: string,
  ): Promise<void> {
    const request: RegisterRequest = { email: userEmail, password, firstName: userFirstName, lastName: userLastName, organizationName };
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
    firstName,
    lastName,
    email,
    displayName,
    isAuthenticated,
    loadFromStorage,
    updateProfile,
    login,
    register,
    logout,
  };
});
