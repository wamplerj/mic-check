import apiClient from './client';
import type {
  ProjectResponse,
  CreateProjectRequest,
  UpdateProjectRequest,
  UserPermissionResponse,
  SetUserPermissionsRequest,
  PaginatedResponse,
} from '@/types/api';

export async function listProjects(organizationId: number, page = 1, pageSize = 100): Promise<ProjectResponse[]> {
  const { data } = await apiClient.get<PaginatedResponse<ProjectResponse>>(
    '/v1/projects',
    { params: { organizationId, page, pageSize } },
  );
  return data.results;
}

export async function getProject(id: number): Promise<ProjectResponse> {
  const { data } = await apiClient.get<ProjectResponse>(`/v1/project/${id}`);
  return data;
}

export async function createProject(request: CreateProjectRequest): Promise<ProjectResponse> {
  const { data } = await apiClient.post<ProjectResponse>('/v1/projects', request);
  return data;
}

export async function updateProject(id: number, request: UpdateProjectRequest): Promise<ProjectResponse> {
  const { data } = await apiClient.put<ProjectResponse>(`/v1/project/${id}`, request);
  return data;
}

export async function deleteProject(id: number): Promise<void> {
  await apiClient.delete(`/v1/project/${id}`);
}

export async function listProjectUserPermissions(projectId: number): Promise<UserPermissionResponse[]> {
  const { data } = await apiClient.get<UserPermissionResponse[]>(
    `/v1/project/${projectId}/user-permissions`,
  );
  return data;
}

export async function setProjectUserPermissions(
  projectId: number,
  request: SetUserPermissionsRequest,
): Promise<UserPermissionResponse> {
  const { data } = await apiClient.post<UserPermissionResponse>(
    `/v1/project/${projectId}/user-permissions`,
    request,
  );
  return data;
}

export async function updateProjectUserPermissions(
  projectId: number,
  userId: number,
  request: SetUserPermissionsRequest,
): Promise<UserPermissionResponse> {
  const { data } = await apiClient.put<UserPermissionResponse>(
    `/v1/project/${projectId}/user-permission/${userId}`,
    request,
  );
  return data;
}

export async function removeProjectUserPermissions(projectId: number, userId: number): Promise<void> {
  await apiClient.delete(`/v1/project/${projectId}/user-permission/${userId}`);
}
