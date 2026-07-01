<template>
  <div data-testid="feature-value-editor">
    <!-- Type selector -->
    <div class="d-flex mb-3" data-testid="value-mode-toggle">
      <v-btn
        size="small"
        :variant="detectedMode === 'text' ? 'flat' : 'outlined'"
        color="primary"
        rounded="0"
        style="border-radius: 4px 0 0 4px"
        @click="detectedMode = 'text'"
      >Text</v-btn>
      <v-btn
        size="small"
        :variant="detectedMode === 'json' ? 'flat' : 'outlined'"
        color="primary"
        rounded="0"
        style="border-radius: 0 4px 4px 0; margin-left: -1px"
        @click="detectedMode = 'json'"
      >JSON</v-btn>
    </div>

    <!-- JSON mode -->
    <v-textarea
      v-if="detectedMode === 'json'"
      :model-value="modelValue ?? ''"
      label="Value (JSON)"
      variant="outlined"
      density="comfortable"
      rows="5"
      style="font-family: monospace; font-size: 0.85rem"
      :rules="[rules.validJson]"
      data-testid="json-value-input"
      @update:model-value="$emit('update:modelValue', $event || null)"
    />

    <!-- Text mode -->
    <v-text-field
      v-else
      :model-value="modelValue ?? ''"
      label="Value"
      variant="outlined"
      density="comfortable"
      clearable
      data-testid="text-value-input"
      @update:model-value="$emit('update:modelValue', $event || null)"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';

interface Props {
  modelValue: string | null;
}

const props = defineProps<Props>();

defineEmits<{
  'update:modelValue': [value: string | null];
}>();

type ValueMode = 'text' | 'json';

function detectMode(value: string | null): ValueMode {
  if (!value) return 'text';
  const trimmed = value.trim();
  if (trimmed.startsWith('{') || trimmed.startsWith('[')) return 'json';
  return 'text';
}

const detectedMode = ref<ValueMode>(detectMode(props.modelValue));

// Re-detect when value changes externally (e.g. a different feature is opened)
watch(
  () => props.modelValue,
  (v) => {
    detectedMode.value = detectMode(v);
  },
);

const rules = {
  validJson: (v: string) => {
    if (!v) return true;
    try {
      JSON.parse(v);
      return true;
    } catch {
      return 'Invalid JSON';
    }
  },
};
</script>

