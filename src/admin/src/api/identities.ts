import apiClient from './client';
import type {
  IdentityResponse,
  TraitResponse,
  FeatureStateResponse,
  UpdateFeatureStateRequest,
  PaginatedResponse,
  CreateIdentityRequest,
  UpsertTraitRequest,
} from '@/types/api';

export async function listIdentities(
  envApiKey: string,
  page = 1,
  pageSize = 100,
): Promise<PaginatedResponse<IdentityResponse>> {
  const { data } = await apiClient.get<PaginatedResponse<IdentityResponse>>(
    `/v1/environment/${envApiKey}/identities`,
    { params: { page, pageSize } },
  );
  return data;
}

export async function createIdentity(
  envApiKey: string,
  request: CreateIdentityRequest,
): Promise<IdentityResponse> {
  const { data } = await apiClient.post<IdentityResponse>(
    `/v1/environment/${envApiKey}/identities`,
    request,
  );
  return data;
}

export async function getIdentity(envApiKey: string, id: number): Promise<IdentityResponse> {
  const { data } = await apiClient.get<IdentityResponse>(
    `/v1/environment/${envApiKey}/identity/${id}`,
  );
  return data;
}

export async function upsertIdentityTrait(
  envApiKey: string,
  identityId: number,
  key: string,
  request: UpsertTraitRequest,
): Promise<TraitResponse> {
  const { data } = await apiClient.put<TraitResponse>(
    `/v1/environment/${envApiKey}/identity/${identityId}/trait/${encodeURIComponent(key)}`,
    request,
  );
  return data;
}

export async function deleteIdentityTrait(
  envApiKey: string,
  identityId: number,
  key: string,
): Promise<void> {
  await apiClient.delete(
    `/v1/environment/${envApiKey}/identity/${identityId}/trait/${encodeURIComponent(key)}`,
  );
}

export async function deleteIdentity(envApiKey: string, id: number): Promise<void> {
  await apiClient.delete(`/v1/environment/${envApiKey}/identity/${id}`);
}

export async function getIdentityFeatureStates(
  envApiKey: string,
  identityId: number,
): Promise<FeatureStateResponse[]> {
  const { data } = await apiClient.get<FeatureStateResponse[]>(
    `/v1/environment/${envApiKey}/identity/${identityId}/featurestates`,
  );
  return data;
}

export async function setIdentityFeatureState(
  envApiKey: string,
  identityId: number,
  featureId: number,
  request: UpdateFeatureStateRequest,
): Promise<FeatureStateResponse> {
  const { data } = await apiClient.put<FeatureStateResponse>(
    `/v1/environment/${envApiKey}/identity/${identityId}/featurestate/${featureId}`,
    request,
  );
  return data;
}

export async function deleteIdentityFeatureState(
  envApiKey: string,
  identityId: number,
  featureId: number,
): Promise<void> {
  await apiClient.delete(
    `/v1/environment/${envApiKey}/identity/${identityId}/featurestate/${featureId}`,
  );
}
