<template>
  <div>
    <div class="d-flex align-center justify-space-between mb-4">
      <h1 class="text-h5 font-weight-bold">Identities</h1>
    </div>

    <!-- No environment selected -->
    <v-card v-if="!contextStore.currentEnvironment" variant="outlined" rounded="lg">
      <v-card-text class="text-center py-10">
        <v-icon size="48" color="medium-emphasis" class="mb-3">mdi-account-outline</v-icon>
        <p class="text-h6 mb-2">Select an environment</p>
        <p class="text-body-2 text-medium-emphasis">
          Choose an environment to view and manage identities.
        </p>
      </v-card-text>
    </v-card>

    <template v-else>
      <!-- Search bar -->
      <v-text-field
        v-model="search"
        prepend-inner-icon="mdi-magnify"
        label="Search identities"
        variant="outlined"
        density="comfortable"
        clearable
        hide-details
        class="mb-4"
        data-testid="identity-search"
      />

      <!-- Identities table -->
      <v-card rounded="lg">
        <v-data-table
          :headers="headers"
          :items="filteredIdentities"
          :loading="isLoading"
          :items-per-page="20"
          hover
          data-testid="identities-table"
          @click:row="onRowClick"
        >
          <!-- Identifier -->
          <template #item.identifier="{ item }: { item: IdentityResponse }">
            <span class="text-body-2 font-weight-medium">{{ item.identifier }}</span>
          </template>

          <!-- Trait count -->
          <template #item.traits="{ item }: { item: IdentityResponse }">
            <span class="text-body-2 text-medium-emphasis">{{ item.traits.length }}</span>
          </template>

          <!-- Created at -->
          <template #item.createdAt="{ item }: { item: IdentityResponse }">
            <span class="text-caption text-medium-emphasis">{{ formatDate(item.createdAt) }}</span>
          </template>

          <!-- Row actions -->
          <template #item.actions="{ item }: { item: IdentityResponse }">
            <v-btn
              icon="mdi-eye-outline"
              size="small"
              variant="text"
              :data-testid="`view-identity-${item.id}`"
              @click.stop="openDetail(item)"
            />
          </template>

          <!-- Empty state -->
          <template #no-data>
            <div class="text-center py-8">
              <v-icon size="40" color="medium-emphasis" class="mb-2">mdi-account-outline</v-icon>
              <p class="text-body-2 text-medium-emphasis">No identities found in this environment.</p>
            </div>
          </template>
        </v-data-table>
      </v-card>
    </template>

    <!-- Identity detail dialog -->
    <IdentityDetail
      v-model="showDetail"
      :identity="selectedIdentity"
      @deleted="onIdentityDeleted"
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
import { listIdentities } from '@/api/identities';
import { useContextStore } from '@/stores/context';
import IdentityDetail from '@/components/identities/IdentityDetail.vue';
import type { IdentityResponse } from '@/types/api';

const contextStore = useContextStore();

const identities = ref<IdentityResponse[]>([]);
const isLoading = ref(false);
const search = ref('');
const showErrorSnackbar = ref(false);
const snackbarMessage = ref('');

const showDetail = ref(false);
const selectedIdentity = ref<IdentityResponse | null>(null);

const headers = [
  { title: 'Identifier', key: 'identifier', sortable: true },
  { title: 'Traits', key: 'traits', sortable: false, width: '80' },
  { title: 'Created', key: 'createdAt', sortable: true, width: '130' },
  { title: '', key: 'actions', sortable: false, width: '50', align: 'end' as const },
];

function showError(message: string): void {
  snackbarMessage.value = message;
  showErrorSnackbar.value = true;
}

const filteredIdentities = computed(() => {
  if (!search.value) return identities.value;
  const q = search.value.toLowerCase();
  return identities.value.filter((i) => i.identifier.toLowerCase().includes(q));
});

async function loadIdentities(): Promise<void> {
  const envApiKey = contextStore.currentEnvironment?.apiKey;
  if (!envApiKey) {
    identities.value = [];
    return;
  }
  isLoading.value = true;
  try {
    const result = await listIdentities(envApiKey, 1, 100);
    identities.value = result.results;
  } catch {
    identities.value = [];
    showError('Failed to load identities. Please refresh the page.');
  } finally {
    isLoading.value = false;
  }
}

watch(() => contextStore.currentEnvironment, loadIdentities, { immediate: true });

function openDetail(identity: IdentityResponse): void {
  selectedIdentity.value = identity;
  showDetail.value = true;
}

function onRowClick(_event: Event, { item }: { item: IdentityResponse }): void {
  openDetail(item);
}

function onIdentityDeleted(identityId: number): void {
  identities.value = identities.value.filter((i) => i.id !== identityId);
  showDetail.value = false;
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
}
</script>
