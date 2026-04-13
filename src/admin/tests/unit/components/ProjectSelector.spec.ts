import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { mount, flushPromises } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { createRouter, createMemoryHistory } from 'vue-router';
import ProjectSelector from '@/components/nav/ProjectSelector.vue';
import { useContextStore } from '@/stores/context';
import type { OrganizationResponse, ProjectResponse } from '@/types/api';

jest.mock('@/api/projects');
jest.mock('@/api/client', () => ({
  setAuthTokens: jest.fn(),
  clearAuthTokens: jest.fn(),
  getAccessToken: jest.fn(),
}));

import * as projectsApi from '@/api/projects';

const mockOrg: OrganizationResponse = { id: 1, name: 'Acme', createdAt: '2026-01-01T00:00:00Z' };
const mockProjects: ProjectResponse[] = [
  { id: 10, name: 'Website', organizationId: 1, hideDisabledFlags: false, createdAt: '2026-01-01T00:00:00Z' },
  { id: 11, name: 'Mobile App', organizationId: 1, hideDisabledFlags: false, createdAt: '2026-01-01T00:00:00Z' },
];

function mountComponent() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: '/', component: { template: '<div />' } }],
  });
  return mount(ProjectSelector, {
    global: { plugins: [router] },
  });
}

describe('ProjectSelector', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    jest.clearAllMocks();
  });

  describe('WhenNoOrganizationIsSelected', () => {
    it('ThenProjectsAreNotFetched', async () => {
      const wrapper = mountComponent();
      await flushPromises();

      expect(projectsApi.listProjects).not.toHaveBeenCalled();
      expect(wrapper.find('[data-testid="project-selector"]').exists()).toBe(true);
    });
  });

  describe('WhenOrganizationIsSetInContextStore', () => {
    it('ThenProjectsAreFetchedForThatOrganization', async () => {
      jest.mocked(projectsApi.listProjects).mockResolvedValue(mockProjects);

      const contextStore = useContextStore();
      mountComponent();

      contextStore.setOrganization(mockOrg);
      await flushPromises();

      expect(projectsApi.listProjects).toHaveBeenCalledWith(mockOrg.id);
    });
  });

  describe('WhenProjectIsSelected', () => {
    it('ThenContextStoreIsUpdated', async () => {
      jest.mocked(projectsApi.listProjects).mockResolvedValue(mockProjects);

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.findComponent({ name: 'VSelect' }).vm.$emit('update:modelValue', mockProjects[0]);

      expect(contextStore.currentProject).toEqual(mockProjects[0]);
    });
  });

  describe('WhenOrganizationChanges', () => {
    it('ThenProjectsAreReloaded', async () => {
      jest.mocked(projectsApi.listProjects).mockResolvedValue(mockProjects);

      const contextStore = useContextStore();
      mountComponent();

      contextStore.setOrganization(mockOrg);
      await flushPromises();

      const newOrg: OrganizationResponse = { id: 2, name: 'Globex', createdAt: '2026-01-01T00:00:00Z' };
      contextStore.setOrganization(newOrg);
      await flushPromises();

      expect(projectsApi.listProjects).toHaveBeenCalledTimes(2);
      expect(projectsApi.listProjects).toHaveBeenLastCalledWith(newOrg.id);
    });
  });
});
