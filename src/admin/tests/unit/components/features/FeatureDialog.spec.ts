import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { mount, flushPromises, type VueWrapper } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { createRouter, createMemoryHistory } from 'vue-router';
import FeatureDialog from '@/components/features/FeatureDialog.vue';
import { useContextStore } from '@/stores/context';
import type { FeatureResponse, ProjectResponse } from '@/types/api';

jest.mock('@/api/features');
jest.mock('@/api/client', () => ({
  setAuthTokens: jest.fn(),
  clearAuthTokens: jest.fn(),
  getAccessToken: jest.fn(),
}));

import * as featuresApi from '@/api/features';

const mockProject: ProjectResponse = {
  id: 10, name: 'Website', organizationId: 1, hideDisabledFlags: false, createdAt: '2026-01-01T00:00:00Z',
};
const mockFeature: FeatureResponse = {
  id: 1, name: 'dark_mode', type: 'STANDARD', initialValue: null,
  description: 'Enables dark mode', defaultEnabled: false, projectId: 10, createdAt: '2026-01-01T00:00:00Z',
};
const savedFeature: FeatureResponse = { ...mockFeature, id: 99, name: 'new_flag' };

const dialogStub = { template: '<div><slot /></div>' };

function mountDialog(props: Record<string, unknown> = {}) {
  const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/', component: { template: '<div />' } }] });
  return mount(FeatureDialog, {
    props: { modelValue: true, ...props },
    global: { plugins: [router], stubs: { 'v-dialog': dialogStub } },
  });
}

describe('FeatureDialog', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    jest.clearAllMocks();
    const contextStore = useContextStore();
    contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '', isPrimary: false });
    contextStore.setProject(mockProject);
  });

  describe('WhenOpenedInCreateMode', () => {
    it('ThenNameTypeAndDescriptionFieldsArePresent', () => {
      const wrapper = mountDialog();
      expect(wrapper.find('[data-testid="feature-name-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="feature-type-select"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="feature-description-input"]').exists()).toBe(true);
    });

    it('ThenNameAndTypeFieldsAreEnabled', () => {
      const wrapper = mountDialog();
      const nameInput = wrapper.findComponent('[data-testid="feature-name-input"]') as VueWrapper<any>;
      expect(nameInput.props('disabled')).toBeFalsy();
    });
  });

  describe('WhenOpenedInEditMode', () => {
    it('ThenNameAndTypeFieldsAreDisabled', () => {
      const wrapper = mountDialog({ feature: mockFeature });
      const nameInput = wrapper.findComponent('[data-testid="feature-name-input"]') as VueWrapper<any>;
      expect(nameInput.props('disabled')).toBe(true);
    });

    it('ThenFormIsPopulatedWithFeatureData', () => {
      const wrapper = mountDialog({ feature: mockFeature });
      const descInput = wrapper.findComponent('[data-testid="feature-description-input"]') as VueWrapper<any>;
      expect(descInput.props('modelValue')).toBe(mockFeature.description);
    });
  });

  describe('WhenFormIsSubmittedInCreateMode', () => {
    it('ThenCreateFeatureApiIsCalled', async () => {
      jest.mocked(featuresApi.createFeature).mockResolvedValue(savedFeature);

      const wrapper = mountDialog();

      // Fill in name
      await (wrapper.findComponent('[data-testid="feature-name-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'new_flag');

      await wrapper.find('[data-testid="feature-dialog-save"]').trigger('click');
      await flushPromises();

      expect(featuresApi.createFeature).toHaveBeenCalledWith(
        mockProject.id,
        expect.objectContaining({ name: 'new_flag', type: 'STANDARD' }),
      );
    });

    it('ThenSavedEventIsEmitted', async () => {
      jest.mocked(featuresApi.createFeature).mockResolvedValue(savedFeature);

      const wrapper = mountDialog();
      await (wrapper.findComponent('[data-testid="feature-name-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'new_flag');
      await wrapper.find('[data-testid="feature-dialog-save"]').trigger('click');
      await flushPromises();

      expect(wrapper.emitted('saved')).toBeTruthy();
      expect(wrapper.emitted('saved')![0]).toEqual([savedFeature]);
    });
  });

  describe('WhenFormIsSubmittedInEditMode', () => {
    it('ThenUpdateFeatureApiIsCalled', async () => {
      jest.mocked(featuresApi.updateFeature).mockResolvedValue({ ...mockFeature, description: 'Updated' });

      const wrapper = mountDialog({ feature: mockFeature });
      await (wrapper.findComponent('[data-testid="feature-description-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'Updated');
      await wrapper.find('[data-testid="feature-dialog-save"]').trigger('click');
      await flushPromises();

      expect(featuresApi.updateFeature).toHaveBeenCalledWith(
        mockProject.id,
        mockFeature.id,
        expect.objectContaining({ name: mockFeature.name }),
      );
    });
  });

  describe('WhenCancelIsClicked', () => {
    it('ThenDialogIsClosedWithoutApiCall', async () => {
      const wrapper = mountDialog();
      await wrapper.find('[data-testid="feature-dialog-cancel"]').trigger('click');

      expect(featuresApi.createFeature).not.toHaveBeenCalled();
      expect(wrapper.emitted('update:modelValue')).toBeTruthy();
      expect(wrapper.emitted('update:modelValue')![0]).toEqual([false]);
    });
  });
});
