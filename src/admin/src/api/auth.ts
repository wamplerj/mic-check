import apiClient from './client';
import type {
  LoginRequest,
  RegisterRequest,
  TokenResponse,
  RefreshRequest,
  LogoutRequest,
} from '@/types/api';

export async function login(request: LoginRequest): Promise<TokenResponse> {
  const { data } = await apiClient.post<TokenResponse>('/v1/auth/login', request);
  return data;
}

export async function register(request: RegisterRequest): Promise<TokenResponse> {
  const { data } = await apiClient.post<TokenResponse>('/v1/auth/register', request);
  return data;
}

export async function refreshToken(request: RefreshRequest): Promise<TokenResponse> {
  const { data } = await apiClient.post<TokenResponse>('/v1/auth/refresh', request);
  return data;
}

export async function logout(request: LogoutRequest): Promise<void> {
  await apiClient.post('/v1/auth/logout', request);
}
