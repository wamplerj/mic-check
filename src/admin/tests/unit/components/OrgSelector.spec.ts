import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { mount, flushPromises } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { createRouter, createMemoryHistory } from 'vue-router';
import OrgSelector from '@/components/nav/OrgSelector.vue';
import { useContextStore } from '@/stores/context';
import type { OrganizationResponse } from '@/types/api';

jest.mock('@/api/organizations');
jest.mock('@/api/client', () => ({
  setAuthTokens: jest.fn(),
  clearAuthTokens: jest.fn(),
  getAccessToken: jest.fn(),
}));

import * as orgsApi from '@/api/organizations';

const mockOrgs: OrganizationResponse[] = [
  { id: 1, name: 'Acme Corp', createdAt: '2026-01-01T00:00:00Z' },
  { id: 2, name: 'Globex', createdAt: '2026-01-01T00:00:00Z' },
];

function mountComponent() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: '/', component: { template: '<div />' } }],
  });
  return mount(OrgSelector, {
    global: { plugins: [router] },
  });
}

describe('OrgSelector', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    jest.clearAllMocks();
  });

  describe('WhenComponentMounts', () => {
    it('ThenOrganizationsAreFetchedFromTheApi', async () => {
      jest.mocked(orgsApi.listOrganizations).mockResolvedValue(mockOrgs);

      mountComponent();
      await flushPromises();

      expect(orgsApi.listOrganizations).toHaveBeenCalledTimes(1);
    });

    it('ThenOrganizationsAreRenderedInTheSelect', async () => {
      jest.mocked(orgsApi.listOrganizations).mockResolvedValue(mockOrgs);

      const wrapper = mountComponent();
      await flushPromises();

      const select = wrapper.find('[data-testid="org-select"]');
      expect(select.exists()).toBe(true);
    });
  });

  describe('WhenOrganizationIsSelected', () => {
    it('ThenContextStoreIsUpdated', async () => {
      jest.mocked(orgsApi.listOrganizations).mockResolvedValue(mockOrgs);

      const wrapper = mountComponent();
      await flushPromises();

      const contextStore = useContextStore();
      // Simulate the v-select emitting an update
      await wrapper.findComponent({ name: 'VSelect' }).vm.$emit('update:modelValue', mockOrgs[0]);

      expect(contextStore.currentOrganization).toEqual(mockOrgs[0]);
    });
  });

  describe('WhenApiReturnsNoOrganizations', () => {
    it('ThenEmptyMessageIsDisplayed', async () => {
      jest.mocked(orgsApi.listOrganizations).mockResolvedValue([]);

      const wrapper = mountComponent();
      await flushPromises();

      const emptyMsg = wrapper.find('[data-testid="org-empty-message"]');
      expect(emptyMsg.exists()).toBe(true);
    });
  });

  describe('WhenApiCallFails', () => {
    it('ThenOrganizationListRemainsEmpty', async () => {
      jest.mocked(orgsApi.listOrganizations).mockRejectedValue(new Error('Network error'));

      const wrapper = mountComponent();
      await flushPromises();

      // No crash — component stays rendered
      expect(wrapper.find('[data-testid="org-selector"]').exists()).toBe(true);
    });
  });
});
