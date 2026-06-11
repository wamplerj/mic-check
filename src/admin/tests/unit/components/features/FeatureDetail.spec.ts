import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { mount, flushPromises, type VueWrapper } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { createRouter, createMemoryHistory } from 'vue-router';
import FeatureDetail from '@/components/features/FeatureDetail.vue';
import { useContextStore } from '@/stores/context';
import type { FeatureResponse, FeatureStateResponse, ProjectResponse } from '@/types/api';

jest.mock('@/api/features');
jest.mock('@/api/featureStates');
jest.mock('@/api/tags');
jest.mock('@/api/client', () => ({
  setAuthTokens: jest.fn(),
  clearAuthTokens: jest.fn(),
  getAccessToken: jest.fn(),
}));

import * as featuresApi from '@/api/features';
import * as featureStatesApi from '@/api/featureStates';

const mockProject: ProjectResponse = {
  id: 10, name: 'Website', organizationId: 1, hideDisabledFlags: false, createdAt: '2026-01-01T00:00:00Z',
};
const mockFeature: FeatureResponse = {
  id: 1, name: 'dark_mode', type: 'STANDARD', initialValue: null,
  description: 'Dark mode toggle', defaultEnabled: false, projectId: 10, createdAt: '2026-01-01T00:00:00Z',
};
const mockFeatureState: FeatureStateResponse = {
  id: 101, featureId: 1, environmentId: 100, identityId: null, featureSegmentId: null,
  enabled: false, value: null, createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-01T00:00:00Z',
};

const dialogStub = { template: '<div><slot /></div>' };
const tagsStub = { template: '<div data-testid="tags-chip-input" />' };

function mountDetail(props: Record<string, unknown> = {}) {
  const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/', component: { template: '<div />' } }] });
  return mount(FeatureDetail, {
    props: { modelValue: true, feature: mockFeature, featureState: null, ...props },
    global: { plugins: [router], stubs: { 'v-dialog': dialogStub, TagsChipInput: tagsStub } },
  });
}

describe('FeatureDetail', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    jest.clearAllMocks();
    const contextStore = useContextStore();
    contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '', isPrimary: false });
    contextStore.setProject(mockProject);
    contextStore.setEnvironment({ id: 100, name: 'Development', apiKey: 'env-dev', projectId: 10, createdAt: '' });
  });

  describe('WhenOpenedWithFeature', () => {
    it('ThenFeatureDetailCardIsRendered', () => {
      const wrapper = mountDetail();
      expect(wrapper.find('[data-testid="feature-detail"]').exists()).toBe(true);
    });

    it('ThenFeatureNameIsDisplayed', () => {
      const wrapper = mountDetail();
      expect(wrapper.text()).toContain('dark_mode');
    });

    it('ThenValueAndSettingsTabsArePresent', () => {
      const wrapper = mountDetail();
      expect(wrapper.find('[data-testid="tab-value"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="tab-settings"]').exists()).toBe(true);
    });
  });

  describe('WhenNoEnvironmentIsSelected', () => {
    it('ThenSelectEnvironmentMessageIsShown', () => {
      const wrapper = mountDetail({ featureState: null });
      expect(wrapper.text()).toContain('Select an environment');
    });
  });

  describe('WhenFeatureStateIsProvided', () => {
    it('ThenEnabledToggleIsRendered', () => {
      const wrapper = mountDetail({ featureState: mockFeatureState });
      expect(wrapper.find('[data-testid="feature-enabled-toggle"]').exists()).toBe(true);
    });

    it('ThenPatchFeatureStateIsCalledOnToggle', async () => {
      jest.mocked(featureStatesApi.patchFeatureState).mockResolvedValue({ ...mockFeatureState, enabled: true });

      const wrapper = mountDetail({ featureState: mockFeatureState });

      await (wrapper.find('[data-testid="feature-enabled-toggle"]').findComponent({ name: 'VSwitch' }) as VueWrapper<any>).vm.$emit('update:modelValue', true);
      await flushPromises();

      expect(featureStatesApi.patchFeatureState).toHaveBeenCalledWith('env-dev', mockFeatureState.id, { enabled: true });
    });

    it('ThenStateUpdatedEventIsEmittedAfterToggle', async () => {
      const updatedState = { ...mockFeatureState, enabled: true };
      jest.mocked(featureStatesApi.patchFeatureState).mockResolvedValue(updatedState);

      const wrapper = mountDetail({ featureState: mockFeatureState });

      await (wrapper.find('[data-testid="feature-enabled-toggle"]').findComponent({ name: 'VSwitch' }) as VueWrapper<any>).vm.$emit('update:modelValue', true);
      await flushPromises();

      expect(wrapper.emitted('stateUpdated')).toBeTruthy();
      expect(wrapper.emitted('stateUpdated')![0]).toEqual([updatedState]);
    });
  });

  describe('WhenSaveValueIsClicked', () => {
    it('ThenPatchFeatureStateIsCalledWithEditedValue', async () => {
      const stateWithValue: FeatureStateResponse = { ...mockFeatureState, value: 'old_value' };
      const updated = { ...stateWithValue, value: 'new_value' };
      jest.mocked(featureStatesApi.patchFeatureState).mockResolvedValue(updated);

      const wrapper = mountDetail({ featureState: stateWithValue });

      await (wrapper.findComponent({ name: 'FeatureValueEditor' }) as VueWrapper<any>).vm.$emit('update:modelValue', 'new_value');
      await wrapper.find('[data-testid="save-value-btn"]').trigger('click');
      await flushPromises();

      expect(featureStatesApi.patchFeatureState).toHaveBeenCalledWith('env-dev', stateWithValue.id, { value: 'new_value' });
    });

    it('ThenStateUpdatedEventIsEmittedAfterValueSave', async () => {
      const stateWithValue: FeatureStateResponse = { ...mockFeatureState, value: 'old_value' };
      const updated = { ...stateWithValue, value: 'new_value' };
      jest.mocked(featureStatesApi.patchFeatureState).mockResolvedValue(updated);

      const wrapper = mountDetail({ featureState: stateWithValue });

      await (wrapper.findComponent({ name: 'FeatureValueEditor' }) as VueWrapper<any>).vm.$emit('update:modelValue', 'new_value');
      await wrapper.find('[data-testid="save-value-btn"]').trigger('click');
      await flushPromises();

      expect(wrapper.emitted('stateUpdated')).toBeTruthy();
      expect(wrapper.emitted('stateUpdated')![0]).toEqual([updated]);
    });

    it('ThenSaveIsBlockedWhenValueIsInvalidJson', async () => {
      const stateWithValue: FeatureStateResponse = { ...mockFeatureState, value: '{"key": "val"}' };

      const wrapper = mountDetail({ featureState: stateWithValue });

      await (wrapper.findComponent({ name: 'FeatureValueEditor' }) as VueWrapper<any>).vm.$emit('update:modelValue', '{bad json}');
      await wrapper.find('[data-testid="save-value-btn"]').trigger('click');
      await flushPromises();

      expect(featureStatesApi.patchFeatureState).not.toHaveBeenCalled();
    });
  });

  describe('WhenSettingsFormIsSubmitted', () => {
    it('ThenPatchFeatureIsCalledWithFormValues', async () => {
      const updated = { ...mockFeature, description: 'Updated desc' };
      jest.mocked(featuresApi.patchFeature).mockResolvedValue(updated);

      const wrapper = mountDetail();

      // Switch to settings tab
      await wrapper.find('[data-testid="tab-settings"]').trigger('click');
      await wrapper.vm.$nextTick();

      await (wrapper.findComponent('[data-testid="settings-description-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'Updated desc');
      await wrapper.find('[data-testid="save-settings-btn"]').trigger('click');
      await flushPromises();

      expect(featuresApi.patchFeature).toHaveBeenCalledWith(
        mockProject.id,
        mockFeature.id,
        expect.objectContaining({ name: mockFeature.name }),
      );
    });

    it('ThenUpdatedEventIsEmittedAfterSave', async () => {
      const updated = { ...mockFeature, description: 'Updated desc' };
      jest.mocked(featuresApi.patchFeature).mockResolvedValue(updated);

      const wrapper = mountDetail();

      await wrapper.find('[data-testid="tab-settings"]').trigger('click');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="save-settings-btn"]').trigger('click');
      await flushPromises();

      expect(wrapper.emitted('updated')).toBeTruthy();
    });
  });

  describe('WhenDeleteConfirmationIsEntered', () => {
    it('ThenDeleteButtonIsDisabledWhenNameDoesNotMatch', async () => {
      const wrapper = mountDetail();

      await wrapper.find('[data-testid="tab-settings"]').trigger('click');
      await wrapper.vm.$nextTick();

      const deleteBtn = wrapper.findComponent('[data-testid="delete-feature-btn"]') as VueWrapper<any>;
      expect(deleteBtn.props('disabled')).toBe(true);
    });

    it('ThenDeleteFeatureIsCalledWhenNameMatches', async () => {
      jest.mocked(featuresApi.deleteFeature).mockResolvedValue(undefined);

      const wrapper = mountDetail();

      await wrapper.find('[data-testid="tab-settings"]').trigger('click');
      await wrapper.vm.$nextTick();

      // Type the feature name into the confirm input
      await (wrapper.findComponent('[data-testid="delete-confirm-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', mockFeature.name);
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="delete-feature-btn"]').trigger('click');
      await flushPromises();

      expect(featuresApi.deleteFeature).toHaveBeenCalledWith(mockProject.id, mockFeature.id);
    });

    it('ThenDeletedEventIsEmittedAfterDeletion', async () => {
      jest.mocked(featuresApi.deleteFeature).mockResolvedValue(undefined);

      const wrapper = mountDetail();

      await wrapper.find('[data-testid="tab-settings"]').trigger('click');
      await wrapper.vm.$nextTick();

      await (wrapper.findComponent('[data-testid="delete-confirm-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', mockFeature.name);
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="delete-feature-btn"]').trigger('click');
      await flushPromises();

      expect(wrapper.emitted('deleted')).toBeTruthy();
      expect(wrapper.emitted('deleted')![0]).toEqual([mockFeature.id]);
    });
  });
});
