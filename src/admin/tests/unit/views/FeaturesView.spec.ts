import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { mount, flushPromises } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { createRouter, createMemoryHistory } from 'vue-router';
import FeaturesView from '@/views/FeaturesView.vue';
import { useContextStore } from '@/stores/context';
import type { ProjectResponse, EnvironmentResponse, FeatureResponse, FeatureStateResponse } from '@/types/api';

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
const mockEnv: EnvironmentResponse = {
  id: 100, name: 'Development', apiKey: 'env-dev', projectId: 10, createdAt: '2026-01-01T00:00:00Z',
};
const mockFeatures: FeatureResponse[] = [
  { id: 1, name: 'dark_mode', type: 'STANDARD', initialValue: null, description: 'Dark mode toggle', defaultEnabled: false, projectId: 10, createdAt: '2026-01-01T00:00:00Z' },
  { id: 2, name: 'new_checkout', type: 'STANDARD', initialValue: null, description: null, defaultEnabled: true, projectId: 10, createdAt: '2026-01-01T00:00:00Z' },
];
const mockStates: FeatureStateResponse[] = [
  { id: 101, featureId: 1, environmentId: 100, identityId: null, featureSegmentId: null, enabled: false, value: null, createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-01T00:00:00Z' },
  { id: 102, featureId: 2, environmentId: 100, identityId: null, featureSegmentId: null, enabled: true, value: null, createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-01T00:00:00Z' },
];

function mountView() {
  const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/', component: { template: '<div />' } }] });
  return mount(FeaturesView, { global: { plugins: [router] } });
}

describe('FeaturesView', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    jest.clearAllMocks();
  });

  describe('WhenNoProjectIsSelected', () => {
    it('ThenEmptyStateIsShown', () => {
      const wrapper = mountView();
      expect(wrapper.find('[data-testid="features-table"]').exists()).toBe(false);
      expect(wrapper.text()).toContain('Select a project');
    });
  });

  describe('WhenProjectIsSelected', () => {
    it('ThenFeaturesAndStatesAreLoaded', async () => {
      jest.mocked(featuresApi.listFeatures).mockResolvedValue({ count: 2, next: null, previous: null, results: mockFeatures });
      jest.mocked(featureStatesApi.listFeatureStates).mockResolvedValue(mockStates);

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '' });
      contextStore.setProject(mockProject);
      contextStore.setEnvironment(mockEnv);

      mountView();
      await flushPromises();

      expect(featuresApi.listFeatures).toHaveBeenCalledWith(mockProject.id, 1, 100);
      expect(featureStatesApi.listFeatureStates).toHaveBeenCalledWith(mockEnv.apiKey);
    });

    it('ThenFeaturesTableIsRendered', async () => {
      jest.mocked(featuresApi.listFeatures).mockResolvedValue({ count: 2, next: null, previous: null, results: mockFeatures });
      jest.mocked(featureStatesApi.listFeatureStates).mockResolvedValue(mockStates);

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '' });
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      expect(wrapper.find('[data-testid="features-table"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="create-feature-btn"]').exists()).toBe(true);
    });
  });

  describe('WhenToggleEnabledIsClicked', () => {
    it('ThenPatchFeatureStateIsCalled', async () => {
      jest.mocked(featuresApi.listFeatures).mockResolvedValue({ count: 2, next: null, previous: null, results: mockFeatures });
      jest.mocked(featureStatesApi.listFeatureStates).mockResolvedValue(mockStates);
      jest.mocked(featureStatesApi.patchFeatureState).mockResolvedValue({ ...mockStates[0], enabled: true });

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '' });
      contextStore.setProject(mockProject);
      contextStore.setEnvironment(mockEnv);

      const wrapper = mountView();
      await flushPromises();

      // Find the toggle for feature id=1 and simulate a change
      const toggle = wrapper.find('[data-testid="toggle-1"]');
      expect(toggle.exists()).toBe(true);
      await toggle.findComponent({ name: 'VSwitch' }).vm.$emit('update:modelValue', true);
      await flushPromises();

      expect(featureStatesApi.patchFeatureState).toHaveBeenCalledWith(mockEnv.apiKey, 101, { enabled: true });
    });
  });

  describe('WhenCreateButtonIsClicked', () => {
    it('ThenFeatureDialogIsOpened', async () => {
      jest.mocked(featuresApi.listFeatures).mockResolvedValue({ count: 0, next: null, previous: null, results: [] });
      jest.mocked(featureStatesApi.listFeatureStates).mockResolvedValue([]);

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '' });
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      await wrapper.find('[data-testid="create-feature-btn"]').trigger('click');
      await wrapper.vm.$nextTick();

      // FeatureDialog should now be open (v-dialog with model-value=true)
      const dialog = wrapper.findComponent({ name: 'FeatureDialog' });
      expect(dialog.exists()).toBe(true);
    });
  });
});
