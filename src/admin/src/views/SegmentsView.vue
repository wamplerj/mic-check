<template>
  <div>
    <div class="d-flex align-center justify-space-between mb-4">
      <h1 class="text-h5 font-weight-bold">Segments</h1>
      <v-btn
        v-if="contextStore.currentProject"
        color="primary"
        prepend-icon="ri-add-line"
        data-testid="create-segment-btn"
        @click="openCreateDialog"
      >
        Create Segment
      </v-btn>
    </div>

    <!-- No project selected -->
    <v-card v-if="!contextStore.currentProject" variant="outlined" rounded="lg">
      <v-card-text class="text-center py-10">
        <v-icon size="48" color="medium-emphasis" class="mb-3">ri-group-line</v-icon>
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
          @click:row="(_: Event, { item }: { item: SegmentResponse }) => openEditDialog(item)"
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
            <div class="d-flex align-center justify-end">
              <v-btn
                icon="ri-delete-bin-line"
                size="small"
                variant="text"
                color="error"
                :data-testid="`delete-segment-${item.id}`"
                @click.stop="openDeleteConfirm(item)"
              />
            </div>
          </template>

          <!-- Empty state -->
          <template #no-data>
            <div class="text-center py-8">
              <v-icon size="40" color="medium-emphasis" class="mb-2">ri-group-line</v-icon>
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
    <v-dialog v-model="showDeleteDialog" max-width="400" persistent>
      <v-card rounded="lg" data-testid="delete-confirm-dialog">
        <v-card-title class="text-body-1 font-weight-bold pa-4 pb-2">Delete Segment</v-card-title>
        <v-card-text class="pa-4 pt-0">
          <p class="text-body-2">
            Delete <strong>{{ deletingSegment?.name }}</strong>? This cannot be undone.
          </p>
        </v-card-text>
        <v-card-actions class="pa-4 pt-0">
          <v-spacer />
          <v-btn variant="text" :disabled="isDeleting" @click="closeDeleteConfirm">Cancel</v-btn>
          <v-btn
            color="error"
            variant="flat"
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
const isDeleting = ref(false);

const headers = [
  { title: 'Name', key: 'name', sortable: true },
  { title: 'Rules', key: 'rules', sortable: false, width: '80' },
  { title: 'Created', key: 'createdAt', sortable: true, width: '130' },
  { title: '', key: 'actions', sortable: false, width: '52', align: 'end' as const },
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
  showDeleteDialog.value = true;
}

function closeDeleteConfirm(): void {
  showDeleteDialog.value = false;
  deletingSegment.value = null;
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
  if (!deletingSegment.value) return;
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
