<template>
  <div>
    <div class="d-flex align-center justify-space-between mb-4">
      <h1 class="text-h5 font-weight-bold">Features</h1>
      <v-btn
        v-if="contextStore.currentProject"
        color="primary"
        prepend-icon="mdi-plus"
        data-testid="create-feature-btn"
        @click="openCreateDialog"
      >
        Create Feature
      </v-btn>
    </div>

    <!-- No project selected -->
    <v-card v-if="!contextStore.currentProject" variant="outlined" rounded="lg">
      <v-card-text class="text-center py-10">
        <v-icon size="48" color="medium-emphasis" class="mb-3">mdi-flag-outline</v-icon>
        <p class="text-h6 mb-2">Select a project</p>
        <p class="text-body-2 text-medium-emphasis">
          Choose a project from the sidebar to manage its feature flags.
        </p>
      </v-card-text>
    </v-card>

    <template v-else>
      <!-- Search bar -->
      <v-text-field
        v-model="search"
        prepend-inner-icon="mdi-magnify"
        label="Search features"
        variant="outlined"
        density="comfortable"
        clearable
        hide-details
        class="mb-4"
        data-testid="feature-search"
      />

      <!-- Features table -->
      <v-card rounded="lg">
        <v-data-table
          :headers="headers"
          :items="filteredFeatures"
          :loading="isLoading"
          :items-per-page="20"
          :search="search"
          hover
          data-testid="features-table"
          @click:row="onRowClick"
        >
          <!-- Name + description -->
          <template #item.name="{ item }: { item: FeatureResponse }">
            <div>
              <span class="text-body-2 font-weight-medium">{{ item.name }}</span>
              <p v-if="item.description" class="text-caption text-medium-emphasis mb-0">
                {{ item.description }}
              </p>
            </div>
          </template>

          <!-- Type chip -->
          <template #item.type="{ item }: { item: FeatureResponse }">
            <v-chip
              :color="item.type === 'MULTIVARIATE' ? 'secondary' : 'primary'"
              size="x-small"
              variant="tonal"
            >
              {{ item.type }}
            </v-chip>
          </template>

          <!-- Enabled toggle for current environment -->
          <template #item.enabled="{ item }: { item: FeatureResponse }">
            <div v-if="!contextStore.currentEnvironment" class="text-caption text-medium-emphasis">
              —
            </div>
            <v-switch
              v-else
              :model-value="featureStateMap.get(item.id)?.enabled ?? false"
              color="primary"
              hide-details
              density="compact"
              :loading="togglingFeatureId === item.id"
              :data-testid="`toggle-${item.id}`"
              @update:model-value="onToggleEnabled(item.id, $event)"
              @click.stop
            />
          </template>

          <!-- Created at -->
          <template #item.createdAt="{ item }: { item: FeatureResponse }">
            <span class="text-caption text-medium-emphasis">
              {{ formatDate(item.createdAt) }}
            </span>
          </template>

          <!-- Row actions -->
          <template #item.actions="{ item }: { item: FeatureResponse }">
            <v-btn
              icon="mdi-pencil-outline"
              size="small"
              variant="text"
              :data-testid="`edit-feature-${item.id}`"
              @click.stop="openEditDialog(item)"
            />
          </template>

          <!-- Empty state -->
          <template #no-data>
            <div class="text-center py-8">
              <v-icon size="40" color="medium-emphasis" class="mb-2">mdi-flag-outline</v-icon>
              <p class="text-body-2 text-medium-emphasis">No features yet. Create your first flag.</p>
            </div>
          </template>
        </v-data-table>
      </v-card>
    </template>

    <!-- Create / Edit dialog -->
    <FeatureDialog
      v-model="showDialog"
      :feature="editingFeature"
      @saved="onFeatureSaved"
    />

    <!-- Feature detail drawer -->
    <FeatureDetail
      v-model="showDetail"
      :feature="selectedFeature"
      :feature-state="selectedFeatureState"
      @updated="onFeatureUpdated"
      @deleted="onFeatureDeleted"
      @state-updated="onStateUpdated"
    />

    <!-- Error snackbar -->
    <v-snackbar
      v-model="showErrorSnackbar"
      color="error"
      :timeout="4000"
      location="bottom"
    >
      {{ snackbarMessage }}
      <template #actions>
        <v-btn variant="text" @click="showErrorSnackbar = false">Dismiss</v-btn>
      </template>
    </v-snackbar>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { listFeatures } from '@/api/features';
import { listFeatureStates, patchFeatureState } from '@/api/featureStates';
import { useContextStore } from '@/stores/context';
import FeatureDialog from '@/components/features/FeatureDialog.vue';
import FeatureDetail from '@/components/features/FeatureDetail.vue';
import type { FeatureResponse, FeatureStateResponse } from '@/types/api';

const contextStore = useContextStore();

// ─── State ────────────────────────────────────────────────────────────────────
const features = ref<FeatureResponse[]>([]);
const featureStateMap = ref<Map<number, FeatureStateResponse>>(new Map());
const isLoading = ref(false);
const search = ref('');
const togglingFeatureId = ref<number | null>(null);

// Error snackbar
const showErrorSnackbar = ref(false);
const snackbarMessage = ref('');

function showError(message: string): void {
  snackbarMessage.value = message;
  showErrorSnackbar.value = true;
}

// Dialog / detail state
const showDialog = ref(false);
const editingFeature = ref<FeatureResponse | null>(null);
const showDetail = ref(false);
const selectedFeature = ref<FeatureResponse | null>(null);

const selectedFeatureState = computed(() =>
  selectedFeature.value ? (featureStateMap.value.get(selectedFeature.value.id) ?? null) : null,
);

// ─── Table config ─────────────────────────────────────────────────────────────
const headers = [
  { title: 'Name', key: 'name', sortable: true },
  { title: 'Type', key: 'type', sortable: true, width: '140' },
  { title: 'Enabled', key: 'enabled', sortable: false, width: '100' },
  { title: 'Created', key: 'createdAt', sortable: true, width: '130' },
  { title: '', key: 'actions', sortable: false, width: '50', align: 'end' as const },
];

// ─── Computed ─────────────────────────────────────────────────────────────────
const filteredFeatures = computed(() => {
  if (!search.value) return features.value;
  const q = search.value.toLowerCase();
  return features.value.filter(
    (f) => f.name.toLowerCase().includes(q) || (f.description ?? '').toLowerCase().includes(q),
  );
});

// ─── Data loading ─────────────────────────────────────────────────────────────
async function loadFeatures(): Promise<void> {
  const projectId = contextStore.currentProject?.id;
  if (!projectId) {
    features.value = [];
    return;
  }
  isLoading.value = true;
  try {
    const result = await listFeatures(projectId, 1, 100);
    features.value = result.results;
  } catch {
    features.value = [];
    showError('Failed to load features. Please refresh the page.');
  } finally {
    isLoading.value = false;
  }
}

async function loadFeatureStates(): Promise<void> {
  const envApiKey = contextStore.currentEnvironment?.apiKey;
  if (!envApiKey) {
    featureStateMap.value = new Map();
    return;
  }
  try {
    const states = await listFeatureStates(envApiKey);
    featureStateMap.value = new Map(states.map((s) => [s.featureId, s]));
  } catch {
    featureStateMap.value = new Map();
    showError('Failed to load feature states. Please refresh the page.');
  }
}

async function loadData(): Promise<void> {
  await Promise.all([loadFeatures(), loadFeatureStates()]);
}

// Reload when project or environment changes
watch(() => contextStore.currentProject, loadData, { immediate: true });
watch(() => contextStore.currentEnvironment, loadFeatureStates);

// ─── Actions ──────────────────────────────────────────────────────────────────
function openCreateDialog(): void {
  editingFeature.value = null;
  showDialog.value = true;
}

function openEditDialog(feature: FeatureResponse): void {
  editingFeature.value = feature;
  showDialog.value = true;
}

function onRowClick(_event: Event, { item }: { item: FeatureResponse }): void {
  selectedFeature.value = item;
  showDetail.value = true;
}

async function onFeatureSaved(saved: FeatureResponse): Promise<void> {
  const idx = features.value.findIndex((f) => f.id === saved.id);
  if (idx >= 0) {
    features.value[idx] = saved;
  } else {
    features.value.unshift(saved);
    // New feature: load its state for the current environment
    await loadFeatureStates();
  }
}

function onFeatureUpdated(updated: FeatureResponse): void {
  const idx = features.value.findIndex((f) => f.id === updated.id);
  if (idx >= 0) features.value[idx] = updated;
  if (selectedFeature.value?.id === updated.id) selectedFeature.value = updated;
}

function onFeatureDeleted(featureId: number): void {
  features.value = features.value.filter((f) => f.id !== featureId);
  featureStateMap.value.delete(featureId);
  featureStateMap.value = new Map(featureStateMap.value);
  showDetail.value = false;
}

function onStateUpdated(state: FeatureStateResponse): void {
  featureStateMap.value.set(state.featureId, state);
  // Trigger reactivity on the Map
  featureStateMap.value = new Map(featureStateMap.value);
}

async function onToggleEnabled(featureId: number, enabled: boolean | null): Promise<void> {
  if (enabled === null) return;
  const envApiKey = contextStore.currentEnvironment?.apiKey;
  const state = featureStateMap.value.get(featureId);
  if (!envApiKey || !state) return;

  togglingFeatureId.value = featureId;
  try {
    const updated = await patchFeatureState(envApiKey, state.id, { enabled });
    onStateUpdated(updated);
  } catch {
    showError('Failed to update feature state. Please try again.');
  } finally {
    togglingFeatureId.value = null;
  }
}

// ─── Helpers ──────────────────────────────────────────────────────────────────
function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
}
</script>
