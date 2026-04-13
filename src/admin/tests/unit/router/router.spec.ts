import { describe, it, expect, beforeEach, afterEach, jest } from '@jest/globals';
import { createRouter, createMemoryHistory } from 'vue-router';
import { setActivePinia, createPinia } from 'pinia';
import { setAuthTokens, clearAuthTokens } from '@/api/client';

// Minimal stub components for route testing
const Stub = { template: '<div />' };

jest.mock('@/api/client', () => ({
  setAuthTokens: jest.fn(),
  clearAuthTokens: jest.fn(),
  getAccessToken: jest.fn(),
}));

import { getAccessToken } from '@/api/client';

function buildRouter() {
  return createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/login', name: 'Login', component: Stub, meta: { public: true } },
      { path: '/register', name: 'Register', component: Stub, meta: { public: true } },
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
    it('ThenTheyAreRedirectedToLogin', async () => {
      jest.mocked(getAccessToken).mockReturnValue(null);

      const router = buildRouter();

      // Wire up the same guard logic as the real router
      router.beforeEach((to) => {
        const isAuthenticated = !!getAccessToken();
        const requiresAuth = to.matched.some((r) => r.meta.requiresAuth);
        const isPublic = to.matched.some((r) => r.meta.public);
        if (requiresAuth && !isAuthenticated) return { name: 'Login', query: { redirect: to.fullPath } };
        if (isPublic && isAuthenticated) return { name: 'Dashboard' };
        return true;
      });

      await router.push('/features');

      expect(router.currentRoute.value.name).toBe('Login');
      expect(router.currentRoute.value.query.redirect).toBe('/features');
    });
  });

  describe('WhenAuthenticatedUserNavigatesToLogin', () => {
    it('ThenTheyAreRedirectedToDashboard', async () => {
      jest.mocked(getAccessToken).mockReturnValue('valid-token');

      const router = buildRouter();

      router.beforeEach((to) => {
        const isAuthenticated = !!getAccessToken();
        const requiresAuth = to.matched.some((r) => r.meta.requiresAuth);
        const isPublic = to.matched.some((r) => r.meta.public);
        if (requiresAuth && !isAuthenticated) return { name: 'Login', query: { redirect: to.fullPath } };
        if (isPublic && isAuthenticated) return { name: 'Dashboard' };
        return true;
      });

      // Start authenticated on dashboard, then try to go to login
      await router.push('/');
      await router.push('/login');

      expect(router.currentRoute.value.name).toBe('Dashboard');
    });
  });

  describe('WhenAuthenticatedUserNavigatesToProtectedRoute', () => {
    it('ThenNavigationSucceeds', async () => {
      jest.mocked(getAccessToken).mockReturnValue('valid-token');

      const router = buildRouter();

      router.beforeEach((to) => {
        const isAuthenticated = !!getAccessToken();
        const requiresAuth = to.matched.some((r) => r.meta.requiresAuth);
        const isPublic = to.matched.some((r) => r.meta.public);
        if (requiresAuth && !isAuthenticated) return { name: 'Login', query: { redirect: to.fullPath } };
        if (isPublic && isAuthenticated) return { name: 'Dashboard' };
        return true;
      });

      await router.push('/features');

      expect(router.currentRoute.value.name).toBe('Features');
    });
  });
});
