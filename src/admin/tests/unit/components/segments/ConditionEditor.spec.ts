import { describe, it, expect } from '@jest/globals';
import { mount, type VueWrapper } from '@vue/test-utils';
import ConditionEditor from '@/components/segments/ConditionEditor.vue';
import type { SegmentCondition } from '@/types/api';

function mountEditor(condition: SegmentCondition) {
  return mount(ConditionEditor, { props: { condition } });
}

describe('ConditionEditor', () => {
  describe('WhenThePropertyFieldIsChanged', () => {
    it('ThenUpdateConditionIsEmittedWithTheNewProperty', async () => {
      const condition: SegmentCondition = { property: 'plan', operator: 'Equal', value: 'beta' };
      const wrapper = mountEditor(condition);

      await (wrapper.findComponent('[data-testid="condition-property"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'country');

      expect(wrapper.emitted('update:condition')![0]).toEqual([{ property: 'country', operator: 'Equal', value: 'beta' }]);
    });
  });

  describe('WhenTheOperatorIsChanged', () => {
    it('ThenUpdateConditionIsEmittedWithTheNewOperator', async () => {
      const condition: SegmentCondition = { property: 'plan', operator: 'Equal', value: 'beta' };
      const wrapper = mountEditor(condition);

      await (wrapper.findComponent('[data-testid="condition-operator"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'NotEqual');

      expect(wrapper.emitted('update:condition')![0]).toEqual([{ property: 'plan', operator: 'NotEqual', value: 'beta' }]);
    });
  });

  describe('WhenTheValueFieldIsChanged', () => {
    it('ThenUpdateConditionIsEmittedWithTheNewValue', async () => {
      const condition: SegmentCondition = { property: 'plan', operator: 'Equal', value: 'beta' };
      const wrapper = mountEditor(condition);

      await (wrapper.findComponent('[data-testid="condition-value"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'gamma');

      expect(wrapper.emitted('update:condition')![0]).toEqual([{ property: 'plan', operator: 'Equal', value: 'gamma' }]);
    });
  });

  describe('WhenTheOperatorNeedsNoValue', () => {
    it.each(['IsTrue', 'IsFalse', 'IsSet', 'IsNotSet'] as const)(
      'ThenNeitherTheValueFieldNorThePercentageSliderIsShownFor %s',
      (operator) => {
        const condition: SegmentCondition = { property: 'plan', operator, value: '' };
        const wrapper = mountEditor(condition);

        expect(wrapper.find('[data-testid="condition-value"]').exists()).toBe(false);
        expect(wrapper.find('[data-testid="condition-percentage-slider"]').exists()).toBe(false);
      },
    );
  });

  describe('WhenTheOperatorIsPercentageSplit', () => {
    it('ThenThePercentageSliderIsShownInsteadOfTheValueField', () => {
      const condition: SegmentCondition = { property: 'userId', operator: 'PercentageSplit', value: '25' };
      const wrapper = mountEditor(condition);

      expect(wrapper.find('[data-testid="condition-percentage-slider"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="condition-value"]').exists()).toBe(false);
    });

    it('ThenAnInvalidStoredValueRendersAsZero', () => {
      const condition: SegmentCondition = { property: 'userId', operator: 'PercentageSplit', value: 'not-a-number' };
      const wrapper = mountEditor(condition);

      const slider = wrapper.findComponent('[data-testid="condition-percentage-slider"]') as VueWrapper<any>;
      expect(slider.props('modelValue')).toBe(0);
    });

    it('ThenMovingTheSliderEmitsUpdateConditionWithTheValueAsAString', async () => {
      const condition: SegmentCondition = { property: 'userId', operator: 'PercentageSplit', value: '25' };
      const wrapper = mountEditor(condition);

      await (wrapper.findComponent('[data-testid="condition-percentage-slider"]') as VueWrapper<any>).vm.$emit('update:modelValue', 60);

      expect(wrapper.emitted('update:condition')![0]).toEqual([{ property: 'userId', operator: 'PercentageSplit', value: '60' }]);
    });
  });

  describe('WhenTheRemoveButtonIsClicked', () => {
    it('ThenRemoveIsEmitted', async () => {
      const condition: SegmentCondition = { property: 'plan', operator: 'Equal', value: 'beta' };
      const wrapper = mountEditor(condition);

      await wrapper.find('[data-testid="remove-condition-btn"]').trigger('click');

      expect(wrapper.emitted('remove')).toBeTruthy();
    });
  });
});
