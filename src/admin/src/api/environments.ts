import apiClient from './client';
import type {
  EnvironmentResponse,
  CreateEnvironmentRequest,
  UpdateEnvironmentRequest,
  CloneEnvironmentRequest,
  PaginatedResponse,
} from '@/types/api';

export async function listEnvironments(projectId: number, page = 1, pageSize = 100): Promise<EnvironmentResponse[]> {
  const { data } = await apiClient.get<PaginatedResponse<EnvironmentResponse>>(
    '/v1/environments',
    { params: { projectId, page, pageSize } },
  );
  return data.results;
}

export async function getEnvironment(apiKey: string): Promise<EnvironmentResponse> {
  const { data } = await apiClient.get<EnvironmentResponse>(`/v1/environment/${apiKey}`);
  return data;
}

export async function createEnvironment(request: CreateEnvironmentRequest): Promise<EnvironmentResponse> {
  const { data } = await apiClient.post<EnvironmentResponse>('/v1/environments', request);
  return data;
}

export async function updateEnvironment(apiKey: string, request: UpdateEnvironmentRequest): Promise<EnvironmentResponse> {
  const { data } = await apiClient.put<EnvironmentResponse>(`/v1/environment/${apiKey}`, request);
  return data;
}

export async function deleteEnvironment(apiKey: string): Promise<void> {
  await apiClient.delete(`/v1/environment/${apiKey}`);
}

export async function cloneEnvironment(apiKey: string, request: CloneEnvironmentRequest): Promise<EnvironmentResponse> {
  const { data } = await apiClient.post<EnvironmentResponse>(
    `/v1/environment/${apiKey}/clone`,
    request,
  );
  return data;
}
