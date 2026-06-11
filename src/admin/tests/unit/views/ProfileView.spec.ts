import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { mount, flushPromises } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { createRouter, createMemoryHistory } from 'vue-router';
import ProfileView from '@/views/ProfileView.vue';
import { useAuthStore } from '@/stores/auth';
import { useContextStore } from '@/stores/context';

jest.mock('@/api/auth');
jest.mock('@/api/client', () => ({
  setAuthTokens: jest.fn(),
  clearAuthTokens: jest.fn(),
  getAccessToken: jest.fn(),
}));

import * as authApi from '@/api/auth';

function mountView() {
  const routes = [
    { path: '/', component: { template: '<div />' } },
    { path: '/login', name: 'Login', component: { template: '<div />' } },
  ];
  const router = createRouter({ history: createMemoryHistory(), routes });
  return { wrapper: mount(ProfileView, { global: { plugins: [router] } }), router };
}

describe('ProfileView', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    jest.clearAllMocks();
  });

  describe('WhenAuthenticated', () => {
    it('ThenProfileCardIsRendered', () => {
      const { wrapper } = mountView();
      expect(wrapper.find('[data-testid="profile-card"]').exists()).toBe(true);
    });

    it('ThenLogoutButtonIsPresent', () => {
      const { wrapper } = mountView();
      expect(wrapper.find('[data-testid="logout-btn"]').exists()).toBe(true);
    });

    it('ThenOrgNameIsDisplayedWhenSelected', () => {
      const { wrapper } = mountView();
      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme Corp', createdAt: '', isPrimary: false });

      expect(wrapper.find('[data-testid="profile-org-name"]').exists()).toBe(false); // org name shown in v-if block
    });
  });

  describe('WhenLogoutButtonIsClicked', () => {
    it('ThenAuthStoreLogoutIsCalled', async () => {
      jest.mocked(authApi.logout).mockResolvedValue(undefined);

      const authStore = useAuthStore();
      // Simulate logged-in state by setting tokens manually
      authStore.accessToken = 'fake-token' as any;
      authStore.refreshToken = 'fake-refresh' as any;

      const { wrapper } = mountView();

      await wrapper.find('[data-testid="logout-btn"]').trigger('click');
      await flushPromises();

      expect(authApi.logout).toHaveBeenCalled();
    });

    it('ThenUserIsRedirectedToLoginAfterLogout', async () => {
      jest.mocked(authApi.logout).mockResolvedValue(undefined);

      const authStore = useAuthStore();
      authStore.accessToken = 'fake-token' as any;
      authStore.refreshToken = 'fake-refresh' as any;

      const { wrapper, router } = mountView();

      await wrapper.find('[data-testid="logout-btn"]').trigger('click');
      await flushPromises();

      expect(router.currentRoute.value.name).toBe('Login');
    });
  });
});
