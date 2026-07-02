import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { mount, flushPromises } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { createRouter, createMemoryHistory } from 'vue-router';
import RegisterView from '@/views/RegisterView.vue';
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
    { path: '/login', component: { template: '<div />' } },
  ];
  const router = createRouter({ history: createMemoryHistory(), routes });
  return { wrapper: mount(RegisterView, { global: { plugins: [router] } }), router };
}

interface RegisterFields {
  firstName: string;
  lastName: string;
  email: string;
  organizationName: string;
  password: string;
  confirmPassword: string;
}

const validFields: RegisterFields = {
  firstName: 'Jane',
  lastName: 'Doe',
  email: 'jane@example.com',
  organizationName: 'Acme',
  password: 'password123',
  confirmPassword: 'password123',
};

async function fillAndSubmit(wrapper: ReturnType<typeof mount>, fields: Partial<RegisterFields> = {}) {
  const values = { ...validFields, ...fields };
  await wrapper.find('[data-testid="first-name-input"] input').setValue(values.firstName);
  await wrapper.find('[data-testid="last-name-input"] input').setValue(values.lastName);
  await wrapper.find('[data-testid="email-input"] input').setValue(values.email);
  await wrapper.find('[data-testid="org-name-input"] input').setValue(values.organizationName);
  await wrapper.find('[data-testid="password-input"] input').setValue(values.password);
  await wrapper.find('[data-testid="confirm-password-input"] input').setValue(values.confirmPassword);
  await wrapper.find('[data-testid="register-form"]').trigger('submit.prevent');
  await flushPromises();
}

describe('RegisterView', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    jest.clearAllMocks();
  });

  describe('WhenTheFormIsEmpty', () => {
    it('ThenSubmitDoesNotCallRegister', async () => {
      const { wrapper } = mountView();

      await wrapper.find('[data-testid="register-form"]').trigger('submit.prevent');
      await flushPromises();

      expect(authApi.register).not.toHaveBeenCalled();
    });
  });

  describe('WhenThePasswordIsTooShort', () => {
    it('ThenSubmitDoesNotCallRegister', async () => {
      const { wrapper } = mountView();

      await fillAndSubmit(wrapper, { password: 'short', confirmPassword: 'short' });

      expect(authApi.register).not.toHaveBeenCalled();
    });
  });

  describe('WhenTheConfirmPasswordDoesNotMatch', () => {
    it('ThenSubmitDoesNotCallRegister', async () => {
      const { wrapper } = mountView();

      await fillAndSubmit(wrapper, { confirmPassword: 'somethingElse123' });

      expect(authApi.register).not.toHaveBeenCalled();
    });
  });

  describe('WhenAllFieldsAreValidAndRegisterSucceeds', () => {
    it('ThenAuthStoreRegisterIsCalledWithFormValuesAndUserIsRedirectedHome', async () => {
      jest.mocked(authApi.register).mockResolvedValue(tokenResponse);

      const { wrapper, router } = mountView();
      const pushSpy = jest.spyOn(router, 'push');

      await fillAndSubmit(wrapper);

      expect(authApi.register).toHaveBeenCalledWith({
        email: 'jane@example.com',
        password: 'password123',
        firstName: 'Jane',
        lastName: 'Doe',
        organizationName: 'Acme',
      });
      expect(pushSpy).toHaveBeenCalledWith('/');
    });

    it('ThenAuthStoreBecomesAuthenticated', async () => {
      jest.mocked(authApi.register).mockResolvedValue(tokenResponse);

      const { wrapper } = mountView();
      const authStore = useAuthStore();

      await fillAndSubmit(wrapper);

      expect(authStore.isAuthenticated).toBe(true);
    });
  });

  describe('WhenRegisterFails', () => {
    it('ThenAnErrorMessageIsShownAndUserIsNotRedirected', async () => {
      jest.mocked(authApi.register).mockRejectedValue(new Error('Email already registered'));

      const { wrapper, router } = mountView();
      const pushSpy = jest.spyOn(router, 'push');

      await fillAndSubmit(wrapper);

      expect(wrapper.find('[data-testid="register-error"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Registration failed');
      expect(pushSpy).not.toHaveBeenCalled();
    });

    it('ThenTheSubmitButtonIsNoLongerLoading', async () => {
      jest.mocked(authApi.register).mockRejectedValue(new Error('Email already registered'));

      const { wrapper } = mountView();

      await fillAndSubmit(wrapper);

      const submitBtn = wrapper.findComponent('[data-testid="register-submit"]');
      expect(submitBtn.props('loading')).toBe(false);
    });
  });
});
