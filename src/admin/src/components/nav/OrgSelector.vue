<template>
  <div data-testid="org-selector">
    <v-select
      :model-value="contextStore.currentOrganization"
      :items="organizations"
      item-title="name"
      return-object
      label="Organization"
      density="compact"
      variant="outlined"
      hide-details
      :loading="isLoading"
      :disabled="isLoading"
      no-data-text="No organizations found"
      prepend-inner-icon="mdi-domain"
      data-testid="org-select"
      @update:model-value="onOrgSelected"
    />

    <v-btn
      size="x-small"
      variant="text"
      color="primary"
      prepend-icon="mdi-plus"
      class="mt-1 px-1"
      data-testid="create-org-btn"
      @click="openCreateDialog"
    >
      Create organization
    </v-btn>

    <!-- Create Organization Dialog -->
    <v-dialog v-model="showDialog" max-width="420" data-testid="create-org-dialog">
      <v-card rounded="lg">
        <v-card-title class="pa-5 pb-3">Create organization</v-card-title>
        <v-card-text class="pa-5 pt-0">
          <v-text-field
            v-model="newOrgName"
            label="Organization name"
            variant="outlined"
            density="compact"
            autofocus
            :error-messages="createError ? [createError] : []"
            data-testid="org-name-input"
            @keydown.enter="onCreateConfirm"
          />
        </v-card-text>
        <v-card-actions class="pa-5 pt-0">
          <v-spacer />
          <v-btn variant="text" data-testid="create-org-cancel-btn" @click="closeDialog">Cancel</v-btn>
          <v-btn
            color="primary"
            variant="flat"
            :loading="isSaving"
            :disabled="!newOrgName.trim()"
            data-testid="create-org-confirm-btn"
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
import { ref, onMounted } from 'vue';
import { useContextStore } from '@/stores/context';
import { listOrganizations, createOrganization } from '@/api/organizations';
import type { OrganizationResponse } from '@/types/api';

const contextStore = useContextStore();

const organizations = ref<OrganizationResponse[]>([]);
const isLoading = ref(false);

const showDialog = ref(false);
const newOrgName = ref('');
const isSaving = ref(false);
const createError = ref('');

async function fetchOrganizations(): Promise<void> {
  isLoading.value = true;
  try {
    organizations.value = await listOrganizations();
  } catch {
    organizations.value = [];
  } finally {
    isLoading.value = false;
  }
}

function onOrgSelected(org: OrganizationResponse | null): void {
  contextStore.setOrganization(org);
}

function openCreateDialog(): void {
  newOrgName.value = '';
  createError.value = '';
  showDialog.value = true;
}

function closeDialog(): void {
  showDialog.value = false;
}

async function onCreateConfirm(): Promise<void> {
  const name = newOrgName.value.trim();
  if (!name) return;

  isSaving.value = true;
  createError.value = '';
  try {
    const created = await createOrganization({ name });
    organizations.value = [...organizations.value, created];
    contextStore.setOrganization(created);
    closeDialog();
  } catch {
    createError.value = 'Failed to create organization. Please try again.';
  } finally {
    isSaving.value = false;
  }
}

onMounted(fetchOrganizations);
</script>
