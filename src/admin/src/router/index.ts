import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router';
import { getAccessToken } from '@/api/client';

const routes: RouteRecordRaw[] = [
  // Root redirect
  { path: '/', redirect: '/dashboard' },

  // ─── Authenticated routes (default layout with vertical nav) ──────────────
  {
    path: '/',
    component: () => import('@/layouts/default.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/views/DashboardView.vue'),
      },
      {
        path: 'features',
        name: 'Features',
        component: () => import('@/views/FeaturesView.vue'),
      },
      {
        path: 'segments',
        name: 'Segments',
        component: () => import('@/views/SegmentsView.vue'),
      },
      {
        path: 'identities',
        name: 'Identities',
        component: () => import('@/views/IdentitiesView.vue'),
      },
      {
        path: 'audit-logs',
        name: 'AuditLogs',
        component: () => import('@/views/AuditLogsView.vue'),
      },
      {
        path: 'environments',
        name: 'Environments',
        component: () => import('@/views/EnvironmentsView.vue'),
      },
      {
        path: 'projects',
        name: 'Projects',
        component: () => import('@/views/ProjectsView.vue'),
      },
      {
        path: 'profile',
        name: 'Profile',
        component: () => import('@/views/ProfileView.vue'),
      },
      {
        path: 'users/:id',
        name: 'UserProfile',
        component: () => import('@/views/UserView.vue'),
      },
      {
        path: 'settings',
        name: 'Settings',
        component: () => import('@/views/SettingsView.vue'),
      },
    ],
  },

  // ─── Public routes (blank layout) ─────────────────────────────────────────
  {
    path: '/',
    component: () => import('@/layouts/blank.vue'),
    children: [
      {
        path: 'login',
        name: 'Login',
        component: () => import('@/views/LoginView.vue'),
        meta: { public: true },
      },
      {
        path: 'accept-invite/:token',
        name: 'AcceptInvite',
        component: () => import('@/views/AcceptInviteView.vue'),
        meta: { public: true, allowAuthenticated: true },
      },
      {
        path: 'register',
        name: 'Register',
        component: () => import('@/views/RegisterView.vue'),
        meta: { public: true },
      },
      {
        path: ':pathMatch(.*)*',
        redirect: '/dashboard',
      },
    ],
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

// ─── Auth Guard ───────────────────────────────────────────────────────────────

router.beforeEach((to) => {
  const isAuthenticated = !!getAccessToken();
  const requiresAuth = to.matched.some((record) => record.meta.requiresAuth);
  const isPublic = to.matched.some((record) => record.meta.public);

  if (requiresAuth && !isAuthenticated) {
    return { name: 'Login', query: { redirect: to.fullPath } };
  }

  if (isPublic && isAuthenticated && !to.matched.some((r) => r.meta.allowAuthenticated)) {
    return { name: 'Dashboard' };
  }

  return true;
});

export default router;
