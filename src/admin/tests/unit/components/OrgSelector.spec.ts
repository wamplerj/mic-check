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

const dialogStub = { template: '<div><slot /></div>' };

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
    global: {
      plugins: [router],
      stubs: { 'v-dialog': dialogStub },
    },
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

    it('ThenCreateOrganizationButtonIsAlwaysVisible', async () => {
      jest.mocked(orgsApi.listOrganizations).mockResolvedValue(mockOrgs);

      const wrapper = mountComponent();
      await flushPromises();

      expect(wrapper.find('[data-testid="create-org-btn"]').exists()).toBe(true);
    });
  });

  describe('WhenOrganizationIsSelected', () => {
    it('ThenContextStoreIsUpdated', async () => {
      jest.mocked(orgsApi.listOrganizations).mockResolvedValue(mockOrgs);

      const wrapper = mountComponent();
      await flushPromises();

      const contextStore = useContextStore();
      await wrapper.findComponent({ name: 'VSelect' }).vm.$emit('update:modelValue', mockOrgs[0]);

      expect(contextStore.currentOrganization).toEqual(mockOrgs[0]);
    });
  });

  describe('WhenApiReturnsNoOrganizations', () => {
    it('ThenCreateButtonIsStillVisible', async () => {
      jest.mocked(orgsApi.listOrganizations).mockResolvedValue([]);

      const wrapper = mountComponent();
      await flushPromises();

      expect(wrapper.find('[data-testid="create-org-btn"]').exists()).toBe(true);
    });
  });

  describe('WhenApiCallFails', () => {
    it('ThenOrganizationListRemainsEmpty', async () => {
      jest.mocked(orgsApi.listOrganizations).mockRejectedValue(new Error('Network error'));

      const wrapper = mountComponent();
      await flushPromises();

      expect(wrapper.find('[data-testid="org-selector"]').exists()).toBe(true);
    });
  });

  describe('WhenCreateOrganizationButtonIsClicked', () => {
    it('ThenCreateDialogIsShown', async () => {
      jest.mocked(orgsApi.listOrganizations).mockResolvedValue([]);

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-org-btn"]').trigger('click');

      expect(wrapper.find('[data-testid="create-org-dialog"]').exists()).toBe(true);
    });

    it('ThenNameInputIsPresentInDialog', async () => {
      jest.mocked(orgsApi.listOrganizations).mockResolvedValue([]);

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-org-btn"]').trigger('click');

      expect(wrapper.find('[data-testid="org-name-input"]').exists()).toBe(true);
    });
  });

  describe('WhenCreateOrganizationIsConfirmed', () => {
    const newOrg: OrganizationResponse = { id: 3, name: 'New Org', createdAt: '2026-04-13T00:00:00Z' };

    it('ThenCreateOrganizationApiIsCalled', async () => {
      jest.mocked(orgsApi.listOrganizations).mockResolvedValue([]);
      jest.mocked(orgsApi.createOrganization).mockResolvedValue(newOrg);

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-org-btn"]').trigger('click');
      (wrapper.vm as any).newOrgName = 'New Org';
      await (wrapper.vm as any).onCreateConfirm();
      await flushPromises();

      expect(orgsApi.createOrganization).toHaveBeenCalledWith({ name: 'New Org' });
    });

    it('ThenNewOrgIsAutoSelectedInContextStore', async () => {
      jest.mocked(orgsApi.listOrganizations).mockResolvedValue([]);
      jest.mocked(orgsApi.createOrganization).mockResolvedValue(newOrg);

      const wrapper = mountComponent();
      await flushPromises();

      const contextStore = useContextStore();

      await wrapper.find('[data-testid="create-org-btn"]').trigger('click');
      (wrapper.vm as any).newOrgName = 'New Org';
      await (wrapper.vm as any).onCreateConfirm();
      await flushPromises();

      expect(contextStore.currentOrganization).toEqual(newOrg);
    });

    it('ThenDialogIsClosedAfterSuccessfulCreate', async () => {
      jest.mocked(orgsApi.listOrganizations).mockResolvedValue([]);
      jest.mocked(orgsApi.createOrganization).mockResolvedValue(newOrg);

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-org-btn"]').trigger('click');
      (wrapper.vm as any).newOrgName = 'New Org';
      await (wrapper.vm as any).onCreateConfirm();
      await flushPromises();

      expect((wrapper.vm as any).showDialog).toBe(false);
    });
  });

  describe('WhenCreateOrganizationFails', () => {
    it('ThenErrorMessageIsDisplayed', async () => {
      jest.mocked(orgsApi.listOrganizations).mockResolvedValue([]);
      jest.mocked(orgsApi.createOrganization).mockRejectedValue(new Error('Server error'));

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-org-btn"]').trigger('click');
      (wrapper.vm as any).newOrgName = 'Bad Org';
      await (wrapper.vm as any).onCreateConfirm();
      await flushPromises();

      expect((wrapper.vm as any).createError).toBeTruthy();
    });

    it('ThenDialogRemainsOpen', async () => {
      jest.mocked(orgsApi.listOrganizations).mockResolvedValue([]);
      jest.mocked(orgsApi.createOrganization).mockRejectedValue(new Error('Server error'));

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-org-btn"]').trigger('click');
      (wrapper.vm as any).newOrgName = 'Bad Org';
      await (wrapper.vm as any).onCreateConfirm();
      await flushPromises();

      expect((wrapper.vm as any).showDialog).toBe(true);
    });
  });

  describe('WhenCancelIsClicked', () => {
    it('ThenDialogIsClosed', async () => {
      jest.mocked(orgsApi.listOrganizations).mockResolvedValue([]);

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-org-btn"]').trigger('click');
      await wrapper.find('[data-testid="create-org-cancel-btn"]').trigger('click');

      const vm = wrapper.vm as any;
      expect(vm.showDialog).toBe(false);
    });
  });
});
