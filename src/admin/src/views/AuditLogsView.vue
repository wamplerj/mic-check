<template>
  <div>
    <div class="d-flex align-center justify-space-between mb-4">
      <h1 class="text-h5 font-weight-bold">Audit Logs</h1>
    </div>

    <!-- No project selected -->
    <v-card v-if="!contextStore.currentProject" variant="outlined" rounded="lg">
      <v-card-text class="text-center py-10">
        <v-icon size="48" color="medium-emphasis" class="mb-3">mdi-clipboard-text-clock-outline</v-icon>
        <p class="text-h6 mb-2">Select a project</p>
        <p class="text-body-2 text-medium-emphasis">
          Choose a project from the sidebar to view its audit logs.
        </p>
      </v-card-text>
    </v-card>

    <template v-else>
      <!-- Filters -->
      <v-card variant="outlined" rounded="lg" class="mb-4 pa-4">
        <div class="d-flex align-center ga-3 flex-wrap">
          <v-select
            v-model="filter.resourceType"
            :items="resourceTypeOptions"
            label="Resource type"
            variant="outlined"
            density="compact"
            hide-details
            clearable
            style="min-width: 160px; flex: 1"
            data-testid="filter-resource-type"
            @update:model-value="onFilterChange"
          />
          <v-select
            v-model="filter.action"
            :items="actionOptions"
            label="Action"
            variant="outlined"
            density="compact"
            hide-details
            clearable
            style="min-width: 140px; flex: 1"
            data-testid="filter-action"
            @update:model-value="onFilterChange"
          />
          <v-text-field
            v-model="filter.from"
            label="From date"
            variant="outlined"
            density="compact"
            hide-details
            type="date"
            style="min-width: 160px; flex: 1"
            data-testid="filter-from"
            @update:model-value="onFilterChange"
          />
          <v-text-field
            v-model="filter.to"
            label="To date"
            variant="outlined"
            density="compact"
            hide-details
            type="date"
            style="min-width: 160px; flex: 1"
            data-testid="filter-to"
            @update:model-value="onFilterChange"
          />
          <v-btn
            variant="text"
            size="small"
            data-testid="clear-filters-btn"
            @click="clearFilters"
          >
            Clear
          </v-btn>
        </div>
      </v-card>

      <!-- Table -->
      <v-card rounded="lg">
        <v-data-table
          :headers="headers"
          :items="logs"
          :loading="isLoading"
          :items-per-page="pageSize"
          hover
          data-testid="audit-logs-table"
        >
          <!-- Timestamp -->
          <template #item.createdAt="{ item }: { item: AuditLogResponse }">
            <span class="text-caption text-medium-emphasis">{{ formatDate(item.createdAt) }}</span>
          </template>

          <!-- Action chip -->
          <template #item.action="{ item }: { item: AuditLogResponse }">
            <v-chip
              :color="actionColor(item.action)"
              size="x-small"
              variant="tonal"
            >
              {{ item.action }}
            </v-chip>
          </template>

          <!-- Resource type -->
          <template #item.resourceType="{ item }: { item: AuditLogResponse }">
            <span class="text-body-2">{{ item.resourceType }}</span>
          </template>

          <!-- Resource ID -->
          <template #item.resourceId="{ item }: { item: AuditLogResponse }">
            <span class="text-body-2 text-medium-emphasis">{{ item.resourceId }}</span>
          </template>

          <!-- Changes (expandable) -->
          <template #item.changes="{ item }: { item: AuditLogResponse }">
            <div v-if="item.changes">
              <v-btn
                size="x-small"
                variant="text"
                :data-testid="`expand-changes-${item.id}`"
                @click="toggleChanges(item.id)"
              >
                {{ expandedIds.has(item.id) ? 'Hide' : 'Show' }}
              </v-btn>
              <pre
                v-if="expandedIds.has(item.id)"
                class="text-caption mt-1 pa-2 rounded"
                style="background: rgba(0,0,0,0.05); white-space: pre-wrap; word-break: break-all; max-width: 300px"
                :data-testid="`changes-${item.id}`"
              >{{ formatJson(item.changes) }}</pre>
            </div>
            <span v-else class="text-caption text-medium-emphasis">—</span>
          </template>

          <!-- Empty state -->
          <template #no-data>
            <div class="text-center py-8">
              <v-icon size="40" color="medium-emphasis" class="mb-2">mdi-clipboard-text-clock-outline</v-icon>
              <p class="text-body-2 text-medium-emphasis">No audit log entries found.</p>
            </div>
          </template>
        </v-data-table>

        <!-- Manual pagination controls -->
        <div class="d-flex align-center justify-end pa-3 ga-2">
          <span class="text-caption text-medium-emphasis">
            Page {{ page }} of {{ totalPages }} ({{ total }} entries)
          </span>
          <v-btn
            icon="mdi-chevron-left"
            size="small"
            variant="text"
            :disabled="page <= 1 || isLoading"
            data-testid="prev-page-btn"
            @click="prevPage"
          />
          <v-btn
            icon="mdi-chevron-right"
            size="small"
            variant="text"
            :disabled="page >= totalPages || isLoading"
            data-testid="next-page-btn"
            @click="nextPage"
          />
        </div>
      </v-card>
    </template>

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
import { listAuditLogsByProject } from '@/api/auditLogs';
import { useContextStore } from '@/stores/context';
import type { AuditLogResponse } from '@/types/api';

const contextStore = useContextStore();

const logs = ref<AuditLogResponse[]>([]);
const total = ref(0);
const page = ref(1);
const pageSize = 20;
const isLoading = ref(false);
const expandedIds = ref(new Set<number>());
const showErrorSnackbar = ref(false);
const snackbarMessage = ref('');

const filter = ref<{
  resourceType: string | null;
  action: string | null;
  from: string | null;
  to: string | null;
}>({ resourceType: null, action: null, from: null, to: null });

const resourceTypeOptions = [
  'Feature', 'FeatureState', 'Segment', 'Environment', 'Project',
  'Organization', 'Identity', 'ApiKey', 'Webhook',
];
const actionOptions = ['Created', 'Updated', 'Deleted', 'Enabled', 'Disabled'];

const headers = [
  { title: 'Timestamp', key: 'createdAt', sortable: false, width: '160' },
  { title: 'Action', key: 'action', sortable: false, width: '100' },
  { title: 'Resource Type', key: 'resourceType', sortable: false, width: '140' },
  { title: 'Resource ID', key: 'resourceId', sortable: false, width: '100' },
  { title: 'Changes', key: 'changes', sortable: false },
];

const totalPages = computed(() => Math.max(1, Math.ceil(total.value / pageSize)));

function showError(message: string): void {
  snackbarMessage.value = message;
  showErrorSnackbar.value = true;
}

async function loadLogs(): Promise<void> {
  const projectId = contextStore.currentProject?.id;
  if (!projectId) {
    logs.value = [];
    total.value = 0;
    return;
  }
  isLoading.value = true;
  try {
    const result = await listAuditLogsByProject(projectId, {
      resourceType: filter.value.resourceType,
      action: filter.value.action,
      from: filter.value.from ? `${filter.value.from}T00:00:00Z` : null,
      to: filter.value.to ? `${filter.value.to}T23:59:59Z` : null,
      page: page.value,
      pageSize,
    });
    logs.value = result.results;
    total.value = result.count;
  } catch {
    logs.value = [];
    total.value = 0;
    showError('Failed to load audit logs. Please refresh the page.');
  } finally {
    isLoading.value = false;
  }
}

watch(() => contextStore.currentProject, () => {
  page.value = 1;
  loadLogs();
}, { immediate: true });

function onFilterChange(): void {
  page.value = 1;
  loadLogs();
}

function clearFilters(): void {
  filter.value = { resourceType: null, action: null, from: null, to: null };
  page.value = 1;
  loadLogs();
}

function prevPage(): void {
  if (page.value > 1) { page.value--; loadLogs(); }
}

function nextPage(): void {
  if (page.value < totalPages.value) { page.value++; loadLogs(); }
}

function toggleChanges(id: number): void {
  const next = new Set(expandedIds.value);
  if (next.has(id)) { next.delete(id); } else { next.add(id); }
  expandedIds.value = next;
}

function formatJson(raw: string): string {
  try { return JSON.stringify(JSON.parse(raw), null, 2); } catch { return raw; }
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString(undefined, {
    month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit',
  });
}

function actionColor(action: string): string {
  if (action === 'Created') return 'success';
  if (action === 'Deleted') return 'error';
  return 'primary';
}
</script>
