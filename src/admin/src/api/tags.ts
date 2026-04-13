import apiClient from './client';
import type { TagResponse, CreateTagRequest } from '@/types/api';

export async function listTags(projectId: number): Promise<TagResponse[]> {
  const { data } = await apiClient.get<TagResponse[]>(`/v1/projects/${projectId}/tags`);
  return data;
}

export async function createTag(projectId: number, request: CreateTagRequest): Promise<TagResponse> {
  const { data } = await apiClient.post<TagResponse>(`/v1/projects/${projectId}/tags`, request);
  return data;
}

export async function deleteTag(projectId: number, id: number): Promise<void> {
  await apiClient.delete(`/v1/projects/${projectId}/tags/${id}`);
}
