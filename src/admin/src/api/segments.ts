import apiClient from './client';
import type {
  SegmentResponse,
  CreateSegmentRequest,
  UpdateSegmentRequest,
  PaginatedResponse,
} from '@/types/api';

export async function listSegments(projectId: number, page = 1, pageSize = 100): Promise<SegmentResponse[]> {
  const { data } = await apiClient.get<PaginatedResponse<SegmentResponse>>(
    `/v1/project/${projectId}/segments`,
    { params: { page, pageSize } },
  );
  return data.results;
}

export async function getSegment(projectId: number, id: number): Promise<SegmentResponse> {
  const { data } = await apiClient.get<SegmentResponse>(
    `/v1/project/${projectId}/segment/${id}`,
  );
  return data;
}

export async function createSegment(
  projectId: number,
  request: CreateSegmentRequest,
): Promise<SegmentResponse> {
  const { data } = await apiClient.post<SegmentResponse>(
    `/v1/project/${projectId}/segments`,
    request,
  );
  return data;
}

export async function updateSegment(
  projectId: number,
  id: number,
  request: UpdateSegmentRequest,
): Promise<SegmentResponse> {
  const { data } = await apiClient.put<SegmentResponse>(
    `/v1/project/${projectId}/segment/${id}`,
    request,
  );
  return data;
}

export async function deleteSegment(projectId: number, id: number): Promise<void> {
  await apiClient.delete(`/v1/project/${projectId}/segment/${id}`);
}
