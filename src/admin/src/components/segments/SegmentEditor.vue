<template>
  <v-dialog
    :model-value="modelValue"
    max-width="680"
    scrollable
    @update:model-value="$emit('update:modelValue', $event)"
  >
    <v-card rounded="lg" data-testid="segment-editor">
      <v-card-title class="d-flex align-center justify-space-between pa-6 pb-3">
        <span class="text-h6">{{ segment ? 'Edit Segment' : 'Create Segment' }}</span>
        <v-btn icon="mdi-close" variant="text" size="small" @click="$emit('update:modelValue', false)" />
      </v-card-title>
      <v-divider />

      <v-card-text class="pa-6">
        <v-alert
          v-if="errorMessage"
          type="error"
          variant="tonal"
          density="compact"
          closable
          class="mb-4"
          @click:close="errorMessage = null"
        >
          {{ errorMessage }}
        </v-alert>

        <!-- Name -->
        <v-form ref="formRef" data-testid="segment-form">
          <v-text-field
            v-model="form.name"
            label="Segment name"
            variant="outlined"
            density="comfortable"
            class="mb-4"
            :rules="[rules.required]"
            :counter="150"
            data-testid="segment-name-input"
          />
        </v-form>

        <!-- Rules -->
        <p class="text-body-2 font-weight-medium mb-2">Rules</p>

        <div v-if="form.rules.length === 0" class="text-center py-4 text-medium-emphasis text-body-2" data-testid="no-rules-message">
          No rules yet. Add a rule group to start targeting users.
        </div>

        <div class="d-flex flex-column ga-3 mb-3">
          <RuleGroupEditor
            v-for="(rule, ri) in form.rules"
            :key="ri"
            :rule="rule"
            :removable="true"
            @update:rule="onUpdateRule(ri, $event)"
            @remove="onRemoveRule(ri)"
          />
        </div>

        <v-btn
          size="small"
          variant="tonal"
          color="primary"
          prepend-icon="mdi-plus"
          data-testid="add-rule-group-btn"
          @click="onAddRuleGroup"
        >
          Add rule group
        </v-btn>
      </v-card-text>

      <v-divider />
      <v-card-actions class="pa-4">
        <v-spacer />
        <v-btn variant="text" @click="$emit('update:modelValue', false)">Cancel</v-btn>
        <v-btn
          color="primary"
          variant="flat"
          :loading="isSaving"
          data-testid="save-segment-btn"
          @click="onSave"
        >
          {{ segment ? 'Save' : 'Create' }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { createSegment, updateSegment } from '@/api/segments';
import { useContextStore } from '@/stores/context';
import RuleGroupEditor from './RuleGroupEditor.vue';
import type { SegmentResponse, SegmentRule } from '@/types/api';

interface Props {
  modelValue: boolean;
  segment?: SegmentResponse | null;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  saved: [segment: SegmentResponse];
}>();

const contextStore = useContextStore();

const formRef = ref<{ validate: () => Promise<{ valid: boolean }> } | null>(null);
const isSaving = ref(false);
const errorMessage = ref<string | null>(null);

const form = ref<{ name: string; rules: SegmentRule[] }>({
  name: '',
  rules: [],
});

const rules = {
  required: (v: string) => !!v || 'Required',
};

function resetForm(): void {
  if (props.segment) {
    form.value = {
      name: props.segment.name,
      rules: JSON.parse(JSON.stringify(props.segment.rules)),
    };
  } else {
    form.value = { name: '', rules: [] };
  }
  errorMessage.value = null;
}

watch(() => props.modelValue, (open) => {
  if (open) resetForm();
});

watch(() => props.segment, resetForm, { immediate: true });

function onAddRuleGroup(): void {
  form.value.rules = [...form.value.rules, { type: 'All', conditions: [] }];
}

function onUpdateRule(index: number, rule: SegmentRule): void {
  const updated = [...form.value.rules];
  updated[index] = rule;
  form.value.rules = updated;
}

function onRemoveRule(index: number): void {
  form.value.rules = form.value.rules.filter((_, i) => i !== index);
}

async function onSave(): Promise<void> {
  if (!formRef.value) return;
  const { valid } = await formRef.value.validate();
  if (!valid) return;

  const projectId = contextStore.currentProject?.id;
  if (!projectId) return;

  isSaving.value = true;
  errorMessage.value = null;
  try {
    const payload = { name: form.value.name, rules: form.value.rules };
    const saved = props.segment
      ? await updateSegment(projectId, props.segment.id, payload)
      : await createSegment(projectId, payload);
    emit('saved', saved);
    emit('update:modelValue', false);
  } catch {
    errorMessage.value = 'Failed to save segment. Please try again.';
  } finally {
    isSaving.value = false;
  }
}
</script>
