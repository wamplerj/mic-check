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
    <p
      v-if="!isLoading && organizations.length === 0"
      class="text-caption text-medium-emphasis mt-1 px-1"
      data-testid="org-empty-message"
    >
      No organizations available.
    </p>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useContextStore } from '@/stores/context';
import { listOrganizations } from '@/api/organizations';
import type { OrganizationResponse } from '@/types/api';

const contextStore = useContextStore();

const organizations = ref<OrganizationResponse[]>([]);
const isLoading = ref(false);

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

onMounted(fetchOrganizations);
</script>
