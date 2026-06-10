import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { mount, flushPromises, type VueWrapper } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { createRouter, createMemoryHistory } from 'vue-router';
import AuditLogsView from '@/views/AuditLogsView.vue';
import { useContextStore } from '@/stores/context';
import type { ProjectResponse, AuditLogResponse } from '@/types/api';

jest.mock('@/api/auditLogs');
jest.mock('@/api/client', () => ({
  setAuthTokens: jest.fn(),
  clearAuthTokens: jest.fn(),
  getAccessToken: jest.fn(),
}));

import * as auditLogsApi from '@/api/auditLogs';

const mockProject: ProjectResponse = {
  id: 10, name: 'Website', organizationId: 1, hideDisabledFlags: false, createdAt: '2026-01-01T00:00:00Z',
};

const mockLogs: AuditLogResponse[] = [
  {
    id: 1, resourceType: 'Feature', resourceId: '42', action: 'Created',
    changes: null, organizationId: 1, projectId: 10, environmentId: null,
    actorUserId: 5, createdAt: '2026-01-10T10:00:00Z',
  },
  {
    id: 2, resourceType: 'FeatureState', resourceId: '99', action: 'Updated',
    changes: '{"enabled":true}', organizationId: 1, projectId: 10, environmentId: 100,
    actorUserId: 5, createdAt: '2026-01-11T11:00:00Z',
  },
];

function mountView() {
  const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/', component: { template: '<div />' } }] });
  return mount(AuditLogsView, { global: { plugins: [router] } });
}

describe('AuditLogsView', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    jest.clearAllMocks();
  });

  describe('WhenNoProjectIsSelected', () => {
    it('ThenEmptyStateIsShown', () => {
      const wrapper = mountView();
      expect(wrapper.find('[data-testid="audit-logs-table"]').exists()).toBe(false);
      expect(wrapper.text()).toContain('Select a project');
    });
  });

  describe('WhenProjectIsSelected', () => {
    it('ThenAuditLogsAreLoaded', async () => {
      jest.mocked(auditLogsApi.listAuditLogsByProject).mockResolvedValue({
        count: 2, next: null, previous: null, results: mockLogs,
      });

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '' });
      contextStore.setProject(mockProject);

      mountView();
      await flushPromises();

      expect(auditLogsApi.listAuditLogsByProject).toHaveBeenCalledWith(
        mockProject.id,
        expect.objectContaining({ page: 1 }),
      );
    });

    it('ThenAuditLogsTableIsRendered', async () => {
      jest.mocked(auditLogsApi.listAuditLogsByProject).mockResolvedValue({
        count: 2, next: null, previous: null, results: mockLogs,
      });

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '' });
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      expect(wrapper.find('[data-testid="audit-logs-table"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Feature');
      expect(wrapper.text()).toContain('Created');
    });
  });

  describe('WhenResourceTypeFilterIsApplied', () => {
    it('ThenLogsAreReloadedWithFilter', async () => {
      jest.mocked(auditLogsApi.listAuditLogsByProject).mockResolvedValue({
        count: 1, next: null, previous: null, results: [mockLogs[0]],
      });

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '' });
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      jest.mocked(auditLogsApi.listAuditLogsByProject).mockClear();

      await (wrapper.findComponent('[data-testid="filter-resource-type"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'Feature');
      await flushPromises();

      expect(auditLogsApi.listAuditLogsByProject).toHaveBeenCalledWith(
        mockProject.id,
        expect.objectContaining({ resourceType: 'Feature', page: 1 }),
      );
    });
  });

  describe('WhenActionFilterIsApplied', () => {
    it('ThenLogsAreReloadedWithActionFilter', async () => {
      jest.mocked(auditLogsApi.listAuditLogsByProject).mockResolvedValue({
        count: 1, next: null, previous: null, results: [mockLogs[0]],
      });

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '' });
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      jest.mocked(auditLogsApi.listAuditLogsByProject).mockClear();

      await (wrapper.findComponent('[data-testid="filter-action"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'Created');
      await flushPromises();

      expect(auditLogsApi.listAuditLogsByProject).toHaveBeenCalledWith(
        mockProject.id,
        expect.objectContaining({ action: 'Created', page: 1 }),
      );
    });
  });

  describe('WhenDateRangeFilterIsApplied', () => {
    it('ThenLogsAreReloadedWithDateRange', async () => {
      jest.mocked(auditLogsApi.listAuditLogsByProject).mockResolvedValue({
        count: 1, next: null, previous: null, results: [mockLogs[0]],
      });

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '' });
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      jest.mocked(auditLogsApi.listAuditLogsByProject).mockClear();

      await (wrapper.findComponent('[data-testid="filter-from"]') as VueWrapper<any>).vm.$emit('update:modelValue', '2026-01-01');
      await flushPromises();

      expect(auditLogsApi.listAuditLogsByProject).toHaveBeenCalledWith(
        mockProject.id,
        expect.objectContaining({ from: '2026-01-01T00:00:00Z' }),
      );
    });
  });

  describe('WhenClearFiltersIsClicked', () => {
    it('ThenFiltersAreResetAndLogsReloaded', async () => {
      jest.mocked(auditLogsApi.listAuditLogsByProject).mockResolvedValue({
        count: 2, next: null, previous: null, results: mockLogs,
      });

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '' });
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      jest.mocked(auditLogsApi.listAuditLogsByProject).mockClear();

      await wrapper.find('[data-testid="clear-filters-btn"]').trigger('click');
      await flushPromises();

      expect(auditLogsApi.listAuditLogsByProject).toHaveBeenCalledWith(
        mockProject.id,
        expect.objectContaining({ resourceType: null, action: null, from: null, to: null }),
      );
    });
  });

  describe('WhenChangesExpandButtonIsClicked', () => {
    it('ThenChangesContentIsDisplayed', async () => {
      jest.mocked(auditLogsApi.listAuditLogsByProject).mockResolvedValue({
        count: 2, next: null, previous: null, results: mockLogs,
      });

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '' });
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      // log id=2 has changes
      await wrapper.find('[data-testid="expand-changes-2"]').trigger('click');
      await wrapper.vm.$nextTick();

      expect(wrapper.find('[data-testid="changes-2"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="changes-2"]').text()).toContain('enabled');
    });
  });
});
