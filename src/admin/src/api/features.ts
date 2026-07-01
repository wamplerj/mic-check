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
    `/v1/project/${projectId}/features`,
    { params: { page, pageSize } },
  );
  return data;
}

export async function getFeature(projectId: number, id: number): Promise<FeatureResponse> {
  const { data } = await apiClient.get<FeatureResponse>(
    `/v1/project/${projectId}/feature/${id}`,
  );
  return data;
}

export async function createFeature(
  projectId: number,
  request: CreateFeatureRequest,
): Promise<FeatureResponse> {
  const { data } = await apiClient.post<FeatureResponse>(
    `/v1/project/${projectId}/features`,
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
    `/v1/project/${projectId}/feature/${id}`,
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
    `/v1/project/${projectId}/feature/${id}`,
    request,
  );
  return data;
}

export async function deleteFeature(projectId: number, id: number): Promise<void> {
  await apiClient.delete(`/v1/project/${projectId}/feature/${id}`);
}

export async function assignFeatureTag(
  projectId: number,
  featureId: number,
  tagId: number,
): Promise<FeatureResponse> {
  const { data } = await apiClient.put<FeatureResponse>(
    `/v1/project/${projectId}/feature/${featureId}/tag/${tagId}`,
  );
  return data;
}

export async function removeFeatureTag(
  projectId: number,
  featureId: number,
  tagId: number,
): Promise<FeatureResponse> {
  const { data } = await apiClient.delete<FeatureResponse>(
    `/v1/project/${projectId}/feature/${featureId}/tag/${tagId}`,
  );
  return data;
}
