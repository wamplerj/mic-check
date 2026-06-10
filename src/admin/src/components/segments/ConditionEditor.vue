<template>
  <div class="d-flex align-center ga-2 flex-wrap" data-testid="condition-editor">
    <!-- Property -->
    <v-text-field
      :model-value="condition.property"
      label="Property"
      variant="outlined"
      density="compact"
      hide-details
      style="min-width: 140px; flex: 1"
      data-testid="condition-property"
      @update:model-value="update('property', $event)"
    />

    <!-- Operator -->
    <v-select
      :model-value="condition.operator"
      :items="operatorItems"
      label="Operator"
      variant="outlined"
      density="compact"
      hide-details
      style="min-width: 160px; flex: 1"
      data-testid="condition-operator"
      @update:model-value="update('operator', $event)"
    />

    <!-- Value — hidden for no-value operators -->
    <template v-if="needsValue">
      <v-slider
        v-if="condition.operator === 'PercentageSplit'"
        :model-value="percentageValue"
        :min="0"
        :max="100"
        :step="1"
        color="primary"
        hide-details
        thumb-label
        style="min-width: 160px; flex: 2"
        data-testid="condition-percentage-slider"
        @update:model-value="update('value', String($event))"
      />
      <v-text-field
        v-else
        :model-value="condition.value"
        label="Value"
        variant="outlined"
        density="compact"
        hide-details
        style="min-width: 140px; flex: 1"
        data-testid="condition-value"
        @update:model-value="update('value', $event)"
      />
    </template>

    <!-- Remove button -->
    <v-btn
      icon="ri-close-line"
      size="x-small"
      variant="text"
      color="error"
      data-testid="remove-condition-btn"
      @click="$emit('remove')"
    />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { SegmentCondition, SegmentConditionOperator } from '@/types/api';

interface Props {
  condition: SegmentCondition;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:condition': [condition: SegmentCondition];
  remove: [];
}>();

const operatorItems: { title: string; value: SegmentConditionOperator }[] = [
  { title: 'Equal', value: 'Equal' },
  { title: 'Not Equal', value: 'NotEqual' },
  { title: 'Contains', value: 'Contains' },
  { title: 'Not Contains', value: 'NotContains' },
  { title: 'Regex', value: 'Regex' },
  { title: 'Greater Than', value: 'GreaterThan' },
  { title: 'Greater Than or Equal', value: 'GreaterThanOrEqual' },
  { title: 'Less Than', value: 'LessThan' },
  { title: 'Less Than or Equal', value: 'LessThanOrEqual' },
  { title: 'Is True', value: 'IsTrue' },
  { title: 'Is False', value: 'IsFalse' },
  { title: 'In', value: 'In' },
  { title: 'Not In', value: 'NotIn' },
  { title: 'Is Set', value: 'IsSet' },
  { title: 'Is Not Set', value: 'IsNotSet' },
  { title: 'Percentage Split', value: 'PercentageSplit' },
  { title: 'Modulo', value: 'ModuloValueDivisorRemainder' },
];

const noValueOperators: SegmentConditionOperator[] = [
  'IsTrue',
  'IsFalse',
  'IsSet',
  'IsNotSet',
];

const needsValue = computed(() => !noValueOperators.includes(props.condition.operator));

const percentageValue = computed(() => {
  const n = Number(props.condition.value);
  return isNaN(n) ? 0 : n;
});

function update(field: keyof SegmentCondition, value: string): void {
  emit('update:condition', { ...props.condition, [field]: value });
}
</script>
