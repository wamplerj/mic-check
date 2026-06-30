import apiClient from './client';
import type { ApiKeyResponse, CreateApiKeyRequest, CreateApiKeyResponse } from '@/types/api';

export async function listApiKeys(organizationId: number): Promise<ApiKeyResponse[]> {
  const { data } = await apiClient.get<ApiKeyResponse[]>(
    `/v1/organisation/${organizationId}/api-keys`,
  );
  return data;
}

export async function createApiKey(
  organizationId: number,
  request: CreateApiKeyRequest,
): Promise<CreateApiKeyResponse> {
  const { data } = await apiClient.post<CreateApiKeyResponse>(
    `/v1/organisation/${organizationId}/api-keys`,
    request,
  );
  return data;
}

export async function deleteApiKey(organizationId: number, keyId: number): Promise<void> {
  await apiClient.delete(`/v1/organisation/${organizationId}/api-key/${keyId}`);
}
