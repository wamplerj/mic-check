<template>
  <v-dialog
    :model-value="modelValue"
    max-width="680"
    scrollable
    @update:model-value="$emit('update:modelValue', $event)"
  >
    <v-card v-if="identity" rounded="lg" data-testid="identity-detail">
      <!-- Header -->
      <v-card-title class="d-flex align-center justify-space-between pa-6 pb-3">
        <div>
          <p class="text-h6 mb-0">{{ identity.identifier }}</p>
          <p class="text-caption text-medium-emphasis mb-0">ID: {{ identity.id }}</p>
        </div>
        <v-btn icon="mdi-close" variant="text" size="small" @click="$emit('update:modelValue', false)" />
      </v-card-title>

      <v-tabs v-model="activeTab" color="primary" class="px-4">
        <v-tab value="traits" data-testid="tab-traits">Traits</v-tab>
        <v-tab value="overrides" data-testid="tab-overrides">Feature Overrides</v-tab>
        <v-tab value="segments" data-testid="tab-segments">Segments</v-tab>
      </v-tabs>
      <v-divider />

      <v-card-text class="pa-0">
        <v-tabs-window v-model="activeTab">
          <!-- ── Traits tab ────────────────────────────────────────────── -->
          <v-tabs-window-item value="traits" class="pa-6">
            <v-alert
              v-if="traitsError"
              type="error"
              variant="tonal"
              density="compact"
              closable
              class="mb-4"
              @click:close="traitsError = null"
            >
              {{ traitsError }}
            </v-alert>

            <div v-if="editedTraits.length === 0" class="text-center py-4 text-medium-emphasis text-body-2 mb-4" data-testid="no-traits-message">
              No traits recorded for this identity.
            </div>

            <v-table v-else density="compact" class="mb-4" data-testid="traits-table">
              <thead>
                <tr>
                  <th style="width: 40%">Key</th>
                  <th>Value</th>
                  <th style="width: 40px"></th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(trait, i) in editedTraits" :key="trait.key" :data-testid="`trait-row-${trait.key}`">
                  <td class="text-body-2 font-weight-medium">{{ trait.key }}</td>
                  <td>
                    <v-text-field
                      v-model="editedTraits[Number(i)].value"
                      variant="underlined"
                      density="compact"
                      hide-details
                      :loading="savingTraitKey === trait.key"
                      :data-testid="`trait-value-${trait.key}`"
                      @blur="onSaveTrait(trait.key, editedTraits[Number(i)].value)"
                    />
                  </td>
                  <td>
                    <v-btn
                      icon="mdi-trash-can-outline"
                      size="x-small"
                      variant="text"
                      color="error"
                      :loading="deletingTraitKey === trait.key"
                      :data-testid="`delete-trait-${trait.key}`"
                      @click="onDeleteTrait(trait.key)"
                    />
                  </td>
                </tr>
              </tbody>
            </v-table>

            <!-- Add trait form -->
            <v-divider class="mb-4" />
            <p class="text-body-2 font-weight-medium mb-3">Add Trait</p>
            <div class="d-flex align-center ga-3 flex-wrap">
              <v-text-field
                v-model="newTraitKey"
                label="Key"
                variant="outlined"
                density="compact"
                hide-details
                style="min-width: 140px; flex: 1"
                data-testid="new-trait-key-input"
              />
              <v-text-field
                v-model="newTraitValue"
                label="Value"
                variant="outlined"
                density="compact"
                hide-details
                style="min-width: 140px; flex: 1"
                data-testid="new-trait-value-input"
              />
              <v-btn
                color="primary"
                variant="flat"
                size="small"
                :disabled="!newTraitKey.trim()"
                :loading="isAddingTrait"
                data-testid="add-trait-btn"
                @click="onAddTrait"
              >
                Add
              </v-btn>
            </div>

            <!-- Danger zone -->
            <v-card variant="outlined" color="error" rounded="lg" class="mt-6">
              <v-card-title class="text-body-1 text-error pa-4 pb-2">Danger Zone</v-card-title>
              <v-card-text class="pa-4 pt-0">
                <p class="text-body-2 mb-3">
                  Permanently delete this identity and all its traits and feature overrides.
                  This cannot be undone.
                </p>
                <v-btn
                  color="error"
                  variant="flat"
                  size="small"
                  :loading="isDeleting"
                  data-testid="delete-identity-btn"
                  @click="onDeleteIdentity"
                >
                  Delete identity
                </v-btn>
              </v-card-text>
            </v-card>
          </v-tabs-window-item>

          <!-- ── Overrides tab ─────────────────────────────────────────── -->
          <v-tabs-window-item value="overrides" class="pa-6">
            <v-alert
              v-if="overridesError"
              type="error"
              variant="tonal"
              density="compact"
              closable
              class="mb-4"
              @click:close="overridesError = null"
            >
              {{ overridesError }}
            </v-alert>

            <div v-if="isLoadingOverrides" class="d-flex justify-center py-6">
              <v-progress-circular indeterminate color="primary" />
            </div>

            <template v-else>
              <!-- Existing overrides -->
              <div v-if="overrides.length === 0" class="text-center py-4 text-medium-emphasis text-body-2 mb-4" data-testid="no-overrides-message">
                No feature overrides for this identity.
              </div>

              <v-table v-else density="compact" class="mb-4" data-testid="overrides-table">
                <thead>
                  <tr>
                    <th>Feature</th>
                    <th style="width: 80px">Enabled</th>
                    <th>Value</th>
                    <th style="width: 40px"></th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="override in overrides" :key="override.id" :data-testid="`override-row-${override.featureId}`">
                    <td class="text-body-2 font-weight-medium">{{ featureNameMap.get(override.featureId) ?? `Feature ${override.featureId}` }}</td>
                    <td>
                      <v-switch
                        :model-value="override.enabled"
                        color="primary"
                        hide-details
                        density="compact"
                        :data-testid="`override-toggle-${override.featureId}`"
                        @update:model-value="onToggleOverride(override, $event)"
                      />
                    </td>
                    <td>
                      <v-text-field
                        :model-value="override.value ?? ''"
                        variant="underlined"
                        density="compact"
                        hide-details
                        placeholder="(none)"
                        :data-testid="`override-value-${override.featureId}`"
                        @update:model-value="onUpdateOverrideValue(override, $event)"
                      />
                    </td>
                    <td>
                      <v-btn
                        icon="mdi-trash-can-outline"
                        size="x-small"
                        variant="text"
                        color="error"
                        :data-testid="`remove-override-${override.featureId}`"
                        @click="onRemoveOverride(override)"
                      />
                    </td>
                  </tr>
                </tbody>
              </v-table>

              <!-- Add Override -->
              <v-divider class="mb-4" />
              <p class="text-body-2 font-weight-medium mb-3">Add Override</p>
              <div class="d-flex align-center ga-3 flex-wrap">
                <v-select
                  v-model="newOverrideFeatureId"
                  :items="availableFeatures"
                  item-title="name"
                  item-value="id"
                  label="Select feature"
                  variant="outlined"
                  density="compact"
                  hide-details
                  style="min-width: 200px; flex: 2"
                  data-testid="add-override-feature-select"
                />
                <v-switch
                  v-model="newOverrideEnabled"
                  color="primary"
                  label="Enabled"
                  hide-details
                  density="compact"
                  data-testid="add-override-enabled-toggle"
                />
                <v-text-field
                  v-model="newOverrideValue"
                  label="Value (optional)"
                  variant="outlined"
                  density="compact"
                  hide-details
                  style="min-width: 140px; flex: 1"
                  data-testid="add-override-value-input"
                />
                <v-btn
                  color="primary"
                  variant="flat"
                  size="small"
                  :disabled="!newOverrideFeatureId"
                  :loading="isAddingOverride"
                  data-testid="add-override-btn"
                  @click="onAddOverride"
                >
                  Add
                </v-btn>
              </div>
            </template>
          </v-tabs-window-item>
          <!-- ── Segments tab ─────────────────────────────────────────── -->
          <v-tabs-window-item value="segments" class="pa-6">
            <v-alert
              v-if="segmentsError"
              type="error"
              variant="tonal"
              density="compact"
              closable
              class="mb-4"
              @click:close="segmentsError = null"
            >
              {{ segmentsError }}
            </v-alert>

            <div v-if="isLoadingSegments" class="d-flex justify-center py-6">
              <v-progress-circular indeterminate color="primary" />
            </div>

            <template v-else>
              <p class="text-body-2 text-medium-emphasis mb-4">
                Segments this identity belongs to are determined automatically by evaluating its traits against each segment's rules.
              </p>

              <div v-if="identitySegments.length === 0" class="text-center py-6 text-medium-emphasis" data-testid="no-segments-message">
                <v-icon size="32" class="mb-2">mdi-label-off-outline</v-icon>
                <p class="text-body-2">This identity does not match any segments.</p>
              </div>

              <div v-else class="d-flex flex-wrap ga-2" data-testid="segments-list">
                <v-chip
                  v-for="segment in identitySegments"
                  :key="segment.id"
                  color="primary"
                  variant="tonal"
                  size="small"
                  :data-testid="`segment-chip-${segment.id}`"
                >
                  {{ segment.name }}
                </v-chip>
              </div>
            </template>
          </v-tabs-window-item>
        </v-tabs-window>
      </v-card-text>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { getIdentityFeatureStates, setIdentityFeatureState, deleteIdentity, deleteIdentityFeatureState, upsertIdentityTrait, deleteIdentityTrait } from '@/api/identities';
import { listFeatures } from '@/api/features';
import { listIdentitySegments } from '@/api/featureSegments';
import { useContextStore } from '@/stores/context';
import type { IdentityResponse, FeatureResponse, FeatureStateResponse, SegmentSummaryResponse, TraitResponse } from '@/types/api';

interface Props {
  modelValue: boolean;
  identity: IdentityResponse | null;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  deleted: [identityId: number];
}>();

const contextStore = useContextStore();

const activeTab = ref('traits');
const isDeleting = ref(false);
const traitsError = ref<string | null>(null);

// Local editable copy of traits
const editedTraits = ref<TraitResponse[]>([]);
const savingTraitKey = ref<string | null>(null);
const deletingTraitKey = ref<string | null>(null);
const newTraitKey = ref('');
const newTraitValue = ref('');
const isAddingTrait = ref(false);

const overrides = ref<FeatureStateResponse[]>([]);
const allFeatures = ref<FeatureResponse[]>([]);
const isLoadingOverrides = ref(false);
const overridesError = ref<string | null>(null);
const isAddingOverride = ref(false);

const newOverrideFeatureId = ref<number | null>(null);
const newOverrideEnabled = ref(false);
const newOverrideValue = ref('');

const identitySegments = ref<SegmentSummaryResponse[]>([]);
const isLoadingSegments = ref(false);
const segmentsError = ref<string | null>(null);

// Map featureId → name for display in overrides table
const featureNameMap = computed(() => new Map(allFeatures.value.map((f) => [f.id, f.name])));

// Only show features that don't already have an override
const availableFeatures = computed(() => {
  const overriddenIds = new Set(overrides.value.map((o) => o.featureId));
  return allFeatures.value.filter((f) => !overriddenIds.has(f.id));
});

async function loadSegmentsData(): Promise<void> {
  if (!props.identity) return;
  const envApiKey = contextStore.currentEnvironment?.apiKey;
  if (!envApiKey) return;

  isLoadingSegments.value = true;
  segmentsError.value = null;
  try {
    identitySegments.value = await listIdentitySegments(envApiKey, props.identity.id);
  } catch {
    segmentsError.value = 'Failed to load segments. Please try again.';
  } finally {
    isLoadingSegments.value = false;
  }
}

async function loadOverridesData(): Promise<void> {
  if (!props.identity) return;
  const envApiKey = contextStore.currentEnvironment?.apiKey;
  const projectId = contextStore.currentProject?.id;
  if (!envApiKey || !projectId) return;

  isLoadingOverrides.value = true;
  overridesError.value = null;
  try {
    const [states, features] = await Promise.all([
      getIdentityFeatureStates(envApiKey, props.identity.id),
      allFeatures.value.length === 0 ? listFeatures(projectId, 1, 100) : Promise.resolve(null),
    ]);
    overrides.value = states;
    if (features) allFeatures.value = features.results;
  } catch {
    overridesError.value = 'Failed to load feature overrides. Please try again.';
  } finally {
    isLoadingOverrides.value = false;
  }
}

watch(
  () => props.modelValue,
  (open) => {
    if (open) {
      activeTab.value = 'traits';
      traitsError.value = null;
      overridesError.value = null;
      segmentsError.value = null;
      overrides.value = [];
      identitySegments.value = [];
      newOverrideFeatureId.value = null;
      newOverrideEnabled.value = false;
      newOverrideValue.value = '';
      newTraitKey.value = '';
      newTraitValue.value = '';
      editedTraits.value = props.identity ? props.identity.traits.map((t: TraitResponse) => ({ ...t })) : [];
      loadOverridesData();
      loadSegmentsData();
    }
  },
  { immediate: true },
);

async function onToggleOverride(override: FeatureStateResponse, enabled: boolean | null): Promise<void> {
  if (enabled === null || !props.identity) return;
  const envApiKey = contextStore.currentEnvironment?.apiKey;
  if (!envApiKey) return;
  overridesError.value = null;
  try {
    const updated = await setIdentityFeatureState(envApiKey, props.identity.id, override.featureId, {
      enabled,
      value: override.value,
    });
    const idx = overrides.value.findIndex((o) => o.featureId === override.featureId);
    if (idx >= 0) overrides.value[idx] = updated;
  } catch {
    overridesError.value = 'Failed to update override. Please try again.';
  }
}

async function onUpdateOverrideValue(override: FeatureStateResponse, value: string): Promise<void> {
  if (!props.identity) return;
  const envApiKey = contextStore.currentEnvironment?.apiKey;
  if (!envApiKey) return;
  overridesError.value = null;
  try {
    const updated = await setIdentityFeatureState(envApiKey, props.identity.id, override.featureId, {
      enabled: override.enabled,
      value: value || null,
    });
    const idx = overrides.value.findIndex((o) => o.featureId === override.featureId);
    if (idx >= 0) overrides.value[idx] = updated;
  } catch {
    overridesError.value = 'Failed to update override value. Please try again.';
  }
}

async function onRemoveOverride(override: FeatureStateResponse): Promise<void> {
  if (!props.identity) return;
  const envApiKey = contextStore.currentEnvironment?.apiKey;
  if (!envApiKey) return;
  overridesError.value = null;
  try {
    await deleteIdentityFeatureState(envApiKey, props.identity.id, override.featureId);
    overrides.value = overrides.value.filter((o) => o.featureId !== override.featureId);
  } catch {
    overridesError.value = 'Failed to remove override. Please try again.';
  }
}

async function onAddOverride(): Promise<void> {
  if (!newOverrideFeatureId.value || !props.identity) return;
  const envApiKey = contextStore.currentEnvironment?.apiKey;
  if (!envApiKey) return;
  isAddingOverride.value = true;
  overridesError.value = null;
  try {
    const created = await setIdentityFeatureState(envApiKey, props.identity.id, newOverrideFeatureId.value, {
      enabled: newOverrideEnabled.value,
      value: newOverrideValue.value || null,
    });
    overrides.value = [...overrides.value, created];
    newOverrideFeatureId.value = null;
    newOverrideEnabled.value = false;
    newOverrideValue.value = '';
  } catch {
    overridesError.value = 'Failed to add override. Please try again.';
  } finally {
    isAddingOverride.value = false;
  }
}

async function onSaveTrait(key: string, value: string): Promise<void> {
  if (!props.identity) return;
  const envApiKey = contextStore.currentEnvironment?.apiKey;
  if (!envApiKey) return;
  savingTraitKey.value = key;
  traitsError.value = null;
  try {
    await upsertIdentityTrait(envApiKey, props.identity.id, key, { value });
    const idx = editedTraits.value.findIndex((t: TraitResponse) => t.key === key);
    if (idx >= 0) editedTraits.value[idx] = { key, value };
  } catch {
    traitsError.value = 'Failed to save trait. Please try again.';
  } finally {
    savingTraitKey.value = null;
  }
}

async function onDeleteTrait(key: string): Promise<void> {
  if (!props.identity) return;
  const envApiKey = contextStore.currentEnvironment?.apiKey;
  if (!envApiKey) return;
  deletingTraitKey.value = key;
  traitsError.value = null;
  try {
    await deleteIdentityTrait(envApiKey, props.identity.id, key);
    editedTraits.value = editedTraits.value.filter((t: TraitResponse) => t.key !== key);
  } catch {
    traitsError.value = 'Failed to delete trait. Please try again.';
  } finally {
    deletingTraitKey.value = null;
  }
}

async function onAddTrait(): Promise<void> {
  const key = newTraitKey.value.trim();
  const value = newTraitValue.value;
  if (!key || !props.identity) return;
  const envApiKey = contextStore.currentEnvironment?.apiKey;
  if (!envApiKey) return;
  isAddingTrait.value = true;
  traitsError.value = null;
  try {
    await upsertIdentityTrait(envApiKey, props.identity.id, key, { value });
    const existing = editedTraits.value.findIndex((t: TraitResponse) => t.key === key);
    if (existing >= 0) {
      editedTraits.value[existing] = { key, value };
    } else {
      editedTraits.value = [...editedTraits.value, { key, value }];
    }
    newTraitKey.value = '';
    newTraitValue.value = '';
  } catch {
    traitsError.value = 'Failed to add trait. Please try again.';
  } finally {
    isAddingTrait.value = false;
  }
}

async function onDeleteIdentity(): Promise<void> {
  if (!props.identity) return;
  const envApiKey = contextStore.currentEnvironment?.apiKey;
  if (!envApiKey) return;
  isDeleting.value = true;
  traitsError.value = null;
  try {
    await deleteIdentity(envApiKey, props.identity.id);
    emit('deleted', props.identity.id);
    emit('update:modelValue', false);
  } catch {
    traitsError.value = 'Failed to delete identity. Please try again.';
  } finally {
    isDeleting.value = false;
  }
}
</script>
