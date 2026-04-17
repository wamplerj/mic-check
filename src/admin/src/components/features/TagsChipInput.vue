<template>
  <div data-testid="tags-chip-input">
    <div class="d-flex align-center justify-space-between mb-2">
      <span class="text-body-2 font-weight-medium">Project Tags</span>
      <v-btn
        size="x-small"
        variant="tonal"
        color="primary"
        prepend-icon="ri-add-line"
        :loading="isCreating"
        data-testid="create-tag-btn"
        @click="showCreateForm = !showCreateForm"
      >
        New tag
      </v-btn>
    </div>

    <v-alert
      v-if="errorMessage"
      type="error"
      variant="tonal"
      density="compact"
      closable
      class="mb-2"
      @click:close="errorMessage = null"
    >
      {{ errorMessage }}
    </v-alert>

    <!-- Create form -->
    <v-expand-transition>
      <v-card v-if="showCreateForm" variant="outlined" rounded="lg" class="mb-3 pa-3">
        <v-form ref="createFormRef" @submit.prevent="onCreateTag">
          <div class="d-flex align-center ga-2">
            <v-text-field
              v-model="newTagLabel"
              label="Tag name"
              variant="outlined"
              density="compact"
              hide-details
              :rules="[rules.required]"
              data-testid="new-tag-label-input"
            />
            <input
              v-model="newTagColor"
              type="color"
              style="width:36px;height:36px;border:none;padding:2px;cursor:pointer;border-radius:4px"
              title="Tag colour"
              data-testid="new-tag-color-input"
            />
            <v-btn
              icon="ri-check-line"
              size="small"
              color="primary"
              variant="flat"
              type="submit"
              :loading="isCreating"
              data-testid="confirm-create-tag-btn"
              @click.prevent="onCreateTag"
            />
            <v-btn
              icon="ri-close-line"
              size="small"
              variant="text"
              @click="showCreateForm = false"
            />
          </div>
        </v-form>
      </v-card>
    </v-expand-transition>

    <!-- Tag chips -->
    <div v-if="isLoading" class="d-flex justify-center py-3">
      <v-progress-circular indeterminate size="20" width="2" color="primary" />
    </div>

    <div v-else-if="tags.length > 0" class="d-flex flex-wrap ga-1" data-testid="tag-chips">
      <v-chip
        v-for="tag in tags"
        :key="tag.id"
        size="small"
        closable
        :color="tag.color"
        :data-testid="`tag-chip-${tag.label}`"
        @click:close="onDeleteTag(tag)"
      >
        {{ tag.label }}
      </v-chip>
    </div>

    <p
      v-else
      class="text-body-2 text-medium-emphasis"
      data-testid="tags-empty-message"
    >
      No tags yet. Create one to organise your features.
    </p>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { listTags, createTag, deleteTag } from '@/api/tags';
import { useContextStore } from '@/stores/context';
import type { TagResponse } from '@/types/api';

const contextStore = useContextStore();

const tags = ref<TagResponse[]>([]);
const isLoading = ref(false);
const isCreating = ref(false);
const showCreateForm = ref(false);
const newTagLabel = ref('');
const newTagColor = ref('#1565C0');
const errorMessage = ref<string | null>(null);
const createFormRef = ref<{ validate: () => Promise<{ valid: boolean }> } | null>(null);

const rules = {
  required: (v: string) => !!v || 'Tag name is required',
};

async function fetchTags(): Promise<void> {
  const projectId = contextStore.currentProject?.id;
  if (!projectId) return;
  isLoading.value = true;
  try {
    tags.value = await listTags(projectId);
  } catch {
    tags.value = [];
    errorMessage.value = 'Failed to load tags.';
  } finally {
    isLoading.value = false;
  }
}

async function onCreateTag(): Promise<void> {
  if (!createFormRef.value) return;
  const { valid } = await createFormRef.value.validate();
  if (!valid) return;

  const projectId = contextStore.currentProject?.id;
  if (!projectId) return;

  isCreating.value = true;
  errorMessage.value = null;
  try {
    const created = await createTag(projectId, {
      label: newTagLabel.value.trim(),
      color: newTagColor.value,
    });
    tags.value.push(created);
    newTagLabel.value = '';
    newTagColor.value = '#1565C0';
    showCreateForm.value = false;
  } catch {
    errorMessage.value = 'Failed to create tag. Please try again.';
  } finally {
    isCreating.value = false;
  }
}

async function onDeleteTag(tag: TagResponse): Promise<void> {
  const projectId = contextStore.currentProject?.id;
  if (!projectId) return;
  errorMessage.value = null;
  try {
    await deleteTag(projectId, tag.id);
    tags.value = tags.value.filter((t) => t.id !== tag.id);
  } catch {
    errorMessage.value = 'Failed to delete tag. Please try again.';
  }
}

onMounted(fetchTags);
</script>
