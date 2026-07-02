import { describe, it, expect } from '@jest/globals';
import { mount, type VueWrapper } from '@vue/test-utils';
import RuleGroupEditor from '@/components/segments/RuleGroupEditor.vue';
import ConditionEditor from '@/components/segments/ConditionEditor.vue';
import type { SegmentRule } from '@/types/api';

function mountEditor(rule: SegmentRule, removable = false) {
  return mount(RuleGroupEditor, { props: { rule, removable } });
}

describe('RuleGroupEditor', () => {
  describe('WhenTheRuleTypeIsAll', () => {
    it('ThenTheAndButtonIsHighlighted', () => {
      const wrapper = mountEditor({ type: 'All', conditions: [] });

      expect(wrapper.findComponent('[data-testid="rule-type-and"]').props('variant')).toBe('flat');
      expect(wrapper.findComponent('[data-testid="rule-type-or"]').props('variant')).toBe('outlined');
    });
  });

  describe('WhenTheRuleTypeIsAny', () => {
    it('ThenTheOrButtonIsHighlighted', () => {
      const wrapper = mountEditor({ type: 'Any', conditions: [] });

      expect(wrapper.findComponent('[data-testid="rule-type-and"]').props('variant')).toBe('outlined');
      expect(wrapper.findComponent('[data-testid="rule-type-or"]').props('variant')).toBe('flat');
    });
  });

  describe('WhenTheOrButtonIsClicked', () => {
    it('ThenUpdateRuleIsEmittedWithTypeAny', async () => {
      const wrapper = mountEditor({ type: 'All', conditions: [] });

      await wrapper.find('[data-testid="rule-type-or"]').trigger('click');

      expect(wrapper.emitted('update:rule')![0]).toEqual([{ type: 'Any', conditions: [] }]);
    });
  });

  describe('WhenTheAndButtonIsClicked', () => {
    it('ThenUpdateRuleIsEmittedWithTypeAll', async () => {
      const wrapper = mountEditor({ type: 'Any', conditions: [] });

      await wrapper.find('[data-testid="rule-type-and"]').trigger('click');

      expect(wrapper.emitted('update:rule')![0]).toEqual([{ type: 'All', conditions: [] }]);
    });
  });

  describe('WhenRemovableIsFalse', () => {
    it('ThenTheRemoveButtonIsNotShown', () => {
      const wrapper = mountEditor({ type: 'All', conditions: [] }, false);

      expect(wrapper.find('[data-testid="remove-rule-group-btn"]').exists()).toBe(false);
    });
  });

  describe('WhenRemovableIsTrue', () => {
    it('ThenTheRemoveButtonIsShownAndEmitsRemoveWhenClicked', async () => {
      const wrapper = mountEditor({ type: 'All', conditions: [] }, true);

      expect(wrapper.find('[data-testid="remove-rule-group-btn"]').exists()).toBe(true);

      await wrapper.find('[data-testid="remove-rule-group-btn"]').trigger('click');

      expect(wrapper.emitted('remove')).toBeTruthy();
    });
  });

  describe('WhenTheRuleHasConditions', () => {
    it('ThenAConditionEditorIsRenderedPerCondition', () => {
      const wrapper = mountEditor({
        type: 'All',
        conditions: [
          { property: 'plan', operator: 'Equal', value: 'beta' },
          { property: 'country', operator: 'Equal', value: 'US' },
        ],
      });

      expect(wrapper.findAllComponents(ConditionEditor)).toHaveLength(2);
    });
  });

  describe('WhenAConditionIsUpdated', () => {
    it('ThenUpdateRuleIsEmittedWithThatConditionReplacedInPlace', async () => {
      const wrapper = mountEditor({
        type: 'All',
        conditions: [
          { property: 'plan', operator: 'Equal', value: 'beta' },
          { property: 'country', operator: 'Equal', value: 'US' },
        ],
      });

      const conditionEditors = wrapper.findAllComponents(ConditionEditor);
      await conditionEditors[1].vm.$emit('update:condition', { property: 'country', operator: 'Equal', value: 'CA' });

      expect(wrapper.emitted('update:rule')![0]).toEqual([{
        type: 'All',
        conditions: [
          { property: 'plan', operator: 'Equal', value: 'beta' },
          { property: 'country', operator: 'Equal', value: 'CA' },
        ],
      }]);
    });
  });

  describe('WhenAConditionIsRemoved', () => {
    it('ThenUpdateRuleIsEmittedWithThatConditionFilteredOut', async () => {
      const wrapper = mountEditor({
        type: 'All',
        conditions: [
          { property: 'plan', operator: 'Equal', value: 'beta' },
          { property: 'country', operator: 'Equal', value: 'US' },
        ],
      });

      const conditionEditors = wrapper.findAllComponents(ConditionEditor);
      await conditionEditors[0].vm.$emit('remove');

      expect(wrapper.emitted('update:rule')![0]).toEqual([{
        type: 'All',
        conditions: [{ property: 'country', operator: 'Equal', value: 'US' }],
      }]);
    });
  });

  describe('WhenAddConditionIsClicked', () => {
    it('ThenUpdateRuleIsEmittedWithANewEmptyConditionAppended', async () => {
      const wrapper = mountEditor({
        type: 'All',
        conditions: [{ property: 'plan', operator: 'Equal', value: 'beta' }],
      });

      await wrapper.find('[data-testid="add-condition-btn"]').trigger('click');

      expect(wrapper.emitted('update:rule')![0]).toEqual([{
        type: 'All',
        conditions: [
          { property: 'plan', operator: 'Equal', value: 'beta' },
          { property: '', operator: 'Equal', value: '' },
        ],
      }]);
    });
  });

  describe('WhenAddGroupIsClicked', () => {
    it('ThenUpdateRuleIsEmittedWithANewChildGroupAppended', async () => {
      const wrapper = mountEditor({ type: 'All', conditions: [] });

      await wrapper.find('[data-testid="add-group-btn"]').trigger('click');

      expect(wrapper.emitted('update:rule')![0]).toEqual([{
        type: 'All',
        conditions: [],
        childRules: [{ type: 'All', conditions: [] }],
      }]);
    });
  });

  describe('WhenTheRuleHasChildRules', () => {
    it('ThenANestedRuleGroupEditorIsRenderedPerChildAndMarkedRemovable', () => {
      const wrapper = mountEditor({
        type: 'All',
        conditions: [],
        childRules: [{ type: 'Any', conditions: [{ property: 'plan', operator: 'Equal', value: 'beta' }] }],
      });

      const nested = wrapper.findAllComponents(RuleGroupEditor);
      expect(nested).toHaveLength(1);
      expect(nested[0].props('rule')).toEqual({ type: 'Any', conditions: [{ property: 'plan', operator: 'Equal', value: 'beta' }] });
      expect(nested[0].props('removable')).toBe(true);
    });
  });

  describe('WhenANestedRuleGroupIsUpdated', () => {
    it('ThenUpdateRuleIsEmittedWithThatChildReplacedInPlace', async () => {
      const wrapper = mountEditor({
        type: 'All',
        conditions: [],
        childRules: [{ type: 'Any', conditions: [] }],
      });

      const nested = wrapper.findAllComponents(RuleGroupEditor);
      const updatedChild: SegmentRule = { type: 'All', conditions: [{ property: 'x', operator: 'Equal', value: 'y' }] };
      await nested[0].vm.$emit('update:rule', updatedChild);

      expect(wrapper.emitted('update:rule')![0]).toEqual([{
        type: 'All',
        conditions: [],
        childRules: [updatedChild],
      }]);
    });
  });

  describe('WhenANestedRuleGroupIsRemoved', () => {
    it('ThenUpdateRuleIsEmittedWithThatChildFilteredOut', async () => {
      const wrapper = mountEditor({
        type: 'All',
        conditions: [],
        childRules: [
          { type: 'Any', conditions: [] },
          { type: 'All', conditions: [] },
        ],
      });

      const nested = wrapper.findAllComponents(RuleGroupEditor);
      await nested[0].vm.$emit('remove');

      expect(wrapper.emitted('update:rule')![0]).toEqual([{
        type: 'All',
        conditions: [],
        childRules: [{ type: 'All', conditions: [] }],
      }]);
    });
  });
});
