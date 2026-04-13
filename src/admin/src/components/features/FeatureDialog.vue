<template>
  <v-dialog
    :model-value="modelValue"
    max-width="560"
    persistent
    @update:model-value="$emit('update:modelValue', $event)"
  >
    <v-card rounded="lg">
      <v-card-title class="pa-6 pb-2 text-h6">
        {{ isEditing ? 'Edit Feature' : 'Create Feature' }}
      </v-card-title>

      <v-card-text class="pa-6 pt-2">
        <v-alert
          v-if="errorMessage"
          type="error"
          variant="tonal"
          closable
          class="mb-4"
          @click:close="errorMessage = null"
        >
          {{ errorMessage }}
        </v-alert>

        <v-form ref="formRef" @submit.prevent="onSubmit" data-testid="feature-form">
          <!-- Name -->
          <v-text-field
            v-model="form.name"
            label="Name"
            variant="outlined"
            density="comfortable"
            class="mb-3"
            :rules="[rules.required, rules.nameFormat]"
            :counter="150"
            :maxlength="150"
            :disabled="isEditing"
            hint="Letters, numbers, hyphens and underscores only"
            persistent-hint
            data-testid="feature-name-input"
          />

          <!-- Type -->
          <v-select
            v-model="form.type"
            label="Type"
            :items="typeOptions"
            variant="outlined"
            density="comfortable"
            class="mb-3"
            :rules="[rules.required]"
            :disabled="isEditing"
            data-testid="feature-type-select"
          />

          <!-- Initial Value (MULTIVARIATE only, create mode only) -->
          <v-textarea
            v-if="form.type === 'MULTIVARIATE' && !isEditing"
            v-model="form.initialValue"
            label="Initial Value"
            variant="outlined"
            density="comfortable"
            class="mb-3"
            rows="3"
            :counter="20000"
            :maxlength="20000"
            hint="Default value delivered to all environments"
            persistent-hint
            data-testid="feature-initial-value-input"
          />

          <!-- Description -->
          <v-textarea
            v-model="form.description"
            label="Description"
            variant="outlined"
            density="comfortable"
            rows="2"
            data-testid="feature-description-input"
          />
        </v-form>
      </v-card-text>

      <v-card-actions class="pa-6 pt-0">
        <v-spacer />
        <v-btn
          variant="text"
          :disabled="isSaving"
          data-testid="feature-dialog-cancel"
          @click="onCancel"
        >
          Cancel
        </v-btn>
        <v-btn
          color="primary"
          variant="flat"
          :loading="isSaving"
          data-testid="feature-dialog-save"
          @click="onSubmit"
        >
          {{ isEditing ? 'Save' : 'Create' }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import { createFeature, updateFeature } from '@/api/features';
import { useContextStore } from '@/stores/context';
import type { FeatureResponse, FeatureType } from '@/types/api';

interface Props {
  modelValue: boolean;
  feature?: FeatureResponse | null;
}

const props = withDefaults(defineProps<Props>(), {
  feature: null,
});

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  saved: [feature: FeatureResponse];
}>();

const contextStore = useContextStore();

const formRef = ref<{ validate: () => Promise<{ valid: boolean }> } | null>(null);
const isSaving = ref(false);
const errorMessage = ref<string | null>(null);

const form = ref({
  name: '',
  type: 'STANDARD' as FeatureType,
  initialValue: '',
  description: '',
});

const isEditing = computed(() => props.feature !== null);

const typeOptions = [
  { title: 'Standard (boolean/string)', value: 'STANDARD' },
  { title: 'Multivariate (A/B variants)', value: 'MULTIVARIATE' },
];

const rules = {
  required: (v: string) => !!v || 'This field is required',
  nameFormat: (v: string) =>
    /^[a-zA-Z0-9_-]+$/.test(v) || 'Only letters, numbers, hyphens and underscores allowed',
};

function resetForm(): void {
  if (props.feature) {
    form.value = {
      name: props.feature.name,
      type: props.feature.type,
      initialValue: props.feature.initialValue ?? '',
      description: props.feature.description ?? '',
    };
  } else {
    form.value = { name: '', type: 'STANDARD', initialValue: '', description: '' };
  }
}

// Populate form when editing an existing feature
watch(() => props.feature, resetForm, { immediate: true });

// Reset form and clear errors each time the dialog opens
watch(
  () => props.modelValue,
  (open) => {
    if (open) {
      errorMessage.value = null;
      resetForm();
    }
  },
);

async function onSubmit(): Promise<void> {
  if (!formRef.value) return;
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  const projectId = contextStore.currentProject?.id;
  if (!projectId) return;

  isSaving.value = true;
  errorMessage.value = null;
  try {
    let saved: FeatureResponse;
    if (isEditing.value && props.feature) {
      saved = await updateFeature(projectId, props.feature.id, {
        name: form.value.name,
        description: form.value.description || null,
      });
    } else {
      saved = await createFeature(projectId, {
        name: form.value.name,
        type: form.value.type,
        initialValue: form.value.initialValue || null,
        description: form.value.description || null,
      });
    }
    emit('saved', saved);
    emit('update:modelValue', false);
  } catch {
    errorMessage.value = 'Failed to save feature. Please try again.';
  } finally {
    isSaving.value = false;
  }
}

function onCancel(): void {
  emit('update:modelValue', false);
}
</script>
