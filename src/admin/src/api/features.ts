import apiClient from './client';
import type {
  FeatureResponse,
  CreateFeatureRequest,
  UpdateFeatureRequest,
  PatchFeatureRequest,
  PaginatedResponse,
} from '@/types/api';

export async function listFeatures(
  projectId: number,
  page = 1,
  pageSize = 100,
): Promise<PaginatedResponse<FeatureResponse>> {
  const { data } = await apiClient.get<PaginatedResponse<FeatureResponse>>(
    `/v1/projects/${projectId}/features`,
    { params: { page, pageSize } },
  );
  return data;
}

export async function getFeature(projectId: number, id: number): Promise<FeatureResponse> {
  const { data } = await apiClient.get<FeatureResponse>(
    `/v1/projects/${projectId}/features/${id}`,
  );
  return data;
}

export async function createFeature(
  projectId: number,
  request: CreateFeatureRequest,
): Promise<FeatureResponse> {
  const { data } = await apiClient.post<FeatureResponse>(
    `/v1/projects/${projectId}/features`,
    request,
  );
  return data;
}

export async function updateFeature(
  projectId: number,
  id: number,
  request: UpdateFeatureRequest,
): Promise<FeatureResponse> {
  const { data } = await apiClient.put<FeatureResponse>(
    `/v1/projects/${projectId}/features/${id}`,
    request,
  );
  return data;
}

export async function patchFeature(
  projectId: number,
  id: number,
  request: PatchFeatureRequest,
): Promise<FeatureResponse> {
  const { data } = await apiClient.patch<FeatureResponse>(
    `/v1/projects/${projectId}/features/${id}`,
    request,
  );
  return data;
}

export async function deleteFeature(projectId: number, id: number): Promise<void> {
  await apiClient.delete(`/v1/projects/${projectId}/features/${id}`);
}
