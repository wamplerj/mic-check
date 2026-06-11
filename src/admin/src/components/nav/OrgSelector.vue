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
      prepend-inner-icon="ri-building-line"
      data-testid="org-select"
      @update:model-value="onOrgSelected"
    />

    <v-btn
      size="x-small"
      variant="text"
      color="primary"
      prepend-icon="ri-add-line"
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
import { listOrganizations, createOrganization, setPrimaryOrganization } from '@/api/organizations';
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
    autoSelectOrganization();
  } catch {
    organizations.value = [];
  } finally {
    isLoading.value = false;
  }
}

function autoSelectOrganization(): void {
  if (contextStore.currentOrganization) {
    // Refresh stored org with latest data from API (name may have changed)
    const fresh = organizations.value.find(o => o.id === contextStore.currentOrganization!.id);
    if (fresh) contextStore.setOrganization(fresh);
    return;
  }

  const primary = organizations.value.find(o => o.isPrimary);
  if (primary) {
    contextStore.setOrganization(primary);
    return;
  }

  if (organizations.value.length === 1) {
    contextStore.setOrganization(organizations.value[0]);
  }
}

async function onOrgSelected(org: OrganizationResponse | null): Promise<void> {
  contextStore.setOrganization(org);
  if (org) {
    try {
      await setPrimaryOrganization(org.id);
      organizations.value = organizations.value.map(o => ({ ...o, isPrimary: o.id === org.id }));
    } catch {
      // Non-critical — selection is already saved to localStorage
    }
  }
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
