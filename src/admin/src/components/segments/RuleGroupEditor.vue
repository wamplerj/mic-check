<template>
  <v-card variant="outlined" rounded="lg" class="pa-3" data-testid="rule-group-editor">
    <!-- AND / OR toggle -->
    <div class="d-flex align-center ga-2 mb-3">
      <span class="text-body-2 font-weight-medium text-medium-emphasis">Match</span>
      <v-btn-toggle
        :model-value="rule.type"
        density="compact"
        mandatory
        color="primary"
        variant="outlined"
        data-testid="rule-type-toggle"
        @update:model-value="onTypeChange"
      >
        <v-btn value="AND" size="small" data-testid="rule-type-and">ALL (AND)</v-btn>
        <v-btn value="OR" size="small" data-testid="rule-type-or">ANY (OR)</v-btn>
      </v-btn-toggle>
      <v-spacer />
      <v-btn
        v-if="removable"
        icon="mdi-trash-can-outline"
        size="x-small"
        variant="text"
        color="error"
        data-testid="remove-rule-group-btn"
        @click="$emit('remove')"
      />
    </div>

    <!-- Conditions -->
    <div class="d-flex flex-column ga-2 mb-3">
      <ConditionEditor
        v-for="(condition, ci) in rule.conditions"
        :key="ci"
        :condition="condition"
        @update:condition="onUpdateCondition(Number(ci), $event)"
        @remove="onRemoveCondition(Number(ci))"
      />
    </div>

    <!-- Child rule groups -->
    <div v-if="rule.childRules && rule.childRules.length > 0" class="d-flex flex-column ga-2 mb-3">
      <RuleGroupEditor
        v-for="(child, ri) in rule.childRules"
        :key="ri"
        :rule="child"
        :removable="true"
        @update:rule="onUpdateChildRule(Number(ri), $event)"
        @remove="onRemoveChildRule(Number(ri))"
      />
    </div>

    <!-- Add buttons -->
    <div class="d-flex ga-2">
      <v-btn
        size="x-small"
        variant="tonal"
        color="primary"
        prepend-icon="mdi-plus"
        data-testid="add-condition-btn"
        @click="onAddCondition"
      >
        Add condition
      </v-btn>
      <v-btn
        size="x-small"
        variant="tonal"
        color="secondary"
        prepend-icon="mdi-plus"
        data-testid="add-group-btn"
        @click="onAddChildGroup"
      >
        Add group
      </v-btn>
    </div>
  </v-card>
</template>

<script setup lang="ts">
import type { SegmentRule, SegmentCondition, SegmentRuleType } from '@/types/api';
import ConditionEditor from './ConditionEditor.vue';

interface Props {
  rule: SegmentRule;
  removable?: boolean;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:rule': [rule: SegmentRule];
  remove: [];
}>();

function onTypeChange(type: string): void {
  emit('update:rule', { ...props.rule, type: type as SegmentRuleType });
}

function onUpdateCondition(index: number, condition: SegmentCondition): void {
  const conditions = [...props.rule.conditions];
  conditions[index] = condition;
  emit('update:rule', { ...props.rule, conditions });
}

function onRemoveCondition(index: number): void {
  const conditions = props.rule.conditions.filter((_: SegmentCondition, i: number) => i !== index);
  emit('update:rule', { ...props.rule, conditions });
}

function onAddCondition(): void {
  const newCondition: SegmentCondition = { property: '', operator: 'Equal', value: '' };
  emit('update:rule', { ...props.rule, conditions: [...props.rule.conditions, newCondition] });
}

function onAddChildGroup(): void {
  const newGroup: SegmentRule = { type: 'AND', conditions: [] };
  const childRules = [...(props.rule.childRules ?? []), newGroup];
  emit('update:rule', { ...props.rule, childRules });
}

function onUpdateChildRule(index: number, child: SegmentRule): void {
  const childRules = [...(props.rule.childRules ?? [])];
  childRules[index] = child;
  emit('update:rule', { ...props.rule, childRules });
}

function onRemoveChildRule(index: number): void {
  const childRules = (props.rule.childRules ?? []).filter((_: SegmentRule, i: number) => i !== index);
  emit('update:rule', { ...props.rule, childRules });
}
</script>
