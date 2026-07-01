<template>
  <div data-testid="webhooks-panel">
    <div class="d-flex align-center justify-space-between mb-4">
      <p class="text-body-1 font-weight-medium mb-0">Webhooks</p>
      <v-btn
        size="small"
        color="primary"
        variant="tonal"
        prepend-icon="ri-add-line"
        data-testid="add-webhook-btn"
        @click="openAddDialog"
      >
        Add Webhook
      </v-btn>
    </div>

    <v-alert
      v-if="errorMessage"
      type="error"
      variant="tonal"
      density="compact"
      closable
      class="mb-4"
      @click:close="errorMessage = null"
    >
      {{ errorMessage }}
    </v-alert>

    <div v-if="isLoading" class="d-flex justify-center py-6">
      <v-progress-circular indeterminate color="primary" />
    </div>

    <template v-else>
      <div
        v-if="webhooks.length === 0"
        class="text-center py-6 text-medium-emphasis text-body-2"
        data-testid="no-webhooks-message"
      >
        No webhooks configured. Add one to start receiving events.
      </div>

      <v-table v-else density="compact" data-testid="webhooks-table">
        <thead>
          <tr>
            <th>URL</th>
            <th style="width: 80px">Enabled</th>
            <th style="width: 120px">Created</th>
            <th style="width: 120px" class="text-end">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="hook in webhooks" :key="hook.id" :data-testid="`webhook-row-${hook.id}`">
            <td>
              <span class="text-body-2 font-weight-medium" style="word-break: break-all">
                {{ hook.url }}
              </span>
            </td>
            <td>
              <v-switch
                :model-value="hook.enabled"
                color="primary"
                hide-details
                density="compact"
                :loading="togglingId === hook.id"
                :data-testid="`webhook-enabled-toggle-${hook.id}`"
                @update:model-value="onToggleEnabled(hook, $event)"
              />
            </td>
            <td class="text-caption text-medium-emphasis">{{ formatDate(hook.createdAt) }}</td>
            <td class="text-end">
              <v-btn
                icon="ri-history-line"
                size="x-small"
                variant="text"
                :data-testid="`view-deliveries-${hook.id}`"
                @click="openDeliveries(hook)"
              />
              <v-btn
                icon="ri-edit-line"
                size="x-small"
                variant="text"
                :data-testid="`edit-webhook-${hook.id}`"
                @click="openEditDialog(hook)"
              />
              <v-btn
                icon="ri-delete-bin-line"
                size="x-small"
                variant="text"
                color="error"
                :data-testid="`delete-webhook-${hook.id}`"
                @click="openDeleteConfirm(hook)"
              />
            </td>
          </tr>
        </tbody>
      </v-table>
    </template>

    <!-- Add / Edit dialog -->
    <WebhookDialog
      v-model="showDialog"
      :webhook="editingWebhook"
      :org-id="orgId"
      :env-api-key="envApiKey"
      @saved="onWebhookSaved"
    />

    <!-- Delivery history dialog -->
    <v-dialog v-model="showDeliveries" max-width="700">
      <v-card rounded="lg" data-testid="deliveries-dialog">
        <v-card-title class="d-flex align-center justify-space-between pa-4 pb-2">
          <span class="text-h6">Delivery History</span>
          <v-btn icon="ri-close-line" variant="text" size="small" @click="showDeliveries = false" />
        </v-card-title>
        <v-divider />
        <v-card-text class="pa-4">
          <WebhookDeliveryHistory
            v-if="deliveryWebhook && envApiKey"
            :env-api-key="envApiKey"
            :webhook-id="deliveryWebhook.id"
          />
          <p v-else class="text-body-2 text-medium-emphasis">
            Delivery history is only available for environment-scoped webhooks.
          </p>
        </v-card-text>
      </v-card>
    </v-dialog>

    <!-- Delete confirmation dialog -->
    <v-dialog v-model="showDeleteDialog" max-width="400">
      <v-card rounded="lg" data-testid="delete-webhook-dialog">
        <v-card-title class="text-body-1 font-weight-bold pa-4 pb-2">Delete Webhook</v-card-title>
        <v-card-text class="pa-4 pt-0">
          <p class="text-body-2">
            Are you sure you want to delete the webhook for
            <strong>{{ deletingWebhook?.url }}</strong>?
          </p>
        </v-card-text>
        <v-card-actions class="pa-4 pt-0">
          <v-spacer />
          <v-btn variant="text" @click="showDeleteDialog = false">Cancel</v-btn>
          <v-btn
            color="error"
            variant="flat"
            :loading="isDeleting"
            data-testid="confirm-delete-webhook-btn"
            @click="onDeleteWebhook"
          >
            Delete
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import {
  listOrgWebhooks, createOrgWebhook, updateOrgWebhook, deleteOrgWebhook,
  listEnvWebhooks, updateEnvWebhook, deleteEnvWebhook,
} from '@/api/webhooks';
import WebhookDialog from './WebhookDialog.vue';
import WebhookDeliveryHistory from './WebhookDeliveryHistory.vue';
import type { WebhookResponse } from '@/types/api';

interface Props {
  orgId?: number;
  envApiKey?: string;
}

const props = defineProps<Props>();

const webhooks = ref<WebhookResponse[]>([]);
const isLoading = ref(false);
const errorMessage = ref<string | null>(null);
const togglingId = ref<number | null>(null);

const showDialog = ref(false);
const editingWebhook = ref<WebhookResponse | null>(null);

const showDeliveries = ref(false);
const deliveryWebhook = ref<WebhookResponse | null>(null);

const showDeleteDialog = ref(false);
const deletingWebhook = ref<WebhookResponse | null>(null);
const isDeleting = ref(false);

async function loadWebhooks(): Promise<void> {
  isLoading.value = true;
  errorMessage.value = null;
  try {
    if (props.envApiKey) {
      webhooks.value = await listEnvWebhooks(props.envApiKey);
    } else if (props.orgId !== undefined) {
      webhooks.value = await listOrgWebhooks(props.orgId);
    }
  } catch {
    webhooks.value = [];
    errorMessage.value = 'Failed to load webhooks. Please try again.';
  } finally {
    isLoading.value = false;
  }
}

onMounted(loadWebhooks);

function openAddDialog(): void {
  editingWebhook.value = null;
  showDialog.value = true;
}

function openEditDialog(hook: WebhookResponse): void {
  editingWebhook.value = hook;
  showDialog.value = true;
}

function openDeliveries(hook: WebhookResponse): void {
  deliveryWebhook.value = hook;
  showDeliveries.value = true;
}

function openDeleteConfirm(hook: WebhookResponse): void {
  deletingWebhook.value = hook;
  showDeleteDialog.value = true;
}

async function onToggleEnabled(hook: WebhookResponse, enabled: boolean | null): Promise<void> {
  if (enabled === null) return;
  togglingId.value = hook.id;
  errorMessage.value = null;
  try {
    const request = { url: hook.url, secret: hook.secret, enabled };
    let updated: WebhookResponse;
    if (props.envApiKey) {
      updated = await updateEnvWebhook(props.envApiKey, hook.id, request);
    } else if (props.orgId !== undefined) {
      updated = await updateOrgWebhook(props.orgId, hook.id, request);
    } else {
      return;
    }
    const idx = webhooks.value.findIndex((w) => w.id === hook.id);
    if (idx >= 0) webhooks.value[idx] = updated;
  } catch {
    errorMessage.value = 'Failed to update webhook. Please try again.';
  } finally {
    togglingId.value = null;
  }
}

function onWebhookSaved(saved: WebhookResponse): void {
  const idx = webhooks.value.findIndex((w) => w.id === saved.id);
  if (idx >= 0) {
    webhooks.value[idx] = saved;
  } else {
    webhooks.value.push(saved);
  }
}

async function onDeleteWebhook(): Promise<void> {
  if (!deletingWebhook.value) return;
  isDeleting.value = true;
  errorMessage.value = null;
  try {
    if (props.envApiKey) {
      await deleteEnvWebhook(props.envApiKey, deletingWebhook.value.id);
    } else if (props.orgId !== undefined) {
      await deleteOrgWebhook(props.orgId, deletingWebhook.value.id);
    }
    webhooks.value = webhooks.value.filter((w) => w.id !== deletingWebhook.value!.id);
    showDeleteDialog.value = false;
    deletingWebhook.value = null;
  } catch {
    errorMessage.value = 'Failed to delete webhook. Please try again.';
  } finally {
    isDeleting.value = false;
  }
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
}
</script>
