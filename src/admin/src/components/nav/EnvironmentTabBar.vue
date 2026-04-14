<template>
  <div data-testid="environment-tab-bar">
    <p class="text-caption text-medium-emphasis mb-1 px-1">Environment</p>

    <div v-if="isLoading" class="d-flex justify-center py-2">
      <v-progress-circular indeterminate size="20" width="2" color="primary" />
    </div>

    <template v-else-if="environments.length > 0">
      <v-chip-group
        :model-value="selectedIndex"
        column
        mandatory
        selected-class="text-primary"
        data-testid="env-chip-group"
        @update:model-value="onEnvironmentSelected"
      >
        <v-chip
          v-for="(env, index) in environments"
          :key="env.id"
          :value="index"
          size="small"
          variant="tonal"
          :data-testid="`env-chip-${env.name.toLowerCase()}`"
        >
          {{ env.name }}
        </v-chip>
      </v-chip-group>
    </template>

    <p
      v-else-if="contextStore.currentProject"
      class="text-caption text-medium-emphasis px-1"
      data-testid="env-empty-message"
    >
      No environments found.
    </p>

    <p
      v-else
      class="text-caption text-medium-emphasis px-1"
      data-testid="env-no-project-message"
    >
      Select a project first.
    </p>

    <v-btn
      size="x-small"
      variant="text"
      color="primary"
      prepend-icon="mdi-plus"
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
import { ref, computed, watch } from 'vue';
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

const selectedIndex = computed(() => {
  if (!contextStore.currentEnvironment) return undefined;
  const idx = environments.value.findIndex((e) => e.id === contextStore.currentEnvironment!.id);
  return idx >= 0 ? idx : undefined;
});

async function fetchEnvironments(projectId: number): Promise<void> {
  isLoading.value = true;
  try {
    environments.value = await listEnvironments(projectId);
    // Auto-select the first environment when none is currently selected
    if (environments.value.length > 0 && !contextStore.currentEnvironment) {
      contextStore.setEnvironment(environments.value[0]);
    }
  } finally {
    isLoading.value = false;
  }
}

function onEnvironmentSelected(index: number): void {
  const env = environments.value[index];
  if (env) {
    contextStore.setEnvironment(env);
  }
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
  () => contextStore.currentProject,
  (project) => {
    environments.value = [];
    if (project) {
      fetchEnvironments(project.id);
    }
  },
  { immediate: true },
);
</script>
