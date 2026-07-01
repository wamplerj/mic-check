<template>
  <div data-testid="tags-chip-input">
    <span class="text-body-2 font-weight-medium">Tags</span>

    <v-alert
      v-if="errorMessage"
      type="error"
      variant="tonal"
      density="compact"
      closable
      class="my-2"
      @click:close="errorMessage = null"
    >
      {{ errorMessage }}
    </v-alert>

    <div v-if="isLoading" class="d-flex justify-center py-3">
      <v-progress-circular indeterminate size="20" width="2" color="primary" />
    </div>

    <template v-else>
      <div v-if="assignedTags.length > 0" class="d-flex flex-wrap ga-1 my-2" data-testid="tag-chips">
        <v-chip
          v-for="tag in assignedTags"
          :key="tag.id"
          size="small"
          variant="flat"
          closable
          :color="tag.color"
          :loading="togglingTagId === tag.id"
          :data-testid="`tag-chip-${tag.label}`"
          @click:close="onRemoveTag(tag)"
        >
          {{ tag.label }}
        </v-chip>
      </div>
      <p v-else class="text-body-2 text-medium-emphasis my-2" data-testid="tags-empty-message">
        No tags assigned. Type a name below to add one.
      </p>

      <div class="d-flex align-center ga-2">
        <v-combobox
          v-model="newTagLabel"
          :items="suggestions"
          label="Tag name"
          variant="outlined"
          density="compact"
          hide-details
          hide-no-data
          no-filter
          data-testid="tag-name-input"
          @keyup.enter="onAddTag"
        />
        <button
          type="button"
          style="width:36px;height:36px;border:none;padding:0;cursor:pointer;border-radius:4px"
          :style="{ background: newTagColor }"
          title="Tag Color"
          data-testid="tag-color-swatch"
          @click="showColorDialog = true"
        />
        <v-btn
          icon="ri-add-line"
          size="small"
          color="primary"
          variant="flat"
          :disabled="!newTagLabel.trim()"
          :loading="isSubmitting"
          data-testid="add-tag-btn"
          @click="onAddTag"
        />
      </div>
    </template>

    <!-- Color picker dialog -->
    <v-dialog v-model="showColorDialog" max-width="260" data-testid="color-dialog">
      <v-card rounded="lg">
        <v-card-title class="pa-4 pb-2 text-body-1">Tag Color</v-card-title>
        <v-card-text class="pa-4 pt-0">
          <div class="d-flex flex-wrap ga-2">
            <button
              v-for="color in presetColors"
              :key="color"
              type="button"
              style="width:32px;height:32px;border:none;padding:0;cursor:pointer;border-radius:4px"
              :style="{ background: color }"
              :title="color"
              :data-testid="`preset-color-${color}`"
              @click="onSelectPresetColor(color)"
            />
            <div style="position:relative;width:32px;height:32px">
              <button
                type="button"
                class="d-flex align-center justify-center"
                style="width:32px;height:32px;border:1px solid rgba(0,0,0,0.2);padding:0;cursor:pointer;border-radius:4px;background:conic-gradient(red,yellow,lime,aqua,blue,magenta,red)"
                title="Custom Color"
                data-testid="custom-color-trigger"
                @click="customColorInputRef?.click()"
              >
                <v-icon size="16" color="white">ri-palette-line</v-icon>
              </button>
              <input
                ref="customColorInputRef"
                v-model="newTagColor"
                type="color"
                style="position:absolute;inset:0;width:32px;height:32px;opacity:0;pointer-events:none"
                data-testid="custom-color-input"
                @input="showColorDialog = false"
              />
            </div>
          </div>
        </v-card-text>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { listTags, createTag } from '@/api/tags';
import { assignFeatureTag, removeFeatureTag } from '@/api/features';
import { useContextStore } from '@/stores/context';
import type { TagResponse, FeatureResponse } from '@/types/api';

const presetColors = [
  '#F44336', '#E91E63', '#9C27B0', '#673AB7', '#3F51B5',
  '#2196F3', '#03A9F4', '#00BCD4', '#009688', '#4CAF50',
  '#8BC34A', '#CDDC39', '#FFC107', '#FF9800', '#795548',
];

interface Props {
  feature: FeatureResponse;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  updated: [feature: FeatureResponse];
}>();

const contextStore = useContextStore();

const tags = ref<TagResponse[]>([]);
const isLoading = ref(false);
const isSubmitting = ref(false);
const togglingTagId = ref<number | null>(null);
const newTagLabel = ref('');
const newTagColor = ref('#1565C0');
const errorMessage = ref<string | null>(null);
const showColorDialog = ref(false);
const customColorInputRef = ref<HTMLInputElement | null>(null);

function onSelectPresetColor(color: string): void {
  newTagColor.value = color;
  showColorDialog.value = false;
}

const assignedTags = computed(() => props.feature.tags);

const suggestions = computed(() => {
  const query = newTagLabel.value.trim().toLowerCase();
  if (query.length < 2) return [];
  const assignedIds = new Set(assignedTags.value.map((t) => t.id));
  return tags.value
    .filter((t) => !assignedIds.has(t.id) && t.label.toLowerCase().includes(query))
    .map((t) => t.label);
});

function isAssigned(tag: TagResponse): boolean {
  return assignedTags.value.some((t) => t.id === tag.id);
}

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

async function onAddTag(): Promise<void> {
  const label = newTagLabel.value.trim();
  const projectId = contextStore.currentProject?.id;
  if (!label || !projectId) return;

  errorMessage.value = null;
  isSubmitting.value = true;
  try {
    let tag = tags.value.find((t) => t.label.toLowerCase() === label.toLowerCase());
    if (!tag) {
      tag = await createTag(projectId, { label, color: newTagColor.value });
      tags.value.push(tag);
    }
    if (!isAssigned(tag)) {
      togglingTagId.value = tag.id;
      const updated = await assignFeatureTag(projectId, props.feature.id, tag.id);
      emit('updated', updated);
    }
    newTagLabel.value = '';
    newTagColor.value = '#1565C0';
  } catch {
    errorMessage.value = 'Failed to add tag. Please try again.';
  } finally {
    isSubmitting.value = false;
    togglingTagId.value = null;
  }
}

async function onRemoveTag(tag: TagResponse): Promise<void> {
  const projectId = contextStore.currentProject?.id;
  if (!projectId) return;
  errorMessage.value = null;
  togglingTagId.value = tag.id;
  try {
    const updated = await removeFeatureTag(projectId, props.feature.id, tag.id);
    emit('updated', updated);
  } catch {
    errorMessage.value = 'Failed to remove tag. Please try again.';
  } finally {
    togglingTagId.value = null;
  }
}

onMounted(fetchTags);
</script>
