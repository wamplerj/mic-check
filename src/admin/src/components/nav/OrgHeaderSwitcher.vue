<template>
  <div data-testid="org-header-switcher">
    <v-menu data-testid="org-header-menu">
      <template #activator="{ props: menuProps }">
        <a
          href="#"
          class="org-header-link text-body-2 font-weight-medium d-flex align-center"
          data-testid="org-header-link"
          v-bind="menuProps"
          @click.prevent
        >
          <VIcon icon="ri-building-line" size="18" class="me-1" />
          {{ contextStore.currentOrganization?.name ?? 'Select organization' }}
          <VIcon icon="ri-arrow-down-s-line" size="18" />
        </a>
      </template>

      <v-list density="compact" data-testid="org-header-list">
        <v-list-item
          v-for="org in organizations"
          :key="org.id"
          :title="org.name"
          :active="org.id === contextStore.currentOrganization?.id"
          :data-testid="`org-header-item-${org.id}`"
          @click="onOrgSelected(org)"
        />

        <v-divider v-if="organizations.length" class="my-1" />

        <v-list-item
          title="New Organization"
          prepend-icon="ri-add-line"
          data-testid="org-header-create-item"
          @click="openCreateDialog"
        />
      </v-list>
    </v-menu>

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

const showDialog = ref(false);
const newOrgName = ref('');
const isSaving = ref(false);
const createError = ref('');

async function fetchOrganizations(): Promise<void> {
  try {
    organizations.value = await listOrganizations();
    autoSelectOrganization();
  } catch {
    organizations.value = [];
  }
}

function autoSelectOrganization(): void {
  if (contextStore.currentOrganization) {
    // Refresh stored org with latest data from API (name may have changed)
    const fresh = organizations.value.find(o => o.id === contextStore.currentOrganization!.id);
    if (fresh) contextStore.refreshOrganization(fresh);
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

async function onOrgSelected(org: OrganizationResponse): Promise<void> {
  contextStore.setOrganization(org);
  try {
    await setPrimaryOrganization(org.id);
    organizations.value = organizations.value.map(o => ({ ...o, isPrimary: o.id === org.id }));
  } catch {
    // Non-critical — selection is already saved to localStorage
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

<style lang="scss" scoped>
.org-header-link {
  text-decoration: none;
  color: inherit;
  white-space: nowrap;
}
</style>
