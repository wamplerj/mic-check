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

const dialogStub = { template: '<div><slot /></div>' };

const mockOrg: OrganizationResponse = { id: 1, name: 'Acme', createdAt: '2026-01-01T00:00:00Z', isPrimary: false };
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
    global: {
      plugins: [router],
      stubs: { 'v-dialog': dialogStub },
    },
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

    it('ThenCreateProjectButtonIsDisabled', async () => {
      const wrapper = mountComponent();
      await flushPromises();

      const btn = wrapper.find('[data-testid="create-project-btn"]');
      expect(btn.exists()).toBe(true);
      expect(btn.attributes('disabled')).toBeDefined();
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

    it('ThenCreateProjectButtonIsEnabled', async () => {
      jest.mocked(projectsApi.listProjects).mockResolvedValue(mockProjects);

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);

      const wrapper = mountComponent();
      await flushPromises();

      const btn = wrapper.find('[data-testid="create-project-btn"]');
      expect(btn.attributes('disabled')).toBeUndefined();
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

  describe('WhenStoredProjectExistsInContextStore', () => {
    it('ThenStoredProjectIsRestoredAfterFetch', async () => {
      jest.mocked(projectsApi.listProjects).mockResolvedValue(mockProjects);

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);
      contextStore.setProject(mockProjects[1]);

      mountComponent();
      await flushPromises();

      expect(contextStore.currentProject).toEqual(mockProjects[1]);
    });

    it('ThenProjectIsClearedWhenNotFoundInNewOrg', async () => {
      jest.mocked(projectsApi.listProjects).mockResolvedValue(mockProjects);

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);
      contextStore.setProject(mockProjects[0]);

      mountComponent();
      await flushPromises();

      const newOrg: OrganizationResponse = { id: 2, name: 'Globex', createdAt: '2026-01-01T00:00:00Z', isPrimary: false };
      jest.mocked(projectsApi.listProjects).mockResolvedValue([]);
      contextStore.setOrganization(newOrg);
      await flushPromises();

      expect(contextStore.currentProject).toBeNull();
    });
  });

  describe('WhenSingleProjectIsAvailable', () => {
    it('ThenItIsAutoSelected', async () => {
      const singleProject = mockProjects[0];
      jest.mocked(projectsApi.listProjects).mockResolvedValue([singleProject]);

      const contextStore = useContextStore();
      mountComponent();

      contextStore.setOrganization(mockOrg);
      await flushPromises();

      expect(contextStore.currentProject).toEqual(singleProject);
    });
  });

  describe('WhenOrganizationChanges', () => {
    it('ThenProjectsAreReloaded', async () => {
      jest.mocked(projectsApi.listProjects).mockResolvedValue(mockProjects);

      const contextStore = useContextStore();
      mountComponent();

      contextStore.setOrganization(mockOrg);
      await flushPromises();

      const newOrg: OrganizationResponse = { id: 2, name: 'Globex', createdAt: '2026-01-01T00:00:00Z', isPrimary: false };
      contextStore.setOrganization(newOrg);
      await flushPromises();

      expect(projectsApi.listProjects).toHaveBeenCalledTimes(2);
      expect(projectsApi.listProjects).toHaveBeenLastCalledWith(newOrg.id);
    });
  });

  describe('WhenCreateProjectButtonIsClicked', () => {
    it('ThenCreateDialogIsShown', async () => {
      jest.mocked(projectsApi.listProjects).mockResolvedValue([]);

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-project-btn"]').trigger('click');

      expect(wrapper.find('[data-testid="create-project-dialog"]').exists()).toBe(true);
    });

    it('ThenNameInputIsPresentInDialog', async () => {
      jest.mocked(projectsApi.listProjects).mockResolvedValue([]);

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-project-btn"]').trigger('click');

      expect(wrapper.find('[data-testid="project-name-input"]').exists()).toBe(true);
    });
  });

  describe('WhenCreateProjectIsConfirmed', () => {
    const newProject: ProjectResponse = {
      id: 99, name: 'New Project', organizationId: 1, hideDisabledFlags: false, createdAt: '2026-04-13T00:00:00Z',
    };

    it('ThenCreateProjectApiIsCalled', async () => {
      jest.mocked(projectsApi.listProjects).mockResolvedValue([]);
      jest.mocked(projectsApi.createProject).mockResolvedValue(newProject);

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-project-btn"]').trigger('click');
      (wrapper.vm as any).newProjectName = 'New Project';
      await (wrapper.vm as any).onCreateConfirm();
      await flushPromises();

      expect(projectsApi.createProject).toHaveBeenCalledWith({ name: 'New Project', organizationId: mockOrg.id });
    });

    it('ThenNewProjectIsAutoSelectedInContextStore', async () => {
      jest.mocked(projectsApi.listProjects).mockResolvedValue([]);
      jest.mocked(projectsApi.createProject).mockResolvedValue(newProject);

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-project-btn"]').trigger('click');
      (wrapper.vm as any).newProjectName = 'New Project';
      await (wrapper.vm as any).onCreateConfirm();
      await flushPromises();

      expect(contextStore.currentProject).toEqual(newProject);
    });

    it('ThenDialogIsClosedAfterSuccessfulCreate', async () => {
      jest.mocked(projectsApi.listProjects).mockResolvedValue([]);
      jest.mocked(projectsApi.createProject).mockResolvedValue(newProject);

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-project-btn"]').trigger('click');
      (wrapper.vm as any).newProjectName = 'New Project';
      await (wrapper.vm as any).onCreateConfirm();
      await flushPromises();

      expect((wrapper.vm as any).showDialog).toBe(false);
    });
  });

  describe('WhenCreateProjectFails', () => {
    it('ThenErrorMessageIsDisplayed', async () => {
      jest.mocked(projectsApi.listProjects).mockResolvedValue([]);
      jest.mocked(projectsApi.createProject).mockRejectedValue(new Error('Server error'));

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-project-btn"]').trigger('click');
      (wrapper.vm as any).newProjectName = 'Bad Project';
      await (wrapper.vm as any).onCreateConfirm();
      await flushPromises();

      expect((wrapper.vm as any).createError).toBeTruthy();
    });

    it('ThenDialogRemainsOpen', async () => {
      jest.mocked(projectsApi.listProjects).mockResolvedValue([]);
      jest.mocked(projectsApi.createProject).mockRejectedValue(new Error('Server error'));

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-project-btn"]').trigger('click');
      (wrapper.vm as any).newProjectName = 'Bad Project';
      await (wrapper.vm as any).onCreateConfirm();
      await flushPromises();

      expect((wrapper.vm as any).showDialog).toBe(true);
    });
  });

  describe('WhenCancelIsClicked', () => {
    it('ThenDialogIsClosed', async () => {
      jest.mocked(projectsApi.listProjects).mockResolvedValue([]);

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-project-btn"]').trigger('click');
      await wrapper.find('[data-testid="create-project-cancel-btn"]').trigger('click');

      expect((wrapper.vm as any).showDialog).toBe(false);
    });
  });
});
