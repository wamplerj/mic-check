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
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useContextStore } from '@/stores/context';
import { listEnvironments } from '@/api/environments';
import type { EnvironmentResponse } from '@/types/api';

const contextStore = useContextStore();

const environments = ref<EnvironmentResponse[]>([]);
const isLoading = ref(false);

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
