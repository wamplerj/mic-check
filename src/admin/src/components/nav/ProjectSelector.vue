<template>
  <div data-testid="project-selector">
    <v-select
      :model-value="contextStore.currentProject"
      :items="projects"
      item-title="name"
      return-object
      label="Project"
      density="compact"
      variant="outlined"
      hide-details
      :loading="isLoading"
      :disabled="isLoading || !contextStore.currentOrganization"
      :placeholder="contextStore.currentOrganization ? 'Select a project' : 'Select an organization first'"
      no-data-text="No projects found"
      prepend-inner-icon="mdi-folder-outline"
      data-testid="project-select"
      @update:model-value="onProjectSelected"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useContextStore } from '@/stores/context';
import { listProjects } from '@/api/projects';
import type { ProjectResponse } from '@/types/api';

const contextStore = useContextStore();

const projects = ref<ProjectResponse[]>([]);
const isLoading = ref(false);

async function fetchProjects(organizationId: number): Promise<void> {
  isLoading.value = true;
  try {
    projects.value = await listProjects(organizationId);
  } finally {
    isLoading.value = false;
  }
}

function onProjectSelected(project: ProjectResponse | null): void {
  contextStore.setProject(project);
}

watch(
  () => contextStore.currentOrganization,
  (org) => {
    projects.value = [];
    if (org) {
      fetchProjects(org.id);
    }
  },
  { immediate: true },
);
</script>
