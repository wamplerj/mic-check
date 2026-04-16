<template>
  <v-dialog
    :model-value="modelValue"
    max-width="620"
    scrollable
    @update:model-value="$emit('update:modelValue', $event)"
  >
    <v-card v-if="feature" rounded="lg" data-testid="feature-detail">
      <!-- Header -->
      <v-card-title class="d-flex align-center justify-space-between pa-6 pb-3">
        <div class="d-flex align-center ga-2">
          <v-chip
            :color="feature.type === 'MULTIVARIATE' ? 'secondary' : 'primary'"
            size="small"
            variant="tonal"
          >
            {{ feature.type }}
          </v-chip>
          <span class="text-h6">{{ feature.name }}</span>
        </div>
        <v-btn icon="mdi-close" variant="text" size="small" @click="$emit('update:modelValue', false)" />
      </v-card-title>

      <v-tabs v-model="activeTab" color="primary" class="px-4">
        <v-tab value="value" data-testid="tab-value">Value</v-tab>
        <v-tab value="settings" data-testid="tab-settings">Settings</v-tab>
      </v-tabs>
      <v-divider />

      <v-card-text class="pa-0">
        <v-tabs-window v-model="activeTab">
          <!-- ── Value tab ─────────────────────────────────────────────── -->
          <v-tabs-window-item value="value" class="pa-6">
            <v-alert
              v-if="valueErrorMessage"
              type="error"
              variant="tonal"
              density="compact"
              closable
              class="mb-4"
              @click:close="valueErrorMessage = null"
            >
              {{ valueErrorMessage }}
            </v-alert>

            <div v-if="!featureState" class="text-center py-6 text-medium-emphasis">
              <v-icon size="32" class="mb-2">mdi-server-outline</v-icon>
              <p>Select an environment to manage feature state.</p>
            </div>

            <template v-else>
              <!-- Enabled toggle -->
              <div class="d-flex align-center justify-space-between mb-6">
                <div>
                  <p class="text-body-1 font-weight-medium mb-1">Enabled</p>
                  <p class="text-body-2 text-medium-emphasis">
                    {{ featureState.enabled ? 'On' : 'Off' }} in
                    <strong>{{ contextStore.currentEnvironment?.name }}</strong>
                  </p>
                </div>
                <v-switch
                  :model-value="featureState.enabled"
                  color="primary"
                  hide-details
                  :loading="isTogglingEnabled"
                  data-testid="feature-enabled-toggle"
                  @update:model-value="onToggleEnabled"
                />
              </div>

              <v-divider class="mb-5" />

              <!-- Value editor -->
              <p class="text-body-2 font-weight-medium mb-2">Value</p>
              <FeatureValueEditor
                v-model="editedValue"
                class="mb-4"
              />
              <div class="d-flex justify-end">
                <v-btn
                  color="primary"
                  variant="flat"
                  size="small"
                  :loading="isSavingValue"
                  :disabled="editedValue === featureState.value"
                  data-testid="save-value-btn"
                  @click="onSaveValue"
                >
                  Save value
                </v-btn>
              </div>

              <v-divider class="my-5" />

              <!-- Tags -->
              <TagsChipInput />
            </template>
          </v-tabs-window-item>

          <!-- ── Settings tab ──────────────────────────────────────────── -->
          <v-tabs-window-item value="settings" class="pa-6">
            <v-alert
              v-if="settingsErrorMessage"
              type="error"
              variant="tonal"
              density="compact"
              closable
              class="mb-4"
              @click:close="settingsErrorMessage = null"
            >
              {{ settingsErrorMessage }}
            </v-alert>

            <v-form ref="settingsFormRef" data-testid="settings-form">
              <v-text-field
                v-model="settingsForm.name"
                label="Name"
                variant="outlined"
                density="comfortable"
                class="mb-3"
                :rules="[rules.required, rules.nameFormat]"
                :counter="150"
                data-testid="settings-name-input"
              />
              <v-textarea
                v-model="settingsForm.description"
                label="Description"
                variant="outlined"
                density="comfortable"
                rows="2"
                class="mb-3"
                data-testid="settings-description-input"
              />
              <div class="d-flex align-center justify-space-between mb-4">
                <div>
                  <p class="text-body-2 font-weight-medium">Default enabled</p>
                  <p class="text-caption text-medium-emphasis">Applies to new environments</p>
                </div>
                <v-switch
                  v-model="settingsForm.defaultEnabled"
                  color="primary"
                  hide-details
                  data-testid="settings-default-enabled-toggle"
                />
              </div>
              <div class="d-flex justify-end mb-6">
                <v-btn
                  color="primary"
                  variant="flat"
                  size="small"
                  :loading="isSavingSettings"
                  data-testid="save-settings-btn"
                  @click="onSaveSettings"
                >
                  Save settings
                </v-btn>
              </div>
            </v-form>

            <!-- Danger zone -->
            <v-card variant="outlined" color="error" rounded="lg">
              <v-card-title class="text-body-1 text-error pa-4 pb-2">Danger Zone</v-card-title>
              <v-card-text class="pa-4 pt-0">
                <p class="text-body-2 mb-3">
                  Permanently delete this feature flag. This cannot be undone and will remove
                  the flag from all environments.
                </p>
                <p class="text-body-2 mb-2">
                  Type <strong>{{ feature.name }}</strong> to confirm:
                </p>
                <v-text-field
                  v-model="deleteConfirmName"
                  variant="outlined"
                  density="compact"
                  hide-details
                  :placeholder="feature.name"
                  class="mb-3"
                  data-testid="delete-confirm-input"
                />
                <v-btn
                  color="error"
                  variant="flat"
                  size="small"
                  :disabled="deleteConfirmName !== feature.name"
                  :loading="isDeleting"
                  data-testid="delete-feature-btn"
                  @click="onDelete"
                >
                  Delete feature
                </v-btn>
              </v-card-text>
            </v-card>
          </v-tabs-window-item>
        </v-tabs-window>
      </v-card-text>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { patchFeature, deleteFeature } from '@/api/features';
import { patchFeatureState } from '@/api/featureStates';
import { useContextStore } from '@/stores/context';
import FeatureValueEditor from './FeatureValueEditor.vue';
import TagsChipInput from './TagsChipInput.vue';
import type { FeatureResponse, FeatureStateResponse } from '@/types/api';

interface Props {
  modelValue: boolean;
  feature: FeatureResponse | null;
  featureState: FeatureStateResponse | null;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  updated: [feature: FeatureResponse];
  deleted: [featureId: number];
  stateUpdated: [state: FeatureStateResponse];
}>();

const contextStore = useContextStore();

const activeTab = ref('value');
const isTogglingEnabled = ref(false);
const isSavingValue = ref(false);
const isSavingSettings = ref(false);
const isDeleting = ref(false);
const deleteConfirmName = ref('');
const editedValue = ref<string | null>(null);
const settingsFormRef = ref<{ validate: () => Promise<{ valid: boolean }> } | null>(null);
const valueErrorMessage = ref<string | null>(null);
const settingsErrorMessage = ref<string | null>(null);

const settingsForm = ref({
  name: '',
  description: '',
  defaultEnabled: false,
});

const rules = {
  required: (v: string) => !!v || 'Required',
  nameFormat: (v: string) =>
    /^[a-zA-Z0-9_-]+$/.test(v) || 'Only letters, numbers, hyphens and underscores',
};

function syncSettingsForm(f: FeatureResponse | null): void {
  if (f) {
    settingsForm.value = {
      name: f.name,
      description: f.description ?? '',
      defaultEnabled: f.defaultEnabled,
    };
    deleteConfirmName.value = '';
  }
}

// Sync form when feature changes
watch(() => props.feature, syncSettingsForm, { immediate: true });

// Sync edited value when feature state changes
watch(
  () => props.featureState,
  (s) => {
    editedValue.value = s?.value ?? null;
  },
  { immediate: true },
);

// Reset UI state each time the dialog opens
watch(
  () => props.modelValue,
  (open) => {
    if (open) {
      activeTab.value = 'value';
      valueErrorMessage.value = null;
      settingsErrorMessage.value = null;
      syncSettingsForm(props.feature);
      editedValue.value = props.featureState?.value ?? null;
    }
  },
);

async function onToggleEnabled(enabled: boolean | null): Promise<void> {
  if (!props.featureState || enabled === null) return;
  const envApiKey = contextStore.currentEnvironment?.apiKey;
  if (!envApiKey) return;
  isTogglingEnabled.value = true;
  valueErrorMessage.value = null;
  try {
    const updated = await patchFeatureState(envApiKey, props.featureState.id, { enabled });
    emit('stateUpdated', updated);
  } catch {
    valueErrorMessage.value = 'Failed to update enabled state. Please try again.';
  } finally {
    isTogglingEnabled.value = false;
  }
}

async function onSaveValue(): Promise<void> {
  if (!props.featureState) return;
  const envApiKey = contextStore.currentEnvironment?.apiKey;
  if (!envApiKey) return;

  if (editedValue.value) {
    const trimmed = editedValue.value.trim();
    if (trimmed.startsWith('{') || trimmed.startsWith('[')) {
      try {
        JSON.parse(trimmed);
      } catch {
        valueErrorMessage.value = 'Value contains invalid JSON.';
        return;
      }
    }
  }

  isSavingValue.value = true;
  valueErrorMessage.value = null;
  try {
    const updated = await patchFeatureState(envApiKey, props.featureState.id, {
      value: editedValue.value,
    });
    emit('stateUpdated', updated);
  } catch {
    valueErrorMessage.value = 'Failed to save value. Please try again.';
  } finally {
    isSavingValue.value = false;
  }
}

async function onSaveSettings(): Promise<void> {
  if (!settingsFormRef.value) return;
  const { valid } = await settingsFormRef.value.validate();
  if (!valid || !props.feature) return;
  const projectId = contextStore.currentProject?.id;
  if (!projectId) return;
  isSavingSettings.value = true;
  settingsErrorMessage.value = null;
  try {
    const updated = await patchFeature(projectId, props.feature.id, {
      name: settingsForm.value.name,
      description: settingsForm.value.description || null,
      defaultEnabled: settingsForm.value.defaultEnabled,
    });
    emit('updated', updated);
  } catch {
    settingsErrorMessage.value = 'Failed to save settings. Please try again.';
  } finally {
    isSavingSettings.value = false;
  }
}

async function onDelete(): Promise<void> {
  if (!props.feature || deleteConfirmName.value !== props.feature.name) return;
  const projectId = contextStore.currentProject?.id;
  if (!projectId) return;
  isDeleting.value = true;
  settingsErrorMessage.value = null;
  try {
    await deleteFeature(projectId, props.feature.id);
    emit('deleted', props.feature.id);
    emit('update:modelValue', false);
  } catch {
    settingsErrorMessage.value = 'Failed to delete feature. Please try again.';
  } finally {
    isDeleting.value = false;
  }
}
</script>
