import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { mount, flushPromises, type VueWrapper } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { createRouter, createMemoryHistory } from 'vue-router';
import SettingsView from '@/views/SettingsView.vue';
import { useContextStore } from '@/stores/context';
import type {
  ProjectResponse, OrganizationResponse, EnvironmentResponse,
  UserPermissionResponse, ApiKeyResponse, CreateApiKeyResponse,
} from '@/types/api';

jest.mock('@/api/organizations');
jest.mock('@/api/projects');
jest.mock('@/api/environments');
jest.mock('@/api/apiKeys');
jest.mock('@/api/webhooks');
jest.mock('@/api/client', () => ({
  setAuthTokens: jest.fn(),
  clearAuthTokens: jest.fn(),
  getAccessToken: jest.fn(),
}));

import * as orgsApi from '@/api/organizations';
import * as projectsApi from '@/api/projects';
import * as environmentsApi from '@/api/environments';
import * as apiKeysApi from '@/api/apiKeys';
import * as webhooksApi from '@/api/webhooks';

const mockOrg: OrganizationResponse = { id: 1, name: 'Acme', createdAt: '2026-01-01T00:00:00Z' };
const mockProject: ProjectResponse = { id: 10, name: 'Website', organizationId: 1, hideDisabledFlags: false, createdAt: '2026-01-01T00:00:00Z' };
const mockEnvs: EnvironmentResponse[] = [
  { id: 100, name: 'Development', apiKey: 'env-dev', projectId: 10, createdAt: '2026-01-01T00:00:00Z' },
  { id: 200, name: 'Production', apiKey: 'env-prod', projectId: 10, createdAt: '2026-01-02T00:00:00Z' },
];
const mockApiKeys: ApiKeyResponse[] = [
  { id: 1, name: 'CI Key', prefix: 'org-ci', isActive: true, expiresAt: null, createdAt: '2026-01-01T00:00:00Z' },
];
const mockPermissions: UserPermissionResponse[] = [
  { userId: 5, projectId: 10, isAdmin: false, permissions: ['ViewProject', 'EditFeature'] },
];

const dialogStub = { template: '<div><slot /></div>' };

function mountView() {
  const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/', component: { template: '<div />' } }] });
  return mount(SettingsView, {
    global: { plugins: [router], stubs: { 'v-dialog': dialogStub } },
  });
}

describe('SettingsView', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    jest.clearAllMocks();

    jest.mocked(orgsApi.listOrganizationMembers).mockResolvedValue([]);
    jest.mocked(projectsApi.listProjectUserPermissions).mockResolvedValue([]);
    jest.mocked(environmentsApi.listEnvironments).mockResolvedValue([]);
    jest.mocked(apiKeysApi.listApiKeys).mockResolvedValue([]);
    jest.mocked(webhooksApi.listOrgWebhooks).mockResolvedValue([]);
    jest.mocked(webhooksApi.listEnvWebhooks).mockResolvedValue([]);
  });

  describe('WhenNoOrganizationSelected', () => {
    it('ThenSelectOrgMessageIsShown', () => {
      const wrapper = mountView();
      expect(wrapper.text()).toContain('Select an organization');
    });
  });

  describe('WhenOrganizationIsSelected', () => {
    it('ThenTabsAreRendered', async () => {
      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);

      const wrapper = mountView();
      await flushPromises();

      expect(wrapper.find('[data-testid="tab-organization"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="tab-api-keys"]').exists()).toBe(true);
    });
  });

  // ── Organization tab ────────────────────────────────────────────────────────

  describe('WhenSaveOrgNameIsClicked', () => {
    it('ThenUpdateOrganizationIsCalledWithNewName', async () => {
      jest.mocked(orgsApi.updateOrganization).mockResolvedValue({ ...mockOrg, name: 'New Name' });

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);

      const wrapper = mountView();
      await flushPromises();

      await (wrapper.findComponent('[data-testid="org-name-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'New Name');
      await wrapper.find('[data-testid="save-org-name-btn"]').trigger('click');
      await flushPromises();

      expect(orgsApi.updateOrganization).toHaveBeenCalledWith(mockOrg.id, { name: 'New Name' });
    });
  });

  describe('WhenInviteMemberIsClicked', () => {
    it('ThenInviteOrganizationMemberIsCalledWithUserId', async () => {
      jest.mocked(orgsApi.inviteOrganizationMember).mockResolvedValue(undefined);
      jest.mocked(orgsApi.listOrganizationMembers).mockResolvedValue([{ userId: 42, firstName: 'Jane', lastName: 'Doe', email: 'jane@example.com', role: 'User', lastLoginAt: null }]);

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);

      const wrapper = mountView();
      await flushPromises();

      await (wrapper.findComponent('[data-testid="invite-user-id-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', '42');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="invite-member-btn"]').trigger('click');
      await flushPromises();

      expect(orgsApi.inviteOrganizationMember).toHaveBeenCalledWith(mockOrg.id, { userId: 42, role: 'User' });
    });
  });

  describe('WhenRemoveMemberIsClicked', () => {
    it('ThenRemoveOrganizationMemberIsCalledAndMemberIsRemovedFromList', async () => {
      jest.mocked(orgsApi.listOrganizationMembers).mockResolvedValue([{ userId: 5, firstName: 'John', lastName: 'Smith', email: 'john@example.com', role: 'User', lastLoginAt: null }]);
      jest.mocked(orgsApi.removeOrganizationMember).mockResolvedValue(undefined);

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);

      const wrapper = mountView();
      await flushPromises();

      expect(wrapper.find('[data-testid="member-row-5"]').exists()).toBe(true);

      await wrapper.find('[data-testid="remove-member-5"]').trigger('click');
      await flushPromises();

      expect(orgsApi.removeOrganizationMember).toHaveBeenCalledWith(mockOrg.id, 5);
      expect(wrapper.find('[data-testid="member-row-5"]').exists()).toBe(false);
    });
  });

  // ── Project tab ─────────────────────────────────────────────────────────────

  describe('WhenSaveProjectIsClicked', () => {
    it('ThenUpdateProjectIsCalledWithFormValues', async () => {
      jest.mocked(projectsApi.updateProject).mockResolvedValue(mockProject);

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      await wrapper.find('[data-testid="tab-project"]').trigger('click');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="save-project-btn"]').trigger('click');
      await flushPromises();

      expect(projectsApi.updateProject).toHaveBeenCalledWith(
        mockProject.id,
        expect.objectContaining({ name: mockProject.name }),
      );
    });
  });

  describe('WhenPermissionsAreLoaded', () => {
    it('ThenPermissionRowsAreDisplayed', async () => {
      jest.mocked(projectsApi.listProjectUserPermissions).mockResolvedValue(mockPermissions);

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      await wrapper.find('[data-testid="tab-project"]').trigger('click');
      await wrapper.vm.$nextTick();

      expect(wrapper.find('[data-testid="permission-row-5"]').exists()).toBe(true);
    });
  });

  describe('WhenAdminToggleIsChanged', () => {
    it('ThenUpdateProjectUserPermissionsIsCalledWithIsAdmin', async () => {
      jest.mocked(projectsApi.listProjectUserPermissions).mockResolvedValue(mockPermissions);
      jest.mocked(projectsApi.updateProjectUserPermissions).mockResolvedValue({
        ...mockPermissions[0], isAdmin: true,
      });

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      await wrapper.find('[data-testid="tab-project"]').trigger('click');
      await wrapper.vm.$nextTick();

      await (wrapper.findComponent(`[data-testid="perm-admin-toggle-5"]`) as VueWrapper<any>).vm.$emit('update:modelValue', true);
      await flushPromises();

      expect(projectsApi.updateProjectUserPermissions).toHaveBeenCalledWith(
        mockProject.id, 5,
        expect.objectContaining({ isAdmin: true }),
      );
    });
  });

  describe('WhenAddUserPermissionsIsSubmitted', () => {
    it('ThenSetProjectUserPermissionsIsCalledWithNewUserId', async () => {
      const newPerm: UserPermissionResponse = { userId: 99, projectId: 10, isAdmin: false, permissions: ['ViewProject'] };
      jest.mocked(projectsApi.setProjectUserPermissions).mockResolvedValue(newPerm);

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      await wrapper.find('[data-testid="tab-project"]').trigger('click');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="add-permission-btn"]').trigger('click');
      await wrapper.vm.$nextTick();

      await (wrapper.findComponent('[data-testid="new-perm-user-id-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', '99');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="save-new-permission-btn"]').trigger('click');
      await flushPromises();

      expect(projectsApi.setProjectUserPermissions).toHaveBeenCalledWith(
        mockProject.id,
        expect.objectContaining({ userId: 99 }),
      );
    });
  });

  describe('WhenRemovePermissionIsClicked', () => {
    it('ThenRemoveProjectUserPermissionsIsCalledAndRowIsRemoved', async () => {
      jest.mocked(projectsApi.listProjectUserPermissions).mockResolvedValue(mockPermissions);
      jest.mocked(projectsApi.removeProjectUserPermissions).mockResolvedValue(undefined);

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      await wrapper.find('[data-testid="tab-project"]').trigger('click');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="remove-permission-5"]').trigger('click');
      await flushPromises();

      expect(projectsApi.removeProjectUserPermissions).toHaveBeenCalledWith(mockProject.id, 5);
      expect(wrapper.find('[data-testid="permission-row-5"]').exists()).toBe(false);
    });
  });

  // ── Environments tab ────────────────────────────────────────────────────────

  describe('WhenCreateEnvironmentIsClicked', () => {
    it('ThenCreateEnvironmentIsCalledAndEnvIsAddedToList', async () => {
      jest.mocked(environmentsApi.listEnvironments).mockResolvedValue([]);
      jest.mocked(environmentsApi.createEnvironment).mockResolvedValue(mockEnvs[0]);

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      await wrapper.find('[data-testid="tab-environments"]').trigger('click');
      await wrapper.vm.$nextTick();

      await (wrapper.findComponent('[data-testid="new-env-name-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'Staging');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="create-env-btn"]').trigger('click');
      await flushPromises();

      expect(environmentsApi.createEnvironment).toHaveBeenCalledWith({ projectId: mockProject.id, name: 'Staging' });
    });
  });

  describe('WhenCloneEnvironmentIsConfirmed', () => {
    it('ThenCloneEnvironmentIsCalledWithNewName', async () => {
      jest.mocked(environmentsApi.listEnvironments).mockResolvedValue([...mockEnvs]);
      jest.mocked(environmentsApi.cloneEnvironment).mockResolvedValue({
        ...mockEnvs[0], id: 300, name: 'Staging', apiKey: 'env-stg',
      });

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      await wrapper.find('[data-testid="tab-environments"]').trigger('click');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="clone-env-100"]').trigger('click');
      await wrapper.vm.$nextTick();

      await (wrapper.findComponent('[data-testid="clone-env-name-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'Staging');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="confirm-clone-btn"]').trigger('click');
      await flushPromises();

      expect(environmentsApi.cloneEnvironment).toHaveBeenCalledWith(mockEnvs[0].apiKey, { name: 'Staging' });
    });
  });

  describe('WhenDeleteEnvironmentIsConfirmed', () => {
    it('ThenDeleteEnvironmentIsCalledAndEnvIsRemovedFromList', async () => {
      jest.mocked(environmentsApi.listEnvironments).mockResolvedValue([...mockEnvs]);
      jest.mocked(environmentsApi.deleteEnvironment).mockResolvedValue(undefined);

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      await wrapper.find('[data-testid="tab-environments"]').trigger('click');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="delete-env-100"]').trigger('click');
      await wrapper.vm.$nextTick();

      await (wrapper.findComponent('[data-testid="delete-env-confirm-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'Development');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="confirm-delete-env-btn"]').trigger('click');
      await flushPromises();

      expect(environmentsApi.deleteEnvironment).toHaveBeenCalledWith(mockEnvs[0].apiKey);
    });
  });

  describe('WhenRenameEnvironmentIsConfirmed', () => {
    it('ThenUpdateEnvironmentIsCalledWithNewName', async () => {
      jest.mocked(environmentsApi.listEnvironments).mockResolvedValue([...mockEnvs]);
      jest.mocked(environmentsApi.updateEnvironment).mockResolvedValue({ ...mockEnvs[0], name: 'Dev Renamed' });

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      await wrapper.find('[data-testid="tab-environments"]').trigger('click');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="rename-env-100"]').trigger('click');
      await wrapper.vm.$nextTick();

      await (wrapper.findComponent('[data-testid="rename-env-name-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'Dev Renamed');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="confirm-rename-btn"]').trigger('click');
      await flushPromises();

      expect(environmentsApi.updateEnvironment).toHaveBeenCalledWith(mockEnvs[0].apiKey, { name: 'Dev Renamed' });
    });
  });

  // ── API Keys tab ────────────────────────────────────────────────────────────

  describe('WhenApiKeysTabIsOpened', () => {
    it('ThenApiKeysAreLoaded', async () => {
      jest.mocked(apiKeysApi.listApiKeys).mockResolvedValue(mockApiKeys);

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);

      const wrapper = mountView();
      await flushPromises();

      await wrapper.find('[data-testid="tab-api-keys"]').trigger('click');
      await wrapper.vm.$nextTick();

      expect(wrapper.find('[data-testid="api-key-row-1"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('CI Key');
    });
  });

  describe('WhenCreateApiKeyIsSubmitted', () => {
    it('ThenCreateApiKeyIsCalledAndRawKeyDialogIsShown', async () => {
      const created: CreateApiKeyResponse = { id: 99, name: 'My Key', rawKey: 'raw-secret-key', prefix: 'org-my', expiresAt: null };
      jest.mocked(apiKeysApi.createApiKey).mockResolvedValue(created);

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);

      const wrapper = mountView();
      await flushPromises();

      await wrapper.find('[data-testid="tab-api-keys"]').trigger('click');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="create-api-key-btn"]').trigger('click');
      await wrapper.vm.$nextTick();

      await (wrapper.findComponent('[data-testid="api-key-name-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'My Key');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="save-api-key-btn"]').trigger('click');
      await flushPromises();

      expect(apiKeysApi.createApiKey).toHaveBeenCalledWith(mockOrg.id, expect.objectContaining({ name: 'My Key' }));
      expect(wrapper.find('[data-testid="raw-key-dialog"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="raw-key-field"]').exists()).toBe(true);
    });

    it('ThenRawKeyIsDisplayedInTheRevealDialog', async () => {
      const created: CreateApiKeyResponse = { id: 99, name: 'My Key', rawKey: 'raw-secret-key', prefix: 'org-my', expiresAt: null };
      jest.mocked(apiKeysApi.createApiKey).mockResolvedValue(created);

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);

      const wrapper = mountView();
      await flushPromises();

      await wrapper.find('[data-testid="tab-api-keys"]').trigger('click');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="create-api-key-btn"]').trigger('click');
      await wrapper.vm.$nextTick();

      await (wrapper.findComponent('[data-testid="api-key-name-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'My Key');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="save-api-key-btn"]').trigger('click');
      await flushPromises();

      const rawKeyField = wrapper.findComponent('[data-testid="raw-key-field"]') as VueWrapper<any>;
      expect(rawKeyField.props('modelValue')).toBe('raw-secret-key');
    });
  });

  describe('WhenDeleteApiKeyIsClicked', () => {
    it('ThenDeleteApiKeyIsCalledAndKeyIsRemovedFromList', async () => {
      jest.mocked(apiKeysApi.listApiKeys).mockResolvedValue([...mockApiKeys]);
      jest.mocked(apiKeysApi.deleteApiKey).mockResolvedValue(undefined);

      const contextStore = useContextStore();
      contextStore.setOrganization(mockOrg);

      const wrapper = mountView();
      await flushPromises();

      await wrapper.find('[data-testid="tab-api-keys"]').trigger('click');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="delete-api-key-1"]').trigger('click');
      await flushPromises();

      expect(apiKeysApi.deleteApiKey).toHaveBeenCalledWith(mockOrg.id, 1);
      expect(wrapper.find('[data-testid="api-key-row-1"]').exists()).toBe(false);
    });
  });
});
