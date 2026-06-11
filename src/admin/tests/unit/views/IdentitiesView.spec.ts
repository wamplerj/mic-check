import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { mount, flushPromises } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { createRouter, createMemoryHistory } from 'vue-router';
import IdentitiesView from '@/views/IdentitiesView.vue';
import { useContextStore } from '@/stores/context';
import type { ProjectResponse, EnvironmentResponse, IdentityResponse } from '@/types/api';

jest.mock('@/api/identities');
jest.mock('@/api/features');
jest.mock('@/api/client', () => ({
  setAuthTokens: jest.fn(),
  clearAuthTokens: jest.fn(),
  getAccessToken: jest.fn(),
}));

import * as identitiesApi from '@/api/identities';

const mockProject: ProjectResponse = {
  id: 10, name: 'Website', organizationId: 1, hideDisabledFlags: false, createdAt: '2026-01-01T00:00:00Z',
};
const mockEnv: EnvironmentResponse = {
  id: 100, name: 'Development', apiKey: 'env-dev', projectId: 10, createdAt: '2026-01-01T00:00:00Z',
};
const mockIdentities: IdentityResponse[] = [
  { id: 1, identifier: 'user-alice', environmentId: 100, traits: [{ key: 'plan', value: 'pro' }], createdAt: '2026-01-01T00:00:00Z' },
  { id: 2, identifier: 'user-bob', environmentId: 100, traits: [], createdAt: '2026-01-02T00:00:00Z' },
];

const dialogStub = { template: '<div><slot /></div>' };

function mountView() {
  const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/', component: { template: '<div />' } }] });
  return mount(IdentitiesView, {
    global: { plugins: [router], stubs: { 'v-dialog': dialogStub } },
  });
}

describe('IdentitiesView', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    jest.clearAllMocks();
  });

  describe('WhenNoEnvironmentIsSelected', () => {
    it('ThenEmptyStateIsShown', () => {
      const wrapper = mountView();
      expect(wrapper.find('[data-testid="identities-table"]').exists()).toBe(false);
      expect(wrapper.text()).toContain('Select an environment');
    });
  });

  describe('WhenEnvironmentIsSelected', () => {
    it('ThenIdentitiesAreLoaded', async () => {
      jest.mocked(identitiesApi.listIdentities).mockResolvedValue({
        count: 2, next: null, previous: null, results: mockIdentities,
      });

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '', isPrimary: false });
      contextStore.setProject(mockProject);
      contextStore.setEnvironment(mockEnv);

      mountView();
      await flushPromises();

      expect(identitiesApi.listIdentities).toHaveBeenCalledWith(mockEnv.apiKey, 1, 100);
    });

    it('ThenIdentitiesTableIsRendered', async () => {
      jest.mocked(identitiesApi.listIdentities).mockResolvedValue({
        count: 2, next: null, previous: null, results: mockIdentities,
      });

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '', isPrimary: false });
      contextStore.setProject(mockProject);
      contextStore.setEnvironment(mockEnv);

      const wrapper = mountView();
      await flushPromises();

      expect(wrapper.find('[data-testid="identities-table"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('user-alice');
      expect(wrapper.text()).toContain('user-bob');
    });

    it('ThenSearchFilterIsApplied', async () => {
      jest.mocked(identitiesApi.listIdentities).mockResolvedValue({
        count: 2, next: null, previous: null, results: mockIdentities,
      });

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '', isPrimary: false });
      contextStore.setProject(mockProject);
      contextStore.setEnvironment(mockEnv);

      const wrapper = mountView();
      await flushPromises();

      // Set search value
      const searchInput = wrapper.find('[data-testid="identity-search"]');
      await searchInput.find('input').setValue('alice');
      await wrapper.vm.$nextTick();

      expect(wrapper.text()).toContain('user-alice');
      expect(wrapper.text()).not.toContain('user-bob');
    });
  });

  describe('WhenViewButtonIsClicked', () => {
    it('ThenIdentityDetailIsOpened', async () => {
      jest.mocked(identitiesApi.listIdentities).mockResolvedValue({
        count: 2, next: null, previous: null, results: mockIdentities,
      });

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '', isPrimary: false });
      contextStore.setProject(mockProject);
      contextStore.setEnvironment(mockEnv);

      const wrapper = mountView();
      await flushPromises();

      await wrapper.find('[data-testid="view-identity-1"]').trigger('click');
      await wrapper.vm.$nextTick();

      const detail = wrapper.findComponent({ name: 'IdentityDetail' });
      expect(detail.props('identity')).toEqual(mockIdentities[0]);
    });
  });

  describe('WhenIdentityIsDeleted', () => {
    it('ThenIdentityIsRemovedFromList', async () => {
      jest.mocked(identitiesApi.listIdentities).mockResolvedValue({
        count: 2, next: null, previous: null, results: [...mockIdentities],
      });

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '', isPrimary: false });
      contextStore.setProject(mockProject);
      contextStore.setEnvironment(mockEnv);

      const wrapper = mountView();
      await flushPromises();

      await wrapper.findComponent({ name: 'IdentityDetail' }).vm.$emit('deleted', 1);
      await wrapper.vm.$nextTick();

      expect(wrapper.text()).not.toContain('user-alice');
      expect(wrapper.text()).toContain('user-bob');
    });
  });

  describe('WhenEnvironmentChanges', () => {
    it('ThenIdentitiesAreReloaded', async () => {
      const mockEnv2: EnvironmentResponse = {
        id: 200, name: 'Production', apiKey: 'env-prod', projectId: 10, createdAt: '2026-01-01T00:00:00Z',
      };

      jest.mocked(identitiesApi.listIdentities).mockResolvedValue({
        count: 0, next: null, previous: null, results: [],
      });

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '', isPrimary: false });
      contextStore.setProject(mockProject);
      contextStore.setEnvironment(mockEnv);

      mountView();
      await flushPromises();

      contextStore.setEnvironment(mockEnv2);
      await flushPromises();

      expect(identitiesApi.listIdentities).toHaveBeenCalledTimes(2);
      expect(identitiesApi.listIdentities).toHaveBeenLastCalledWith(mockEnv2.apiKey, 1, 100);
    });
  });
});
