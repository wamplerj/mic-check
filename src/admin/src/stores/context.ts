import { ref } from 'vue';
import { defineStore } from 'pinia';
import type { OrganizationResponse, ProjectResponse, EnvironmentResponse } from '@/types/api';

const STORAGE_KEYS = {
  organization: 'mic_ctx_organization',
  project: 'mic_ctx_project',
  environment: 'mic_ctx_environment',
} as const;

export const useContextStore = defineStore('context', () => {
  const currentOrganization = ref<OrganizationResponse | null>(null);
  const currentProject = ref<ProjectResponse | null>(null);
  const currentEnvironment = ref<EnvironmentResponse | null>(null);

  function setOrganization(org: OrganizationResponse | null): void {
    currentOrganization.value = org;
    currentProject.value = null;
    currentEnvironment.value = null;
    if (org) {
      localStorage.setItem(STORAGE_KEYS.organization, JSON.stringify(org));
    } else {
      localStorage.removeItem(STORAGE_KEYS.organization);
    }
    localStorage.removeItem(STORAGE_KEYS.project);
    localStorage.removeItem(STORAGE_KEYS.environment);
  }

  function setProject(project: ProjectResponse | null): void {
    currentProject.value = project;
    currentEnvironment.value = null;
    if (project) {
      localStorage.setItem(STORAGE_KEYS.project, JSON.stringify(project));
    } else {
      localStorage.removeItem(STORAGE_KEYS.project);
    }
    localStorage.removeItem(STORAGE_KEYS.environment);
  }

  function setEnvironment(environment: EnvironmentResponse | null): void {
    currentEnvironment.value = environment;
    if (environment) {
      localStorage.setItem(STORAGE_KEYS.environment, JSON.stringify(environment));
    } else {
      localStorage.removeItem(STORAGE_KEYS.environment);
    }
  }

  function refreshOrganization(org: OrganizationResponse): void {
    currentOrganization.value = org;
    localStorage.setItem(STORAGE_KEYS.organization, JSON.stringify(org));
  }

  function refreshProject(project: ProjectResponse): void {
    currentProject.value = project;
    localStorage.setItem(STORAGE_KEYS.project, JSON.stringify(project));
  }

  function refreshEnvironment(environment: EnvironmentResponse): void {
    currentEnvironment.value = environment;
    localStorage.setItem(STORAGE_KEYS.environment, JSON.stringify(environment));
  }

  function loadFromStorage(): void {
    try {
      const orgRaw = localStorage.getItem(STORAGE_KEYS.organization);
      const projectRaw = localStorage.getItem(STORAGE_KEYS.project);
      const envRaw = localStorage.getItem(STORAGE_KEYS.environment);

      if (orgRaw) currentOrganization.value = JSON.parse(orgRaw) as OrganizationResponse;
      if (projectRaw) currentProject.value = JSON.parse(projectRaw) as ProjectResponse;
      if (envRaw) currentEnvironment.value = JSON.parse(envRaw) as EnvironmentResponse;
    } catch {
      // Corrupt storage — clear it
      localStorage.removeItem(STORAGE_KEYS.organization);
      localStorage.removeItem(STORAGE_KEYS.project);
      localStorage.removeItem(STORAGE_KEYS.environment);
    }
  }

  function clearContext(): void {
    currentOrganization.value = null;
    currentProject.value = null;
    currentEnvironment.value = null;
    localStorage.removeItem(STORAGE_KEYS.organization);
    localStorage.removeItem(STORAGE_KEYS.project);
    localStorage.removeItem(STORAGE_KEYS.environment);
  }

  return {
    currentOrganization,
    currentProject,
    currentEnvironment,
    setOrganization,
    setProject,
    setEnvironment,
    refreshOrganization,
    refreshProject,
    refreshEnvironment,
    loadFromStorage,
    clearContext,
  };
});
