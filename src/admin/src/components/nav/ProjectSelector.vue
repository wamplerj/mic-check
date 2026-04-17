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
      prepend-inner-icon="ri-folder-line"
      data-testid="project-select"
      @update:model-value="onProjectSelected"
    />

    <v-btn
      size="x-small"
      variant="text"
      color="primary"
      prepend-icon="ri-add-line"
      class="mt-1 px-1"
      :disabled="!contextStore.currentOrganization"
      data-testid="create-project-btn"
      @click="openCreateDialog"
    >
      Create project
    </v-btn>

    <!-- Create Project Dialog -->
    <v-dialog v-model="showDialog" max-width="420" data-testid="create-project-dialog">
      <v-card rounded="lg">
        <v-card-title class="pa-5 pb-3">Create project</v-card-title>
        <v-card-text class="pa-5 pt-0">
          <v-text-field
            v-model="newProjectName"
            label="Project name"
            variant="outlined"
            density="compact"
            autofocus
            :error-messages="createError ? [createError] : []"
            data-testid="project-name-input"
            @keydown.enter="onCreateConfirm"
          />
        </v-card-text>
        <v-card-actions class="pa-5 pt-0">
          <v-spacer />
          <v-btn variant="text" data-testid="create-project-cancel-btn" @click="closeDialog">Cancel</v-btn>
          <v-btn
            color="primary"
            variant="flat"
            :loading="isSaving"
            :disabled="!newProjectName.trim()"
            data-testid="create-project-confirm-btn"
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
import { listProjects, createProject } from '@/api/projects';
import type { ProjectResponse } from '@/types/api';

const contextStore = useContextStore();

const projects = ref<ProjectResponse[]>([]);
const isLoading = ref(false);

const showDialog = ref(false);
const newProjectName = ref('');
const isSaving = ref(false);
const createError = ref('');

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

function openCreateDialog(): void {
  newProjectName.value = '';
  createError.value = '';
  showDialog.value = true;
}

function closeDialog(): void {
  showDialog.value = false;
}

async function onCreateConfirm(): Promise<void> {
  const name = newProjectName.value.trim();
  if (!name || !contextStore.currentOrganization) return;

  isSaving.value = true;
  createError.value = '';
  try {
    const created = await createProject({ name, organizationId: contextStore.currentOrganization.id });
    projects.value = [...projects.value, created];
    contextStore.setProject(created);
    closeDialog();
  } catch {
    createError.value = 'Failed to create project. Please try again.';
  } finally {
    isSaving.value = false;
  }
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
