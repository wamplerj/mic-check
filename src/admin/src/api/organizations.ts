import apiClient from './client';
import type {
  OrganizationResponse,
  CreateOrganizationRequest,
  UpdateOrganizationRequest,
  OrganizationMemberResponse,
  InviteUserRequest,
  PaginatedResponse,
} from '@/types/api';

export async function listOrganizations(page = 1, pageSize = 100): Promise<OrganizationResponse[]> {
  const { data } = await apiClient.get<PaginatedResponse<OrganizationResponse>>(
    '/v1/organisations',
    { params: { page, pageSize } },
  );
  return data.results;
}

export async function getOrganization(id: number): Promise<OrganizationResponse> {
  const { data } = await apiClient.get<OrganizationResponse>(`/v1/organisations/${id}`);
  return data;
}

export async function createOrganization(request: CreateOrganizationRequest): Promise<OrganizationResponse> {
  const { data } = await apiClient.post<OrganizationResponse>('/v1/organisations', request);
  return data;
}

export async function updateOrganization(id: number, request: UpdateOrganizationRequest): Promise<OrganizationResponse> {
  const { data } = await apiClient.put<OrganizationResponse>(`/v1/organisations/${id}`, request);
  return data;
}

export async function deleteOrganization(id: number): Promise<void> {
  await apiClient.delete(`/v1/organisations/${id}`);
}

export async function listOrganizationMembers(organizationId: number): Promise<OrganizationMemberResponse[]> {
  const { data } = await apiClient.get<OrganizationMemberResponse[]>(
    `/v1/organisations/${organizationId}/users`,
  );
  return data;
}

export async function inviteOrganizationMember(organizationId: number, request: InviteUserRequest): Promise<void> {
  await apiClient.post(`/v1/organisations/${organizationId}/users/invite`, request);
}

export async function removeOrganizationMember(organizationId: number, userId: number): Promise<void> {
  await apiClient.delete(`/v1/organisations/${organizationId}/users/${userId}`);
}
