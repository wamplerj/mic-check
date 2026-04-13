import { describe, it, expect, beforeEach } from '@jest/globals';
import { setActivePinia, createPinia } from 'pinia';
import { useContextStore } from '@/stores/context';
import type { OrganizationResponse, ProjectResponse, EnvironmentResponse } from '@/types/api';

const mockOrg: OrganizationResponse = { id: 1, name: 'Acme', createdAt: '2026-01-01T00:00:00Z' };
const mockProject: ProjectResponse = {
  id: 10,
  name: 'Website',
  organizationId: 1,
  hideDisabledFlags: false,
  createdAt: '2026-01-01T00:00:00Z',
};
const mockEnv: EnvironmentResponse = {
  id: 100,
  name: 'Production',
  apiKey: 'env-key-abc',
  projectId: 10,
  createdAt: '2026-01-01T00:00:00Z',
};

describe('useContextStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    localStorage.clear();
  });

  describe('WhenOrganizationIsSelected', () => {
    it('ThenCurrentOrganizationIsUpdated', () => {
      const store = useContextStore();
      store.setOrganization(mockOrg);

      expect(store.currentOrganization).toEqual(mockOrg);
    });

    it('ThenOrganizationIsPersistedToLocalStorage', () => {
      const store = useContextStore();
      store.setOrganization(mockOrg);

      const stored = JSON.parse(localStorage.getItem('mic_ctx_organization')!);
      expect(stored).toEqual(mockOrg);
    });

    it('ThenProjectAndEnvironmentAreReset', () => {
      const store = useContextStore();
      store.setOrganization(mockOrg);
      store.setProject(mockProject);
      store.setEnvironment(mockEnv);

      store.setOrganization({ ...mockOrg, id: 2 });

      expect(store.currentProject).toBeNull();
      expect(store.currentEnvironment).toBeNull();
    });
  });

  describe('WhenProjectIsSelected', () => {
    it('ThenCurrentProjectIsUpdated', () => {
      const store = useContextStore();
      store.setOrganization(mockOrg);
      store.setProject(mockProject);

      expect(store.currentProject).toEqual(mockProject);
    });

    it('ThenEnvironmentIsReset', () => {
      const store = useContextStore();
      store.setOrganization(mockOrg);
      store.setProject(mockProject);
      store.setEnvironment(mockEnv);

      store.setProject({ ...mockProject, id: 11 });

      expect(store.currentEnvironment).toBeNull();
    });
  });

  describe('WhenEnvironmentIsSelected', () => {
    it('ThenCurrentEnvironmentIsUpdated', () => {
      const store = useContextStore();
      store.setEnvironment(mockEnv);

      expect(store.currentEnvironment).toEqual(mockEnv);
    });

    it('ThenEnvironmentIsPersistedToLocalStorage', () => {
      const store = useContextStore();
      store.setEnvironment(mockEnv);

      const stored = JSON.parse(localStorage.getItem('mic_ctx_environment')!);
      expect(stored).toEqual(mockEnv);
    });
  });

  describe('WhenLoadFromStorageIsCalledWithSavedContext', () => {
    it('ThenContextIsRestoredFromLocalStorage', () => {
      localStorage.setItem('mic_ctx_organization', JSON.stringify(mockOrg));
      localStorage.setItem('mic_ctx_project', JSON.stringify(mockProject));
      localStorage.setItem('mic_ctx_environment', JSON.stringify(mockEnv));

      const store = useContextStore();
      store.loadFromStorage();

      expect(store.currentOrganization).toEqual(mockOrg);
      expect(store.currentProject).toEqual(mockProject);
      expect(store.currentEnvironment).toEqual(mockEnv);
    });
  });

  describe('WhenLoadFromStorageEncountersCorruptData', () => {
    it('ThenContextRemainsNullAndStorageIsCleared', () => {
      localStorage.setItem('mic_ctx_organization', 'not-valid-json{{');

      const store = useContextStore();
      store.loadFromStorage();

      expect(store.currentOrganization).toBeNull();
      expect(localStorage.getItem('mic_ctx_organization')).toBeNull();
    });
  });

  describe('WhenClearContextIsCalled', () => {
    it('ThenAllContextIsRemovedFromStateAndStorage', () => {
      const store = useContextStore();
      store.setOrganization(mockOrg);
      store.setProject(mockProject);
      store.setEnvironment(mockEnv);

      store.clearContext();

      expect(store.currentOrganization).toBeNull();
      expect(store.currentProject).toBeNull();
      expect(store.currentEnvironment).toBeNull();
      expect(localStorage.getItem('mic_ctx_organization')).toBeNull();
    });
  });
});
