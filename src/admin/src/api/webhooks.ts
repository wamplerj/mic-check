import apiClient from './client';
import type {
  WebhookResponse,
  WebhookDeliveryLogResponse,
  CreateWebhookRequest,
} from '@/types/api';

// ─── Organization webhooks ────────────────────────────────────────────────────

export async function listOrgWebhooks(orgId: number): Promise<WebhookResponse[]> {
  const { data } = await apiClient.get<WebhookResponse[]>(`/v1/organisations/${orgId}/webhooks`);
  return data;
}

export async function createOrgWebhook(
  orgId: number,
  request: CreateWebhookRequest,
): Promise<WebhookResponse> {
  const { data } = await apiClient.post<WebhookResponse>(
    `/v1/organisations/${orgId}/webhooks`,
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
    `/v1/organisations/${orgId}/webhooks/${webhookId}`,
    request,
  );
  return data;
}

export async function deleteOrgWebhook(orgId: number, webhookId: number): Promise<void> {
  await apiClient.delete(`/v1/organisations/${orgId}/webhooks/${webhookId}`);
}

// ─── Environment webhooks ─────────────────────────────────────────────────────

export async function listEnvWebhooks(envApiKey: string): Promise<WebhookResponse[]> {
  const { data } = await apiClient.get<WebhookResponse[]>(
    `/v1/environments/${envApiKey}/webhooks`,
  );
  return data;
}

export async function createEnvWebhook(
  envApiKey: string,
  request: CreateWebhookRequest,
): Promise<WebhookResponse> {
  const { data } = await apiClient.post<WebhookResponse>(
    `/v1/environments/${envApiKey}/webhooks`,
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
    `/v1/environments/${envApiKey}/webhooks/${webhookId}`,
    request,
  );
  return data;
}

export async function deleteEnvWebhook(envApiKey: string, webhookId: number): Promise<void> {
  await apiClient.delete(`/v1/environments/${envApiKey}/webhooks/${webhookId}`);
}

// ─── Delivery logs ────────────────────────────────────────────────────────────

export async function listWebhookDeliveries(
  envApiKey: string,
  webhookId: number,
): Promise<WebhookDeliveryLogResponse[]> {
  const { data } = await apiClient.get<WebhookDeliveryLogResponse[]>(
    `/v1/environments/${envApiKey}/webhooks/${webhookId}/deliveries`,
  );
  return data;
}
