import apiClient from './client';
import type { UserProfileResponse, UpdateProfileRequest, ChangePasswordRequest } from '@/types/api';

export async function getMe(): Promise<UserProfileResponse> {
  const { data } = await apiClient.get<UserProfileResponse>('/v1/users/me');
  return data;
}

export async function updateMe(request: UpdateProfileRequest): Promise<UserProfileResponse> {
  const { data } = await apiClient.put<UserProfileResponse>('/v1/users/me', request);
  return data;
}

export async function changePassword(request: ChangePasswordRequest): Promise<void> {
  await apiClient.post('/v1/users/me/change-password', request);
}

export async function getUserById(id: number): Promise<UserProfileResponse> {
  const { data } = await apiClient.get<UserProfileResponse>(`/v1/users/${id}`);
  return data;
}
