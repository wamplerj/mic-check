import { describe, it, expect, beforeEach, afterEach, jest } from '@jest/globals';
import { createRouter, createMemoryHistory } from 'vue-router';
import { setActivePinia, createPinia } from 'pinia';
import { clearAuthTokens } from '@/api/client';
import { authGuard } from '@/router';

// Minimal stub components for route testing
const Stub = { template: '<div />' };

jest.mock('@/api/client', () => ({
  setAuthTokens: jest.fn(),
  clearAuthTokens: jest.fn(),
  getAccessToken: jest.fn(),
}));

import { getAccessToken } from '@/api/client';

function buildRouter() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/login', name: 'Login', component: Stub, meta: { public: true } },
      { path: '/register', name: 'Register', component: Stub, meta: { public: true } },
      {
        path: '/accept-invite/:token',
        name: 'AcceptInvite',
        component: Stub,
        meta: { public: true, allowAuthenticated: true },
      },
      {
        path: '/',
        component: Stub,
        meta: { requiresAuth: true },
        children: [
          { path: '', name: 'Dashboard', component: Stub },
          { path: 'features', name: 'Features', component: Stub },
        ],
      },
    ],
  });
  // Wire up the real guard exported by the app router, not a re-implementation.
  router.beforeEach(authGuard);
  return router;
}

describe('Router auth guard', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    jest.clearAllMocks();
  });

  afterEach(() => {
    clearAuthTokens();
  });

  describe('WhenUnauthenticatedUserNavigatesToProtectedRoute', () => {
    it('ThenTheyAreRedirectedToLoginWithARedirectQuery', async () => {
      jest.mocked(getAccessToken).mockReturnValue(null);

      const router = buildRouter();
      await router.push('/features');

      expect(router.currentRoute.value.name).toBe('Login');
      expect(router.currentRoute.value.query.redirect).toBe('/features');
    });
  });

  describe('WhenAuthenticatedUserNavigatesToLogin', () => {
    it('ThenTheyAreRedirectedToDashboard', async () => {
      jest.mocked(getAccessToken).mockReturnValue('valid-token');

      const router = buildRouter();
      await router.push('/');
      await router.push('/login');

      expect(router.currentRoute.value.name).toBe('Dashboard');
    });
  });

  describe('WhenAuthenticatedUserNavigatesToProtectedRoute', () => {
    it('ThenNavigationSucceeds', async () => {
      jest.mocked(getAccessToken).mockReturnValue('valid-token');

      const router = buildRouter();
      await router.push('/features');

      expect(router.currentRoute.value.name).toBe('Features');
    });
  });

  describe('WhenUnauthenticatedUserNavigatesToAnAllowAuthenticatedRoute', () => {
    it('ThenNavigationSucceeds', async () => {
      jest.mocked(getAccessToken).mockReturnValue(null);

      const router = buildRouter();
      await router.push('/accept-invite/some-token');

      expect(router.currentRoute.value.name).toBe('AcceptInvite');
    });
  });

  describe('WhenAuthenticatedUserNavigatesToAnAllowAuthenticatedRoute', () => {
    it('ThenTheyAreNotRedirectedToDashboard', async () => {
      jest.mocked(getAccessToken).mockReturnValue('valid-token');

      const router = buildRouter();
      await router.push('/accept-invite/some-token');

      expect(router.currentRoute.value.name).toBe('AcceptInvite');
    });
  });

  describe('WhenAuthenticatedUserNavigatesToRegister', () => {
    it('ThenTheyAreRedirectedToDashboardBecauseRegisterHasNoAllowAuthenticatedException', async () => {
      jest.mocked(getAccessToken).mockReturnValue('valid-token');

      const router = buildRouter();
      await router.push('/');
      await router.push('/register');

      expect(router.currentRoute.value.name).toBe('Dashboard');
    });
  });

  describe('WhenUnauthenticatedUserNavigatesToAProtectedRouteWithAQueryString', () => {
    it('ThenTheFullPathIncludingTheQueryIsPreservedInTheRedirect', async () => {
      jest.mocked(getAccessToken).mockReturnValue(null);

      const router = buildRouter();
      await router.push('/features?tag=beta');

      expect(router.currentRoute.value.name).toBe('Login');
      expect(router.currentRoute.value.query.redirect).toBe('/features?tag=beta');
    });
  });
});
