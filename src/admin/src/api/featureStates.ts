import apiClient from './client';
import type {
  FeatureStateResponse,
  UpdateFeatureStateRequest,
  PatchFeatureStateRequest,
} from '@/types/api';

export async function listFeatureStates(envApiKey: string): Promise<FeatureStateResponse[]> {
  const { data } = await apiClient.get<FeatureStateResponse[]>(
    `/v1/environment/${envApiKey}/featurestates`,
  );
  return data;
}

export async function getFeatureState(envApiKey: string, id: number): Promise<FeatureStateResponse> {
  const { data } = await apiClient.get<FeatureStateResponse>(
    `/v1/environment/${envApiKey}/featurestate/${id}`,
  );
  return data;
}

export async function updateFeatureState(
  envApiKey: string,
  id: number,
  request: UpdateFeatureStateRequest,
): Promise<FeatureStateResponse> {
  const { data } = await apiClient.put<FeatureStateResponse>(
    `/v1/environment/${envApiKey}/featurestate/${id}`,
    request,
  );
  return data;
}

export async function patchFeatureState(
  envApiKey: string,
  id: number,
  request: PatchFeatureStateRequest,
): Promise<FeatureStateResponse> {
  const { data } = await apiClient.patch<FeatureStateResponse>(
    `/v1/environment/${envApiKey}/featurestate/${id}`,
    request,
  );
  return data;
}
