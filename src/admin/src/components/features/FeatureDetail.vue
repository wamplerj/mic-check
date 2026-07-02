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
        <div class="d-flex align-center">
          {{ feature.name }}
          <v-chip
            :color="feature.type === 'MULTIVARIATE' ? 'secondary' : 'primary'"
            size="x-small"
            variant="tonal"
            style="ml-4"
          >
            {{ feature.type }}
          </v-chip>
        </div>
        <v-btn icon="ri-close-line" variant="text" size="small" @click="$emit('update:modelValue', false)" />
      </v-card-title>

      <v-tabs v-model="activeTab" color="primary" class="px-4">
        <v-tab value="value" data-testid="tab-value">Value</v-tab>
        <v-tab value="segments" data-testid="tab-segments">Segments</v-tab>
        <v-tab value="settings" data-testid="tab-settings">Settings</v-tab>
      </v-tabs>
      <v-divider />

      <v-card-text class="pa-0" style="height: 580px; overflow-y: auto;">
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
              <v-icon size="32" class="mb-2">ri-server-line</v-icon>
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
              <TagsChipInput v-if="feature" :feature="feature" @updated="onTagsUpdated" />
            </template>
          </v-tabs-window-item>

          <!-- ── Segments tab ─────────────────────────────────────────── -->
          <v-tabs-window-item value="segments" class="pa-6">
            <v-alert
              v-if="segmentsErrorMessage"
              type="error"
              variant="tonal"
              density="compact"
              closable
              class="mb-4"
              @click:close="segmentsErrorMessage = null"
            >
              {{ segmentsErrorMessage }}
            </v-alert>

            <div v-if="!featureState" class="text-center py-6 text-medium-emphasis">
              <v-icon size="32" class="mb-2">ri-server-line</v-icon>
              <p>Select an environment to manage segment overrides.</p>
            </div>

            <template v-else>
              <!-- Existing segment overrides -->
              <div v-if="featureSegments.length > 0" class="d-flex flex-column ga-3 mb-4">
                <v-card
                  v-for="fsg in featureSegments"
                  :key="fsg.id"
                  variant="outlined"
                  rounded="lg"
                  :data-testid="`feature-segment-${fsg.id}`"
                >
                  <v-card-text class="pa-3">
                    <div class="d-flex align-center justify-space-between">
                      <div class="d-flex align-center ga-2">
                        <v-icon size="18" color="primary">ri-group-line</v-icon>
                        <span class="text-body-2 font-weight-medium">{{ fsg.segmentName }}</span>
                        <v-chip size="x-small" variant="tonal" color="secondary">
                          Priority {{ fsg.priority }}
                        </v-chip>
                      </div>
                      <div class="d-flex align-center ga-1">
                        <v-switch
                          :model-value="fsg.enabled ?? false"
                          color="primary"
                          hide-details
                          density="compact"
                          :data-testid="`segment-enabled-toggle-${fsg.id}`"
                          @update:model-value="onUpdateSegmentEnabled(fsg, $event)"
                        />
                        <v-btn
                          icon="ri-delete-bin-line"
                          size="x-small"
                          variant="text"
                          color="error"
                          :data-testid="`remove-segment-${fsg.id}`"
                          @click="onRemoveSegment(fsg.id)"
                        />
                      </div>
                    </div>
                    <div v-if="fsg.value !== null" class="mt-2">
                      <v-text-field
                        :model-value="fsg.value"
                        label="Value"
                        variant="outlined"
                        density="compact"
                        hide-details
                        class="mt-1"
                        :data-testid="`segment-value-${fsg.id}`"
                        @update:model-value="onUpdateSegmentValue(fsg, $event)"
                      />
                    </div>
                  </v-card-text>
                </v-card>
              </div>

              <p v-else class="text-body-2 text-medium-emphasis mb-4">
                No segment overrides configured for this environment.
              </p>

              <!-- Add segment form -->
              <v-card variant="outlined" rounded="lg" class="pa-4" data-testid="add-segment-form">
                <p class="text-body-2 font-weight-medium mb-3">Add segment override</p>
                <v-select
                  v-model="newSegmentId"
                  :items="availableSegments"
                  item-title="name"
                  item-value="id"
                  label="Segment"
                  variant="outlined"
                  density="compact"
                  class="mb-3"
                  no-data-text="All segments already configured"
                  data-testid="new-segment-select"
                />
                <div class="d-flex align-center ga-3 mb-3">
                  <v-switch
                    v-model="newSegmentEnabled"
                    color="primary"
                    label="Enabled"
                    hide-details
                    density="compact"
                    data-testid="new-segment-enabled"
                  />
                  <v-text-field
                    v-model="newSegmentPriority"
                    label="Priority"
                    type="number"
                    variant="outlined"
                    density="compact"
                    hide-details
                    style="max-width: 100px"
                    data-testid="new-segment-priority"
                  />
                </div>
                <div class="d-flex justify-end">
                  <v-btn
                    color="primary"
                    variant="flat"
                    size="small"
                    :disabled="!newSegmentId"
                    :loading="isAddingSegment"
                    data-testid="add-segment-btn"
                    @click="onAddSegment"
                  >
                    Add override
                  </v-btn>
                </div>
              </v-card>
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
              <v-text-field
                v-model="deleteConfirmName"
                :label="`Type '${feature?.name}' to confirm deletion`"
                variant="outlined"
                density="compact"
                class="mb-3"
                hide-details
                data-testid="delete-confirm-input"
              />
              <div class="d-flex justify-space-between align-center">
                <v-btn
                  color="error"
                  variant="text"
                  size="small"
                  :loading="isDeleting"
                  :disabled="deleteConfirmName !== feature?.name"
                  data-testid="delete-feature-btn"
                  @click="onDelete"
                >
                  Delete feature
                </v-btn>
                <v-btn
                  color="primary"
                  variant="flat"
                  size="small"
                  :loading="isSavingSettings"
                  data-testid="save-settings-btn"
                  @click="onSaveSettings"
                >
                  Save changes
                </v-btn>
              </div>
            </v-form>
          </v-tabs-window-item>
        </v-tabs-window>
      </v-card-text>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import { patchFeature, deleteFeature } from '@/api/features';
import { patchFeatureState } from '@/api/featureStates';
import { listFeatureSegments, createFeatureSegment, updateFeatureSegment, deleteFeatureSegment } from '@/api/featureSegments';
import { listSegments } from '@/api/segments';
import { useContextStore } from '@/stores/context';
import FeatureValueEditor from './FeatureValueEditor.vue';
import TagsChipInput from './TagsChipInput.vue';
import type { FeatureResponse, FeatureStateResponse, FeatureSegmentResponse, SegmentResponse } from '@/types/api';

interface Props {
  modelValue: boolean;
  feature: FeatureResponse | null;
  featureState: FeatureStateResponse | null;
  initialTab?: string;
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
const editedValue = ref<string | null>(null);
const settingsFormRef = ref<{ validate: () => Promise<{ valid: boolean }> } | null>(null);
const valueErrorMessage = ref<string | null>(null);
const settingsErrorMessage = ref<string | null>(null);
const segmentsErrorMessage = ref<string | null>(null);

// Segments tab state
const featureSegments = ref<FeatureSegmentResponse[]>([]);
const allProjectSegments = ref<SegmentResponse[]>([]);
const newSegmentId = ref<number | null>(null);
const newSegmentEnabled = ref(true);
const newSegmentPriority = ref(1);
const isAddingSegment = ref(false);

const availableSegments = computed(() =>
  allProjectSegments.value.filter(
    (s) => !featureSegments.value.some((fsg) => fsg.segmentId === s.id),
  ),
);

async function loadSegmentsData(): Promise<void> {
  const envApiKey = contextStore.currentEnvironment?.apiKey;
  const projectId = contextStore.currentProject?.id;
  if (!envApiKey || !props.feature || !projectId) return;
  try {
    const [fsegs, segs] = await Promise.all([
      listFeatureSegments(envApiKey, props.feature.id),
      listSegments(projectId),
    ]);
    featureSegments.value = fsegs;
    allProjectSegments.value = segs;
  } catch {
    segmentsErrorMessage.value = 'Failed to load segment overrides.';
  }
}

async function onAddSegment(): Promise<void> {
  const envApiKey = contextStore.currentEnvironment?.apiKey;
  if (!envApiKey || !props.feature || !newSegmentId.value) return;
  isAddingSegment.value = true;
  segmentsErrorMessage.value = null;
  try {
    const created = await createFeatureSegment(envApiKey, props.feature.id, {
      segmentId: newSegmentId.value,
      priority: newSegmentPriority.value,
      enabled: newSegmentEnabled.value,
      value: null,
    });
    featureSegments.value = [...featureSegments.value, created];
    newSegmentId.value = null;
    newSegmentEnabled.value = true;
    newSegmentPriority.value = featureSegments.value.length;
  } catch {
    segmentsErrorMessage.value = 'Failed to add segment override.';
  } finally {
    isAddingSegment.value = false;
  }
}

function onUpdateSegmentEnabled(fsg: FeatureSegmentResponse, v: boolean | null): void {
  onUpdateSegment(fsg, { enabled: !!v });
}

function onUpdateSegmentValue(fsg: FeatureSegmentResponse, val: string): void {
  onUpdateSegment(fsg, { value: val || null });
}

async function onUpdateSegment(
  fsg: FeatureSegmentResponse,
  patch: { enabled?: boolean; value?: string | null },
): Promise<void> {
  const envApiKey = contextStore.currentEnvironment?.apiKey;
  if (!envApiKey || !props.feature) return;
  try {
    const updated = await updateFeatureSegment(envApiKey, props.feature.id, fsg.id, {
      priority: fsg.priority,
      enabled: patch.enabled ?? fsg.enabled ?? false,
      value: patch.value !== undefined ? patch.value : fsg.value,
    });
    featureSegments.value = featureSegments.value.map((f) => (f.id === fsg.id ? updated : f));
  } catch {
    segmentsErrorMessage.value = 'Failed to update segment override.';
  }
}

async function onRemoveSegment(id: number): Promise<void> {
  const envApiKey = contextStore.currentEnvironment?.apiKey;
  if (!envApiKey || !props.feature) return;
  try {
    await deleteFeatureSegment(envApiKey, props.feature.id, id);
    featureSegments.value = featureSegments.value.filter((f) => f.id !== id);
  } catch {
    segmentsErrorMessage.value = 'Failed to remove segment override.';
  }
}

const settingsForm = ref({
  name: '',
  description: '',
  defaultEnabled: false,
});

const deleteConfirmName = ref('');

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
  }
}

// Sync form when feature changes
watch(() => props.feature, syncSettingsForm, { immediate: true });

// Sync edited value when feature state changes
watch(
  () => props.featureState,
  (s: FeatureStateResponse | null) => {
    editedValue.value = s?.value ?? null;
  },
  { immediate: true },
);

// Reset UI state each time the dialog opens
watch(
  () => props.modelValue,
  (open) => {
    if (open) {
      activeTab.value = props.initialTab ?? 'value';
      valueErrorMessage.value = null;
      settingsErrorMessage.value = null;
      segmentsErrorMessage.value = null;
      featureSegments.value = [];
      newSegmentId.value = null;
      newSegmentEnabled.value = true;
      newSegmentPriority.value = 1;
      deleteConfirmName.value = '';
      syncSettingsForm(props.feature);

      editedValue.value = props.featureState?.value ?? null;
      loadSegmentsData();
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

function onTagsUpdated(updated: FeatureResponse): void {
  emit('updated', updated);
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
