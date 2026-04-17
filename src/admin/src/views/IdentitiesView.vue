<template>
  <div>
    <div class="d-flex align-center justify-space-between mb-4">
      <h1 class="text-h5 font-weight-bold">Identities</h1>
      <v-btn
        v-if="contextStore.currentEnvironment"
        color="primary"
        prepend-icon="ri-add-line"
        data-testid="create-identity-btn"
        @click="showCreateDialog = true"
      >
        Create Identity
      </v-btn>
    </div>

    <!-- No environment selected -->
    <v-card v-if="!contextStore.currentEnvironment" variant="outlined" rounded="lg">
      <v-card-text class="text-center py-10">
        <v-icon size="48" color="medium-emphasis" class="mb-3">ri-user-line</v-icon>
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
        prepend-inner-icon="ri-search-line"
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
              icon="ri-edit-line"
              size="small"
              variant="text"
              :data-testid="`view-identity-${item.id}`"
              @click.stop="openDetail(item)"
            />
          </template>

          <!-- Empty state -->
          <template #no-data>
            <div class="text-center py-8">
              <v-icon size="40" color="medium-emphasis" class="mb-2">ri-user-line</v-icon>
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

    <!-- Create identity dialog -->
    <v-dialog v-model="showCreateDialog" max-width="440" @after-leave="onCreateDialogClosed">
      <v-card rounded="lg">
        <v-card-title class="pa-6 pb-3">Create Identity</v-card-title>
        <v-card-text class="pa-6 pt-0">
          <v-alert
            v-if="createError"
            type="error"
            variant="tonal"
            density="compact"
            closable
            class="mb-4"
            @click:close="createError = null"
          >
            {{ createError }}
          </v-alert>
          <v-text-field
            v-model="newIdentifier"
            label="Identifier"
            variant="outlined"
            density="comfortable"
            autofocus
            hide-details="auto"
            placeholder="e.g. user-123 or alice@example.com"
            data-testid="create-identity-identifier-input"
            @keydown.enter="onCreateConfirm"
          />
        </v-card-text>
        <v-card-actions class="pa-6 pt-0 d-flex justify-end ga-2">
          <v-btn variant="text" @click="showCreateDialog = false">Cancel</v-btn>
          <v-btn
            color="primary"
            variant="flat"
            :disabled="!newIdentifier.trim()"
            :loading="isCreating"
            data-testid="create-identity-confirm-btn"
            @click="onCreateConfirm"
          >
            Create
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

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
import { listIdentities, createIdentity } from '@/api/identities';
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

const showCreateDialog = ref(false);
const newIdentifier = ref('');
const isCreating = ref(false);
const createError = ref<string | null>(null);

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

async function onCreateConfirm(): Promise<void> {
  const envApiKey = contextStore.currentEnvironment?.apiKey;
  if (!envApiKey || !newIdentifier.value.trim()) return;
  isCreating.value = true;
  createError.value = null;
  try {
    const created = await createIdentity(envApiKey, { identifier: newIdentifier.value.trim() });
    identities.value.unshift(created);
    showCreateDialog.value = false;
  } catch (err: unknown) {
    const status = (err as { response?: { status?: number } })?.response?.status;
    createError.value = status === 409
      ? 'An identity with this identifier already exists in this environment.'
      : 'Failed to create identity. Please try again.';
  } finally {
    isCreating.value = false;
  }
}

function onCreateDialogClosed(): void {
  newIdentifier.value = '';
  createError.value = null;
}

function onIdentityDeleted(identityId: number): void {
  identities.value = identities.value.filter((i) => i.id !== identityId);
  showDetail.value = false;
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
}
</script>
