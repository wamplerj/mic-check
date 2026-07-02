import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { mount, flushPromises } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import AcceptInviteView from '@/views/AcceptInviteView.vue';

jest.mock('@/api/organizations');
jest.mock('@/api/client', () => ({
  setAuthTokens: jest.fn(),
  clearAuthTokens: jest.fn(),
  getAccessToken: jest.fn(),
}));

import * as orgsApi from '@/api/organizations';
import { getAccessToken } from '@/api/client';

async function mountView(token = 'invite-token-123') {
  const routes = [
    { path: '/accept-invite/:token', name: 'AcceptInvite', component: AcceptInviteView },
    { path: '/dashboard', component: { template: '<div />' } },
    { path: '/login', name: 'Login', component: { template: '<div />' } },
  ];
  const router = createRouter({ history: createMemoryHistory(), routes });
  router.push(`/accept-invite/${token}`);
  await router.isReady();
  return { wrapper: mount(AcceptInviteView, { global: { plugins: [router] } }), router };
}

describe('AcceptInviteView', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('WhenTheUserIsNotAuthenticated', () => {
    it('ThenTheSignInPromptIsShownAndAcceptInviteIsNotCalled', async () => {
      jest.mocked(getAccessToken).mockReturnValue(null);

      const { wrapper } = await mountView();
      await flushPromises();

      expect(wrapper.text()).toContain('Sign in to accept invite');
      expect(orgsApi.acceptInvite).not.toHaveBeenCalled();
    });

    it('ThenTheSignInButtonNavigatesToLoginWithARedirectQuery', async () => {
      jest.mocked(getAccessToken).mockReturnValue(null);

      const { wrapper, router } = await mountView('invite-token-123');
      await flushPromises();
      const pushSpy = jest.spyOn(router, 'push');

      await wrapper.find('button').trigger('click');

      expect(pushSpy).toHaveBeenCalledWith({
        name: 'Login',
        query: { redirect: '/accept-invite/invite-token-123' },
      });
    });
  });

  describe('WhenTheUserIsAuthenticatedAndTheInviteIsAccepted', () => {
    it('ThenTheSuccessStateIsShown', async () => {
      jest.mocked(getAccessToken).mockReturnValue('access-token');
      jest.mocked(orgsApi.acceptInvite).mockResolvedValue(undefined);

      const { wrapper } = await mountView('invite-token-123');
      await flushPromises();

      expect(orgsApi.acceptInvite).toHaveBeenCalledWith('invite-token-123');
      expect(wrapper.text()).toContain("You're in!");
    });
  });

  describe('WhenTheUserIsAuthenticatedButTheInviteIsInvalid', () => {
    it('ThenTheErrorStateIsShown', async () => {
      jest.mocked(getAccessToken).mockReturnValue('access-token');
      jest.mocked(orgsApi.acceptInvite).mockRejectedValue(new Error('Invalid token'));

      const { wrapper } = await mountView('bad-token');
      await flushPromises();

      expect(wrapper.text()).toContain('Invalid invite link');
    });
  });
});
