<template>
  <div>
    <div class="d-flex align-center justify-space-between mb-4">
      <h1 class="text-h5 font-weight-bold">Segments</h1>
      <v-btn
        v-if="contextStore.currentProject"
        color="primary"
        prepend-icon="mdi-plus"
        data-testid="create-segment-btn"
        @click="openCreateDialog"
      >
        Create Segment
      </v-btn>
    </div>

    <!-- No project selected -->
    <v-card v-if="!contextStore.currentProject" variant="outlined" rounded="lg">
      <v-card-text class="text-center py-10">
        <v-icon size="48" color="medium-emphasis" class="mb-3">mdi-account-group-outline</v-icon>
        <p class="text-h6 mb-2">Select a project</p>
        <p class="text-body-2 text-medium-emphasis">
          Choose a project from the sidebar to manage its segments.
        </p>
      </v-card-text>
    </v-card>

    <template v-else>
      <v-card rounded="lg">
        <v-data-table
          :headers="headers"
          :items="segments"
          :loading="isLoading"
          :items-per-page="20"
          hover
          data-testid="segments-table"
        >
          <!-- Name -->
          <template #item.name="{ item }: { item: SegmentResponse }">
            <span class="text-body-2 font-weight-medium">{{ item.name }}</span>
          </template>

          <!-- Rule count -->
          <template #item.rules="{ item }: { item: SegmentResponse }">
            <span class="text-body-2 text-medium-emphasis">{{ item.rules.length }}</span>
          </template>

          <!-- Created at -->
          <template #item.createdAt="{ item }: { item: SegmentResponse }">
            <span class="text-caption text-medium-emphasis">{{ formatDate(item.createdAt) }}</span>
          </template>

          <!-- Actions -->
          <template #item.actions="{ item }: { item: SegmentResponse }">
            <div class="d-flex align-center">
              <v-btn
                icon="mdi-pencil-outline"
                size="small"
                variant="text"
                :data-testid="`edit-segment-${item.id}`"
                @click="openEditDialog(item)"
              />
              <v-btn
                icon="mdi-trash-can-outline"
                size="small"
                variant="text"
                color="error"
                :data-testid="`delete-segment-${item.id}`"
                @click="openDeleteConfirm(item)"
              />
            </div>
          </template>

          <!-- Empty state -->
          <template #no-data>
            <div class="text-center py-8">
              <v-icon size="40" color="medium-emphasis" class="mb-2">mdi-account-group-outline</v-icon>
              <p class="text-body-2 text-medium-emphasis">No segments yet. Create your first segment.</p>
            </div>
          </template>
        </v-data-table>
      </v-card>
    </template>

    <!-- Create / Edit dialog -->
    <SegmentEditor
      v-model="showEditor"
      :segment="editingSegment"
      @saved="onSegmentSaved"
    />

    <!-- Delete confirmation dialog -->
    <v-dialog v-model="showDeleteDialog" max-width="400">
      <v-card rounded="lg" data-testid="delete-confirm-dialog">
        <v-card-title class="text-body-1 font-weight-bold pa-4 pb-2">Delete Segment</v-card-title>
        <v-card-text class="pa-4 pt-0">
          <p class="text-body-2 mb-3">
            Are you sure you want to delete
            <strong>{{ deletingSegment?.name }}</strong>?
            This cannot be undone.
          </p>
          <p class="text-body-2 mb-2">
            Type <strong>{{ deletingSegment?.name }}</strong> to confirm:
          </p>
          <v-text-field
            v-model="deleteConfirmName"
            variant="outlined"
            density="compact"
            hide-details
            :placeholder="deletingSegment?.name"
            data-testid="delete-confirm-input"
          />
        </v-card-text>
        <v-card-actions class="pa-4 pt-0">
          <v-spacer />
          <v-btn variant="text" @click="closeDeleteConfirm">Cancel</v-btn>
          <v-btn
            color="error"
            variant="flat"
            :disabled="deleteConfirmName !== deletingSegment?.name"
            :loading="isDeleting"
            data-testid="confirm-delete-btn"
            @click="onDeleteSegment"
          >
            Delete
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
import { ref, watch } from 'vue';
import { listSegments, deleteSegment } from '@/api/segments';
import { useContextStore } from '@/stores/context';
import SegmentEditor from '@/components/segments/SegmentEditor.vue';
import type { SegmentResponse } from '@/types/api';

const contextStore = useContextStore();

const segments = ref<SegmentResponse[]>([]);
const isLoading = ref(false);
const showErrorSnackbar = ref(false);
const snackbarMessage = ref('');

const showEditor = ref(false);
const editingSegment = ref<SegmentResponse | null>(null);

const showDeleteDialog = ref(false);
const deletingSegment = ref<SegmentResponse | null>(null);
const deleteConfirmName = ref('');
const isDeleting = ref(false);

const headers = [
  { title: 'Name', key: 'name', sortable: true },
  { title: 'Rules', key: 'rules', sortable: false, width: '80' },
  { title: 'Created', key: 'createdAt', sortable: true, width: '130' },
  { title: '', key: 'actions', sortable: false, width: '80', align: 'end' as const },
];

function showError(message: string): void {
  snackbarMessage.value = message;
  showErrorSnackbar.value = true;
}

async function loadSegments(): Promise<void> {
  const projectId = contextStore.currentProject?.id;
  if (!projectId) {
    segments.value = [];
    return;
  }
  isLoading.value = true;
  try {
    segments.value = await listSegments(projectId);
  } catch {
    segments.value = [];
    showError('Failed to load segments. Please refresh the page.');
  } finally {
    isLoading.value = false;
  }
}

watch(() => contextStore.currentProject, loadSegments, { immediate: true });

function openCreateDialog(): void {
  editingSegment.value = null;
  showEditor.value = true;
}

function openEditDialog(segment: SegmentResponse): void {
  editingSegment.value = segment;
  showEditor.value = true;
}

function openDeleteConfirm(segment: SegmentResponse): void {
  deletingSegment.value = segment;
  deleteConfirmName.value = '';
  showDeleteDialog.value = true;
}

function closeDeleteConfirm(): void {
  showDeleteDialog.value = false;
  deletingSegment.value = null;
  deleteConfirmName.value = '';
}

function onSegmentSaved(saved: SegmentResponse): void {
  const idx = segments.value.findIndex((s) => s.id === saved.id);
  if (idx >= 0) {
    segments.value[idx] = saved;
  } else {
    segments.value.unshift(saved);
  }
}

async function onDeleteSegment(): Promise<void> {
  if (!deletingSegment.value || deleteConfirmName.value !== deletingSegment.value.name) return;
  const projectId = contextStore.currentProject?.id;
  if (!projectId) return;

  isDeleting.value = true;
  try {
    await deleteSegment(projectId, deletingSegment.value.id);
    segments.value = segments.value.filter((s) => s.id !== deletingSegment.value!.id);
    closeDeleteConfirm();
  } catch {
    showError('Failed to delete segment. Please try again.');
  } finally {
    isDeleting.value = false;
  }
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
}
</script>
