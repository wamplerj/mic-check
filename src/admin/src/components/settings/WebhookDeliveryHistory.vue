<template>
  <div data-testid="webhook-delivery-history">
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
      <div v-if="deliveries.length === 0" class="text-center py-6 text-medium-emphasis text-body-2" data-testid="no-deliveries-message">
        No delivery history for this webhook.
      </div>

      <v-table v-else density="compact" data-testid="deliveries-table">
        <thead>
          <tr>
            <th>Timestamp</th>
            <th>Event</th>
            <th style="width: 100px">Status</th>
            <th style="width: 80px">Attempt</th>
            <th style="width: 100px">HTTP Code</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="delivery in deliveries" :key="delivery.id" :data-testid="`delivery-row-${delivery.id}`">
            <td class="text-caption">{{ formatDate(delivery.attemptedAt) }}</td>
            <td class="text-body-2">{{ delivery.eventType }}</td>
            <td>
              <v-chip
                :color="delivery.success ? 'success' : 'error'"
                size="x-small"
                variant="tonal"
                :data-testid="`delivery-status-${delivery.id}`"
              >
                {{ delivery.success ? 'Success' : 'Failed' }}
              </v-chip>
            </td>
            <td class="text-body-2 text-medium-emphasis">{{ delivery.attemptNumber }}</td>
            <td class="text-body-2 text-medium-emphasis">{{ delivery.responseStatusCode ?? '—' }}</td>
          </tr>
        </tbody>
      </v-table>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';
import { listWebhookDeliveries } from '@/api/webhooks';
import type { WebhookDeliveryLogResponse } from '@/types/api';

interface Props {
  envApiKey: string;
  webhookId: number;
}

const props = defineProps<Props>();

const deliveries = ref<WebhookDeliveryLogResponse[]>([]);
const isLoading = ref(false);
const errorMessage = ref<string | null>(null);

async function loadDeliveries(): Promise<void> {
  isLoading.value = true;
  errorMessage.value = null;
  try {
    deliveries.value = await listWebhookDeliveries(props.envApiKey, props.webhookId);
  } catch {
    deliveries.value = [];
    errorMessage.value = 'Failed to load delivery history. Please try again.';
  } finally {
    isLoading.value = false;
  }
}

watch(() => props.webhookId, loadDeliveries);

onMounted(loadDeliveries);

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString(undefined, {
    month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit',
  });
}
</script>
