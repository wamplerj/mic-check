<template>
  <div data-testid="environment-tab-bar">
    <v-select
      :model-value="contextStore.currentEnvironment"
      :items="environments"
      item-title="name"
      return-object
      label="Environment"
      density="compact"
      variant="outlined"
      hide-details
      :loading="isLoading"
      :disabled="isLoading || !contextStore.currentProject"
      :placeholder="contextStore.currentProject ? 'Select an environment' : 'Select a project first'"
      no-data-text="No environments found"
      prepend-inner-icon="ri-server-line"
      data-testid="env-select"
      @update:model-value="onEnvironmentSelected"
    />

    <v-btn
      size="x-small"
      variant="text"
      color="primary"
      prepend-icon="ri-add-line"
      class="mt-1 px-1"
      :disabled="!contextStore.currentProject"
      data-testid="create-env-btn"
      @click="openCreateDialog"
    >
      Create environment
    </v-btn>

    <!-- Create Environment Dialog -->
    <v-dialog v-model="showDialog" max-width="420" data-testid="create-env-dialog">
      <v-card rounded="lg">
        <v-card-title class="pa-5 pb-3">Create environment</v-card-title>
        <v-card-text class="pa-5 pt-0">
          <v-text-field
            v-model="newEnvName"
            label="Environment name"
            variant="outlined"
            density="compact"
            autofocus
            :error-messages="createError ? [createError] : []"
            data-testid="env-name-input"
            @keydown.enter="onCreateConfirm"
          />
        </v-card-text>
        <v-card-actions class="pa-5 pt-0">
          <v-spacer />
          <v-btn variant="text" data-testid="create-env-cancel-btn" @click="closeDialog">Cancel</v-btn>
          <v-btn
            color="primary"
            variant="flat"
            :loading="isSaving"
            :disabled="!newEnvName.trim()"
            data-testid="create-env-confirm-btn"
            @click="onCreateConfirm"
          >
            Create
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useContextStore } from '@/stores/context';
import { listEnvironments, createEnvironment } from '@/api/environments';
import type { EnvironmentResponse } from '@/types/api';

const contextStore = useContextStore();

const environments = ref<EnvironmentResponse[]>([]);
const isLoading = ref(false);

const showDialog = ref(false);
const newEnvName = ref('');
const isSaving = ref(false);
const createError = ref('');

async function fetchEnvironments(projectId: number): Promise<void> {
  isLoading.value = true;
  try {
    environments.value = await listEnvironments(projectId);
    autoSelectEnvironment();
  } finally {
    isLoading.value = false;
  }
}

function autoSelectEnvironment(): void {
  if (contextStore.currentEnvironment) {
    const fresh = environments.value.find(e => e.id === contextStore.currentEnvironment!.id);
    if (fresh) {
      contextStore.refreshEnvironment(fresh);
      return;
    }
  }
  if (environments.value.length > 0) {
    contextStore.setEnvironment(environments.value[0]);
  }
}

function onEnvironmentSelected(env: EnvironmentResponse | null): void {
  if (env) contextStore.setEnvironment(env);
}

function openCreateDialog(): void {
  newEnvName.value = '';
  createError.value = '';
  showDialog.value = true;
}

function closeDialog(): void {
  showDialog.value = false;
}

async function onCreateConfirm(): Promise<void> {
  const name = newEnvName.value.trim();
  if (!name || !contextStore.currentProject) return;

  isSaving.value = true;
  createError.value = '';
  try {
    const created = await createEnvironment({ name, projectId: contextStore.currentProject.id });
    environments.value = [...environments.value, created];
    contextStore.setEnvironment(created);
    closeDialog();
  } catch {
    createError.value = 'Failed to create environment. Please try again.';
  } finally {
    isSaving.value = false;
  }
}

watch(
  () => contextStore.currentProject?.id,
  (projectId, oldProjectId) => {
    if (projectId === oldProjectId) return;
    environments.value = [];
    if (oldProjectId !== undefined) contextStore.setEnvironment(null);
    if (projectId) fetchEnvironments(projectId);
  },
  { immediate: true },
);
</script>
