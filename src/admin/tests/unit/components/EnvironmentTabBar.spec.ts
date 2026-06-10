import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { mount, flushPromises } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { createRouter, createMemoryHistory } from 'vue-router';
import EnvironmentTabBar from '@/components/nav/EnvironmentTabBar.vue';
import { useContextStore } from '@/stores/context';
import type { ProjectResponse, EnvironmentResponse } from '@/types/api';

jest.mock('@/api/environments');
jest.mock('@/api/client', () => ({
  setAuthTokens: jest.fn(),
  clearAuthTokens: jest.fn(),
  getAccessToken: jest.fn(),
}));

import * as envApi from '@/api/environments';

const dialogStub = { template: '<div><slot /></div>' };

const mockProject: ProjectResponse = {
  id: 10,
  name: 'Website',
  organizationId: 1,
  hideDisabledFlags: false,
  createdAt: '2026-01-01T00:00:00Z',
};
const mockEnvironments: EnvironmentResponse[] = [
  { id: 100, name: 'Development', apiKey: 'env-key-dev', projectId: 10, createdAt: '2026-01-01T00:00:00Z' },
  { id: 101, name: 'Production', apiKey: 'env-key-prod', projectId: 10, createdAt: '2026-01-01T00:00:00Z' },
];

function mountComponent() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: '/', component: { template: '<div />' } }],
  });
  return mount(EnvironmentTabBar, {
    global: {
      plugins: [router],
      stubs: { 'v-dialog': dialogStub },
    },
  });
}

describe('EnvironmentTabBar', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    jest.clearAllMocks();
  });

  describe('WhenNoProjectIsSelected', () => {
    it('ThenEnvironmentsAreNotFetched', async () => {
      mountComponent();
      await flushPromises();

      expect(envApi.listEnvironments).not.toHaveBeenCalled();
    });

    it('ThenNoProjectMessageIsShown', async () => {
      const wrapper = mountComponent();
      await flushPromises();

      expect(wrapper.find('[data-testid="env-no-project-message"]').exists()).toBe(true);
    });

    it('ThenCreateEnvironmentButtonIsDisabled', async () => {
      const wrapper = mountComponent();
      await flushPromises();

      expect(wrapper.find('[data-testid="create-env-btn"]').attributes('disabled')).toBeDefined();
    });
  });

  describe('WhenProjectIsSetInContextStore', () => {
    it('ThenEnvironmentsAreFetchedForThatProject', async () => {
      jest.mocked(envApi.listEnvironments).mockResolvedValue(mockEnvironments);

      const contextStore = useContextStore();
      mountComponent();

      contextStore.setProject(mockProject);
      await flushPromises();

      expect(envApi.listEnvironments).toHaveBeenCalledWith(mockProject.id);
    });

    it('ThenEnvironmentChipsAreRendered', async () => {
      jest.mocked(envApi.listEnvironments).mockResolvedValue(mockEnvironments);

      const contextStore = useContextStore();
      const wrapper = mountComponent();

      contextStore.setProject(mockProject);
      await flushPromises();

      expect(wrapper.find('[data-testid="env-chip-development"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="env-chip-production"]').exists()).toBe(true);
    });

    it('ThenFirstEnvironmentIsAutoSelected', async () => {
      jest.mocked(envApi.listEnvironments).mockResolvedValue(mockEnvironments);

      const contextStore = useContextStore();
      mountComponent();

      contextStore.setProject(mockProject);
      await flushPromises();

      expect(contextStore.currentEnvironment).toEqual(mockEnvironments[0]);
    });

    it('ThenCreateEnvironmentButtonIsEnabled', async () => {
      jest.mocked(envApi.listEnvironments).mockResolvedValue(mockEnvironments);

      const contextStore = useContextStore();
      contextStore.setProject(mockProject);

      const wrapper = mountComponent();
      await flushPromises();

      expect(wrapper.find('[data-testid="create-env-btn"]').attributes('disabled')).toBeUndefined();
    });
  });

  describe('WhenEnvironmentChipIsClicked', () => {
    it('ThenContextStoreIsUpdatedWithSelectedEnvironment', async () => {
      jest.mocked(envApi.listEnvironments).mockResolvedValue(mockEnvironments);

      const contextStore = useContextStore();
      const wrapper = mountComponent();

      contextStore.setProject(mockProject);
      await flushPromises();

      await wrapper.findComponent({ name: 'VChipGroup' }).vm.$emit('update:modelValue', 1);

      expect(contextStore.currentEnvironment).toEqual(mockEnvironments[1]);
    });
  });

  describe('WhenProjectChanges', () => {
    it('ThenEnvironmentsAreReloaded', async () => {
      jest.mocked(envApi.listEnvironments).mockResolvedValue(mockEnvironments);

      const contextStore = useContextStore();
      mountComponent();

      contextStore.setProject(mockProject);
      await flushPromises();

      const newProject: ProjectResponse = { ...mockProject, id: 20, name: 'Mobile' };
      contextStore.setProject(newProject);
      await flushPromises();

      expect(envApi.listEnvironments).toHaveBeenCalledTimes(2);
      expect(envApi.listEnvironments).toHaveBeenLastCalledWith(newProject.id);
    });
  });

  describe('WhenApiReturnsNoEnvironments', () => {
    it('ThenEmptyMessageIsDisplayed', async () => {
      jest.mocked(envApi.listEnvironments).mockResolvedValue([]);

      const contextStore = useContextStore();
      const wrapper = mountComponent();

      contextStore.setProject(mockProject);
      await flushPromises();

      expect(wrapper.find('[data-testid="env-empty-message"]').exists()).toBe(true);
    });
  });

  describe('WhenCreateEnvironmentButtonIsClicked', () => {
    it('ThenCreateDialogIsShown', async () => {
      jest.mocked(envApi.listEnvironments).mockResolvedValue([]);

      const contextStore = useContextStore();
      contextStore.setProject(mockProject);

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-env-btn"]').trigger('click');

      expect(wrapper.find('[data-testid="create-env-dialog"]').exists()).toBe(true);
    });

    it('ThenNameInputIsPresentInDialog', async () => {
      jest.mocked(envApi.listEnvironments).mockResolvedValue([]);

      const contextStore = useContextStore();
      contextStore.setProject(mockProject);

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-env-btn"]').trigger('click');

      expect(wrapper.find('[data-testid="env-name-input"]').exists()).toBe(true);
    });
  });

  describe('WhenCreateEnvironmentIsConfirmed', () => {
    const newEnv: EnvironmentResponse = {
      id: 200, name: 'Staging', apiKey: 'env-key-staging', projectId: 10, createdAt: '2026-04-14T00:00:00Z',
    };

    it('ThenCreateEnvironmentApiIsCalled', async () => {
      jest.mocked(envApi.listEnvironments).mockResolvedValue([]);
      jest.mocked(envApi.createEnvironment).mockResolvedValue(newEnv);

      const contextStore = useContextStore();
      contextStore.setProject(mockProject);

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-env-btn"]').trigger('click');
      (wrapper.vm as any).newEnvName = 'Staging';
      await (wrapper.vm as any).onCreateConfirm();
      await flushPromises();

      expect(envApi.createEnvironment).toHaveBeenCalledWith({ name: 'Staging', projectId: mockProject.id });
    });

    it('ThenNewEnvironmentIsAutoSelectedInContextStore', async () => {
      jest.mocked(envApi.listEnvironments).mockResolvedValue([]);
      jest.mocked(envApi.createEnvironment).mockResolvedValue(newEnv);

      const contextStore = useContextStore();
      contextStore.setProject(mockProject);

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-env-btn"]').trigger('click');
      (wrapper.vm as any).newEnvName = 'Staging';
      await (wrapper.vm as any).onCreateConfirm();
      await flushPromises();

      expect(contextStore.currentEnvironment).toEqual(newEnv);
    });

    it('ThenDialogIsClosedAfterSuccessfulCreate', async () => {
      jest.mocked(envApi.listEnvironments).mockResolvedValue([]);
      jest.mocked(envApi.createEnvironment).mockResolvedValue(newEnv);

      const contextStore = useContextStore();
      contextStore.setProject(mockProject);

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-env-btn"]').trigger('click');
      (wrapper.vm as any).newEnvName = 'Staging';
      await (wrapper.vm as any).onCreateConfirm();
      await flushPromises();

      expect((wrapper.vm as any).showDialog).toBe(false);
    });
  });

  describe('WhenCreateEnvironmentFails', () => {
    it('ThenErrorMessageIsDisplayed', async () => {
      jest.mocked(envApi.listEnvironments).mockResolvedValue([]);
      jest.mocked(envApi.createEnvironment).mockRejectedValue(new Error('Server error'));

      const contextStore = useContextStore();
      contextStore.setProject(mockProject);

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-env-btn"]').trigger('click');
      (wrapper.vm as any).newEnvName = 'Bad Env';
      await (wrapper.vm as any).onCreateConfirm();
      await flushPromises();

      expect((wrapper.vm as any).createError).toBeTruthy();
    });

    it('ThenDialogRemainsOpen', async () => {
      jest.mocked(envApi.listEnvironments).mockResolvedValue([]);
      jest.mocked(envApi.createEnvironment).mockRejectedValue(new Error('Server error'));

      const contextStore = useContextStore();
      contextStore.setProject(mockProject);

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-env-btn"]').trigger('click');
      (wrapper.vm as any).newEnvName = 'Bad Env';
      await (wrapper.vm as any).onCreateConfirm();
      await flushPromises();

      expect((wrapper.vm as any).showDialog).toBe(true);
    });
  });

  describe('WhenCancelIsClicked', () => {
    it('ThenDialogIsClosed', async () => {
      jest.mocked(envApi.listEnvironments).mockResolvedValue([]);

      const contextStore = useContextStore();
      contextStore.setProject(mockProject);

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-env-btn"]').trigger('click');
      await wrapper.find('[data-testid="create-env-cancel-btn"]').trigger('click');

      expect((wrapper.vm as any).showDialog).toBe(false);
    });
  });
});
