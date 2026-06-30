import apiClient from './client';
import type { AuditLogResponse, AuditLogFilter, PaginatedResponse } from '@/types/api';

function toParams(filter: AuditLogFilter) {
  return {
    resourceType: filter.resourceType ?? undefined,
    action: filter.action ?? undefined,
    from: filter.from ?? undefined,
    to: filter.to ?? undefined,
    page: filter.page ?? 1,
    pageSize: filter.pageSize ?? 20,
  };
}

export async function listAuditLogsByOrganization(
  orgId: number,
  filter: AuditLogFilter = {},
): Promise<PaginatedResponse<AuditLogResponse>> {
  const { data } = await apiClient.get<PaginatedResponse<AuditLogResponse>>(
    `/v1/organisation/${orgId}/audit-logs`,
    { params: toParams(filter) },
  );
  return data;
}

export async function listAuditLogsByProject(
  projectId: number,
  filter: AuditLogFilter = {},
): Promise<PaginatedResponse<AuditLogResponse>> {
  const { data } = await apiClient.get<PaginatedResponse<AuditLogResponse>>(
    `/v1/project/${projectId}/audit-logs`,
    { params: toParams(filter) },
  );
  return data;
}
