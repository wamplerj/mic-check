import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { mount, flushPromises, type VueWrapper } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { createRouter, createMemoryHistory } from 'vue-router';
import IdentityDetail from '@/components/identities/IdentityDetail.vue';
import { useContextStore } from '@/stores/context';
import type { EnvironmentResponse, IdentityResponse, FeatureResponse, FeatureStateResponse, ProjectResponse } from '@/types/api';

jest.mock('@/api/identities');
jest.mock('@/api/features');
jest.mock('@/api/client', () => ({
  setAuthTokens: jest.fn(),
  clearAuthTokens: jest.fn(),
  getAccessToken: jest.fn(),
}));

import * as identitiesApi from '@/api/identities';
import * as featuresApi from '@/api/features';

const mockProject: ProjectResponse = {
  id: 10, name: 'Website', organizationId: 1, hideDisabledFlags: false, createdAt: '2026-01-01T00:00:00Z',
};
const mockEnv: EnvironmentResponse = {
  id: 100, name: 'Development', apiKey: 'env-dev', projectId: 10, createdAt: '2026-01-01T00:00:00Z',
};
const mockIdentity: IdentityResponse = {
  id: 1,
  identifier: 'user-alice',
  environmentId: 100,
  traits: [{ key: 'plan', value: 'pro' }, { key: 'country', value: 'US' }],
  createdAt: '2026-01-01T00:00:00Z',
};
const mockFeatures: FeatureResponse[] = [
  { id: 10, name: 'dark_mode', type: 'STANDARD', initialValue: null, description: null, defaultEnabled: false, projectId: 10, createdAt: '2026-01-01T00:00:00Z' },
  { id: 20, name: 'beta_feature', type: 'STANDARD', initialValue: null, description: null, defaultEnabled: false, projectId: 10, createdAt: '2026-01-01T00:00:00Z' },
];
const mockOverride: FeatureStateResponse = {
  id: 500, featureId: 10, environmentId: 100, identityId: 1, featureSegmentId: null,
  enabled: true, value: null, createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-01T00:00:00Z',
};

const dialogStub = { template: '<div><slot /></div>' };

function mountDetail(props: Record<string, unknown> = {}) {
  const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/', component: { template: '<div />' } }] });
  return mount(IdentityDetail, {
    props: { modelValue: true, identity: mockIdentity, ...props },
    global: { plugins: [router], stubs: { 'v-dialog': dialogStub } },
  });
}

describe('IdentityDetail', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    jest.clearAllMocks();
    const contextStore = useContextStore();
    contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '' });
    contextStore.setProject(mockProject);
    contextStore.setEnvironment(mockEnv);

    jest.mocked(identitiesApi.getIdentityFeatureStates).mockResolvedValue([]);
    jest.mocked(featuresApi.listFeatures).mockResolvedValue({ count: 2, next: null, previous: null, results: mockFeatures });
  });

  describe('WhenOpenedWithIdentity', () => {
    it('ThenDetailCardIsRendered', async () => {
      const wrapper = mountDetail();
      await flushPromises();
      expect(wrapper.find('[data-testid="identity-detail"]').exists()).toBe(true);
    });

    it('ThenIdentifierIsDisplayed', async () => {
      const wrapper = mountDetail();
      await flushPromises();
      expect(wrapper.text()).toContain('user-alice');
    });

    it('ThenTraitsAndOverridesTabsArePresent', async () => {
      const wrapper = mountDetail();
      await flushPromises();
      expect(wrapper.find('[data-testid="tab-traits"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="tab-overrides"]').exists()).toBe(true);
    });
  });

  describe('WhenTraitsTabIsActive', () => {
    it('ThenTraitsTableIsRendered', async () => {
      const wrapper = mountDetail();
      await flushPromises();
      expect(wrapper.find('[data-testid="traits-table"]').exists()).toBe(true);
    });

    it('ThenTraitRowsAreDisplayed', async () => {
      const wrapper = mountDetail();
      await flushPromises();
      expect(wrapper.find('[data-testid="trait-row-plan"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="trait-row-country"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('plan');
      expect(wrapper.text()).toContain('pro');
    });

    it('ThenEmptyMessageIsShownWhenNoTraits', async () => {
      const identityWithNoTraits: IdentityResponse = { ...mockIdentity, traits: [] };
      const wrapper = mountDetail({ identity: identityWithNoTraits });
      await flushPromises();
      expect(wrapper.find('[data-testid="traits-table"]').exists()).toBe(false);
    });
  });

  describe('WhenDeleteIdentityIsClicked', () => {
    it('ThenDeleteIdentityApiIsCalled', async () => {
      jest.mocked(identitiesApi.deleteIdentity).mockResolvedValue(undefined);

      const wrapper = mountDetail();
      await flushPromises();

      await wrapper.find('[data-testid="delete-identity-btn"]').trigger('click');
      await flushPromises();

      expect(identitiesApi.deleteIdentity).toHaveBeenCalledWith(mockEnv.apiKey, mockIdentity.id);
    });

    it('ThenDeletedEventIsEmitted', async () => {
      jest.mocked(identitiesApi.deleteIdentity).mockResolvedValue(undefined);

      const wrapper = mountDetail();
      await flushPromises();

      await wrapper.find('[data-testid="delete-identity-btn"]').trigger('click');
      await flushPromises();

      expect(wrapper.emitted('deleted')).toBeTruthy();
      expect(wrapper.emitted('deleted')![0]).toEqual([mockIdentity.id]);
    });
  });

  describe('WhenOverridesTabIsActive', () => {
    it('ThenNoOverridesMessageIsShownWhenEmpty', async () => {
      const wrapper = mountDetail();
      await flushPromises();

      await wrapper.find('[data-testid="tab-overrides"]').trigger('click');
      await wrapper.vm.$nextTick();

      expect(wrapper.find('[data-testid="no-overrides-message"]').exists()).toBe(true);
    });

    it('ThenExistingOverridesAreDisplayed', async () => {
      jest.mocked(identitiesApi.getIdentityFeatureStates).mockResolvedValue([mockOverride]);

      const wrapper = mountDetail();
      await flushPromises();

      await wrapper.find('[data-testid="tab-overrides"]').trigger('click');
      await wrapper.vm.$nextTick();

      expect(wrapper.find('[data-testid="overrides-table"]').exists()).toBe(true);
      expect(wrapper.find(`[data-testid="override-row-${mockOverride.featureId}"]`).exists()).toBe(true);
    });

    it('ThenFeatureNameIsShownInOverrideRow', async () => {
      jest.mocked(identitiesApi.getIdentityFeatureStates).mockResolvedValue([mockOverride]);

      const wrapper = mountDetail();
      await flushPromises();

      await wrapper.find('[data-testid="tab-overrides"]').trigger('click');
      await wrapper.vm.$nextTick();

      expect(wrapper.text()).toContain('dark_mode');
    });
  });

  describe('WhenOverrideToggleIsChanged', () => {
    it('ThenSetIdentityFeatureStateIsCalledWithNewEnabled', async () => {
      jest.mocked(identitiesApi.getIdentityFeatureStates).mockResolvedValue([mockOverride]);
      jest.mocked(identitiesApi.setIdentityFeatureState).mockResolvedValue({ ...mockOverride, enabled: false });

      const wrapper = mountDetail();
      await flushPromises();

      await wrapper.find('[data-testid="tab-overrides"]').trigger('click');
      await wrapper.vm.$nextTick();

      await (wrapper.findComponent(`[data-testid="override-toggle-${mockOverride.featureId}"]`) as VueWrapper<any>).vm.$emit('update:modelValue', false);
      await flushPromises();

      expect(identitiesApi.setIdentityFeatureState).toHaveBeenCalledWith(
        mockEnv.apiKey,
        mockIdentity.id,
        mockOverride.featureId,
        { enabled: false, value: null },
      );
    });
  });

  describe('WhenRemoveOverrideIsClicked', () => {
    it('ThenDeleteIdentityFeatureStateIsCalledAndOverrideIsRemoved', async () => {
      jest.mocked(identitiesApi.getIdentityFeatureStates).mockResolvedValue([mockOverride]);
      jest.mocked(identitiesApi.deleteIdentityFeatureState).mockResolvedValue(undefined);

      const wrapper = mountDetail();
      await flushPromises();

      await wrapper.find('[data-testid="tab-overrides"]').trigger('click');
      await wrapper.vm.$nextTick();

      await wrapper.find(`[data-testid="remove-override-${mockOverride.featureId}"]`).trigger('click');
      await flushPromises();

      expect(identitiesApi.deleteIdentityFeatureState).toHaveBeenCalledWith(
        mockEnv.apiKey,
        mockIdentity.id,
        mockOverride.featureId,
      );
      expect(wrapper.find('[data-testid="overrides-table"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="no-overrides-message"]').exists()).toBe(true);
    });
  });

  describe('WhenAddOverrideIsSubmitted', () => {
    it('ThenSetIdentityFeatureStateIsCalledWithSelectedFeature', async () => {
      const newOverride: FeatureStateResponse = { ...mockOverride, featureId: 20, enabled: true };
      jest.mocked(identitiesApi.setIdentityFeatureState).mockResolvedValue(newOverride);

      const wrapper = mountDetail();
      await flushPromises();

      await wrapper.find('[data-testid="tab-overrides"]').trigger('click');
      await wrapper.vm.$nextTick();

      await (wrapper.findComponent('[data-testid="add-override-feature-select"]') as VueWrapper<any>).vm.$emit('update:modelValue', 20);
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="add-override-btn"]').trigger('click');
      await flushPromises();

      expect(identitiesApi.setIdentityFeatureState).toHaveBeenCalledWith(
        mockEnv.apiKey,
        mockIdentity.id,
        20,
        expect.objectContaining({ enabled: false }),
      );
    });

    it('ThenOverrideIsAddedToList', async () => {
      const newOverride: FeatureStateResponse = { ...mockOverride, featureId: 20, enabled: true };
      jest.mocked(identitiesApi.setIdentityFeatureState).mockResolvedValue(newOverride);

      const wrapper = mountDetail();
      await flushPromises();

      await wrapper.find('[data-testid="tab-overrides"]').trigger('click');
      await wrapper.vm.$nextTick();

      await (wrapper.findComponent('[data-testid="add-override-feature-select"]') as VueWrapper<any>).vm.$emit('update:modelValue', 20);
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="add-override-btn"]').trigger('click');
      await flushPromises();

      expect(wrapper.find('[data-testid="overrides-table"]').exists()).toBe(true);
      expect(wrapper.find(`[data-testid="override-row-20"]`).exists()).toBe(true);
    });
  });
});
