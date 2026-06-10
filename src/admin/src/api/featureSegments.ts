import apiClient from './client';
import type {
  FeatureSegmentResponse,
  CreateFeatureSegmentRequest,
  UpdateFeatureSegmentRequest,
  SegmentSummaryResponse,
} from '@/types/api';

export async function listFeatureSegments(
  envApiKey: string,
  featureId: number,
): Promise<FeatureSegmentResponse[]> {
  const { data } = await apiClient.get<FeatureSegmentResponse[]>(
    `/v1/environments/${envApiKey}/features/${featureId}/segments`,
  );
  return data;
}

export async function createFeatureSegment(
  envApiKey: string,
  featureId: number,
  request: CreateFeatureSegmentRequest,
): Promise<FeatureSegmentResponse> {
  const { data } = await apiClient.post<FeatureSegmentResponse>(
    `/v1/environments/${envApiKey}/features/${featureId}/segments`,
    request,
  );
  return data;
}

export async function updateFeatureSegment(
  envApiKey: string,
  featureId: number,
  id: number,
  request: UpdateFeatureSegmentRequest,
): Promise<FeatureSegmentResponse> {
  const { data } = await apiClient.put<FeatureSegmentResponse>(
    `/v1/environments/${envApiKey}/features/${featureId}/segments/${id}`,
    request,
  );
  return data;
}

export async function deleteFeatureSegment(
  envApiKey: string,
  featureId: number,
  id: number,
): Promise<void> {
  await apiClient.delete(`/v1/environments/${envApiKey}/features/${featureId}/segments/${id}`);
}

export async function listIdentitySegments(
  envApiKey: string,
  identityId: number,
): Promise<SegmentSummaryResponse[]> {
  const { data } = await apiClient.get<SegmentSummaryResponse[]>(
    `/v1/environments/${envApiKey}/identities/${identityId}/segments`,
  );
  return data;
}
