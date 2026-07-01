import apiClient from './client';
import type {
  WebhookResponse,
  WebhookDeliveryLogResponse,
  CreateWebhookRequest,
} from '@/types/api';

// ─── Organization webhooks ────────────────────────────────────────────────────

export async function listOrgWebhooks(orgId: number): Promise<WebhookResponse[]> {
  const { data } = await apiClient.get<WebhookResponse[]>(`/v1/organisation/${orgId}/webhooks`);
  return data;
}

export async function createOrgWebhook(
  orgId: number,
  request: CreateWebhookRequest,
): Promise<WebhookResponse> {
  const { data } = await apiClient.post<WebhookResponse>(
    `/v1/organisation/${orgId}/webhooks`,
    request,
  );
  return data;
}

export async function updateOrgWebhook(
  orgId: number,
  webhookId: number,
  request: CreateWebhookRequest,
): Promise<WebhookResponse> {
  const { data } = await apiClient.put<WebhookResponse>(
    `/v1/organisation/${orgId}/webhook/${webhookId}`,
    request,
  );
  return data;
}

export async function deleteOrgWebhook(orgId: number, webhookId: number): Promise<void> {
  await apiClient.delete(`/v1/organisation/${orgId}/webhook/${webhookId}`);
}

// ─── Environment webhooks ─────────────────────────────────────────────────────

export async function listEnvWebhooks(envApiKey: string): Promise<WebhookResponse[]> {
  const { data } = await apiClient.get<WebhookResponse[]>(
    `/v1/environment/${envApiKey}/webhooks`,
  );
  return data;
}

export async function createEnvWebhook(
  envApiKey: string,
  request: CreateWebhookRequest,
): Promise<WebhookResponse> {
  const { data } = await apiClient.post<WebhookResponse>(
    `/v1/environment/${envApiKey}/webhooks`,
    request,
  );
  return data;
}

export async function updateEnvWebhook(
  envApiKey: string,
  webhookId: number,
  request: CreateWebhookRequest,
): Promise<WebhookResponse> {
  const { data } = await apiClient.put<WebhookResponse>(
    `/v1/environment/${envApiKey}/webhook/${webhookId}`,
    request,
  );
  return data;
}

export async function deleteEnvWebhook(envApiKey: string, webhookId: number): Promise<void> {
  await apiClient.delete(`/v1/environment/${envApiKey}/webhook/${webhookId}`);
}

// ─── Delivery logs ────────────────────────────────────────────────────────────

export async function listWebhookDeliveries(
  envApiKey: string,
  webhookId: number,
): Promise<WebhookDeliveryLogResponse[]> {
  const { data } = await apiClient.get<WebhookDeliveryLogResponse[]>(
    `/v1/environment/${envApiKey}/webhook/${webhookId}/deliveries`,
  );
  return data;
}
