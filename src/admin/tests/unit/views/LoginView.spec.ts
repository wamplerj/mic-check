import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { mount, flushPromises } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { createRouter, createMemoryHistory } from 'vue-router';
import LoginView from '@/views/LoginView.vue';
import { useAuthStore } from '@/stores/auth';

jest.mock('@/api/auth');
jest.mock('@/api/client', () => ({
  setAuthTokens: jest.fn(),
  clearAuthTokens: jest.fn(),
  getAccessToken: jest.fn(),
}));

import * as authApi from '@/api/auth';

const tokenResponse = {
  accessToken: 'access-token',
  refreshToken: 'refresh-token',
  expiresAt: '2026-01-01T01:00:00Z',
};

function mountView() {
  const routes = [
    { path: '/', component: { template: '<div />' } },
    { path: '/register', component: { template: '<div />' } },
  ];
  const router = createRouter({ history: createMemoryHistory(), routes });
  return { wrapper: mount(LoginView, { global: { plugins: [router] } }), router };
}

async function fillAndSubmit(wrapper: ReturnType<typeof mount>, email: string, password: string) {
  await wrapper.find('[data-testid="email-input"] input').setValue(email);
  await wrapper.find('[data-testid="password-input"] input').setValue(password);
  await wrapper.find('[data-testid="login-form"]').trigger('submit.prevent');
  await flushPromises();
}

describe('LoginView', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    jest.clearAllMocks();
  });

  describe('WhenTheFormIsEmpty', () => {
    it('ThenSubmitDoesNotCallLogin', async () => {
      const { wrapper } = mountView();

      await wrapper.find('[data-testid="login-form"]').trigger('submit.prevent');
      await flushPromises();

      expect(authApi.login).not.toHaveBeenCalled();
    });
  });

  describe('WhenTheEmailIsInvalid', () => {
    it('ThenSubmitDoesNotCallLogin', async () => {
      const { wrapper } = mountView();

      await fillAndSubmit(wrapper, 'not-an-email', 'password123');

      expect(authApi.login).not.toHaveBeenCalled();
    });
  });

  describe('WhenCredentialsAreValidAndLoginSucceeds', () => {
    it('ThenAuthStoreLoginIsCalledAndUserIsRedirectedHome', async () => {
      jest.mocked(authApi.login).mockResolvedValue(tokenResponse);

      const { wrapper, router } = mountView();
      const pushSpy = jest.spyOn(router, 'push');

      await fillAndSubmit(wrapper, 'jane@example.com', 'password123');

      expect(authApi.login).toHaveBeenCalledWith({ email: 'jane@example.com', password: 'password123' });
      expect(pushSpy).toHaveBeenCalledWith('/');
    });

    it('ThenAuthStoreBecomesAuthenticated', async () => {
      jest.mocked(authApi.login).mockResolvedValue(tokenResponse);

      const { wrapper } = mountView();
      const authStore = useAuthStore();

      await fillAndSubmit(wrapper, 'jane@example.com', 'password123');

      expect(authStore.isAuthenticated).toBe(true);
    });
  });

  describe('WhenLoginFails', () => {
    it('ThenAnErrorMessageIsShownAndUserIsNotRedirected', async () => {
      jest.mocked(authApi.login).mockRejectedValue(new Error('Unauthorized'));

      const { wrapper, router } = mountView();
      const pushSpy = jest.spyOn(router, 'push');

      await fillAndSubmit(wrapper, 'jane@example.com', 'wrong-password');

      expect(wrapper.find('[data-testid="login-error"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Invalid email or password');
      expect(pushSpy).not.toHaveBeenCalled();
    });

    it('ThenTheSubmitButtonIsNoLongerLoading', async () => {
      jest.mocked(authApi.login).mockRejectedValue(new Error('Unauthorized'));

      const { wrapper } = mountView();

      await fillAndSubmit(wrapper, 'jane@example.com', 'wrong-password');

      const submitBtn = wrapper.findComponent('[data-testid="login-submit"]');
      expect(submitBtn.props('loading')).toBe(false);
    });
  });

  describe('WhenThePasswordVisibilityIconIsClicked', () => {
    it('ThenThePasswordFieldTypeToggles', async () => {
      const { wrapper } = mountView();

      const passwordInput = () => wrapper.find('[data-testid="password-input"] input');
      expect(passwordInput().attributes('type')).toBe('password');

      await wrapper.find('[data-testid="password-input"] .v-field__append-inner i').trigger('click');

      expect(passwordInput().attributes('type')).toBe('text');
    });
  });
});
