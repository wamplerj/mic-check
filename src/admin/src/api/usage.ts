import apiClient from './client';
import type { DashboardUsageResponse } from '@/types/api';

export async function getUsageDashboard(environmentId: number, days = 14): Promise<DashboardUsageResponse> {
  const { data } = await apiClient.get<DashboardUsageResponse>(`/v1/environments/${environmentId}/usage`, { params: { days } });
  return data;
}
