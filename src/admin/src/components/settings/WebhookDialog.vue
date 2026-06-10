<template>
  <v-dialog
    :model-value="modelValue"
    max-width="500"
    @update:model-value="$emit('update:modelValue', $event)"
  >
    <v-card rounded="lg" data-testid="webhook-dialog">
      <v-card-title class="d-flex align-center justify-space-between pa-6 pb-3">
        <span class="text-h6">{{ webhook ? 'Edit Webhook' : 'Add Webhook' }}</span>
        <v-btn icon="ri-close-line" variant="text" size="small" @click="$emit('update:modelValue', false)" />
      </v-card-title>
      <v-divider />

      <v-card-text class="pa-6">
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

        <v-form ref="formRef" data-testid="webhook-form">
          <v-text-field
            v-model="form.url"
            label="Endpoint URL"
            variant="outlined"
            density="comfortable"
            class="mb-3"
            :rules="[rules.required, rules.url]"
            placeholder="https://example.com/webhook"
            data-testid="webhook-url-input"
          />
          <v-text-field
            v-model="form.secret"
            label="Secret (optional)"
            variant="outlined"
            density="comfortable"
            class="mb-3"
            :type="showSecret ? 'text' : 'password'"
            :append-inner-icon="showSecret ? 'ri-eye-off-line' : 'ri-eye-line'"
            hint="Used to sign the webhook payload"
            data-testid="webhook-secret-input"
            @click:append-inner="showSecret = !showSecret"
          />
          <div class="d-flex align-center justify-space-between">
            <div>
              <p class="text-body-2 font-weight-medium mb-0">Enabled</p>
              <p class="text-caption text-medium-emphasis">Deliver events to this endpoint</p>
            </div>
            <v-switch
              v-model="form.enabled"
              color="primary"
              hide-details
              data-testid="webhook-enabled-toggle"
            />
          </div>
        </v-form>
      </v-card-text>

      <v-divider />
      <v-card-actions class="pa-4">
        <v-spacer />
        <v-btn variant="text" @click="$emit('update:modelValue', false)">Cancel</v-btn>
        <v-btn
          color="primary"
          variant="flat"
          :loading="isSaving"
          data-testid="save-webhook-btn"
          @click="onSave"
        >
          {{ webhook ? 'Save' : 'Add' }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { createOrgWebhook, updateOrgWebhook, createEnvWebhook, updateEnvWebhook } from '@/api/webhooks';
import type { WebhookResponse } from '@/types/api';

interface Props {
  modelValue: boolean;
  webhook?: WebhookResponse | null;
  orgId?: number;
  envApiKey?: string;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  saved: [webhook: WebhookResponse];
}>();

const formRef = ref<{ validate: () => Promise<{ valid: boolean }> } | null>(null);
const isSaving = ref(false);
const errorMessage = ref<string | null>(null);
const showSecret = ref(false);

const form = ref({ url: '', secret: '', enabled: true });

const rules = {
  required: (v: string) => !!v || 'Required',
  url: (v: string) => {
    try { new URL(v); return true; } catch { return 'Must be a valid URL'; }
  },
};

function resetForm(): void {
  form.value = {
    url: props.webhook?.url ?? '',
    secret: props.webhook?.secret ?? '',
    enabled: props.webhook?.enabled ?? true,
  };
  errorMessage.value = null;
  showSecret.value = false;
}

watch(() => props.modelValue, (open) => {
  if (open) resetForm();
});

watch(() => props.webhook, resetForm, { immediate: true });

async function onSave(): Promise<void> {
  if (!formRef.value) return;
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  isSaving.value = true;
  errorMessage.value = null;
  const request = {
    url: form.value.url,
    secret: form.value.secret || null,
    enabled: form.value.enabled,
  };

  try {
    let saved: WebhookResponse;
    if (props.webhook) {
      if (props.orgId !== undefined) {
        saved = await updateOrgWebhook(props.orgId, props.webhook.id, request);
      } else if (props.envApiKey) {
        saved = await updateEnvWebhook(props.envApiKey, props.webhook.id, request);
      } else {
        throw new Error('Missing scope');
      }
    } else {
      if (props.orgId !== undefined) {
        saved = await createOrgWebhook(props.orgId, request);
      } else if (props.envApiKey) {
        saved = await createEnvWebhook(props.envApiKey, request);
      } else {
        throw new Error('Missing scope');
      }
    }
    emit('saved', saved);
    emit('update:modelValue', false);
  } catch {
    errorMessage.value = 'Failed to save webhook. Please try again.';
  } finally {
    isSaving.value = false;
  }
}
</script>
