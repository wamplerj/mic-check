import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router';
import { getAccessToken } from '@/api/client';
import AppLayout from '@/components/AppLayout.vue';
import LoginView from '@/views/LoginView.vue';
import RegisterView from '@/views/RegisterView.vue';
import DashboardView from '@/views/DashboardView.vue';
import FeaturesView from '@/views/FeaturesView.vue';
import SegmentsView from '@/views/SegmentsView.vue';
import IdentitiesView from '@/views/IdentitiesView.vue';
import AuditLogsView from '@/views/AuditLogsView.vue';
import EnvironmentsView from '@/views/EnvironmentsView.vue';
import ProjectsView from '@/views/ProjectsView.vue';
import ProfileView from '@/views/ProfileView.vue';
import SettingsView from '@/views/SettingsView.vue';

const routes: RouteRecordRaw[] = [
  // ─── Public routes (no layout) ────────────────────────────────────────────
  {
    path: '/login',
    name: 'Login',
    component: LoginView,
    meta: { public: true },
  },
  {
    path: '/register',
    name: 'Register',
    component: RegisterView,
    meta: { public: true },
  },

  // ─── Authenticated routes (wrapped in AppLayout) ───────────────────────────
  {
    path: '/',
    component: AppLayout,
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        name: 'Dashboard',
        component: DashboardView,
      },
      {
        path: 'features',
        name: 'Features',
        component: FeaturesView,
      },
      {
        path: 'segments',
        name: 'Segments',
        component: SegmentsView,
      },
      {
        path: 'identities',
        name: 'Identities',
        component: IdentitiesView,
      },
      {
        path: 'audit-logs',
        name: 'AuditLogs',
        component: AuditLogsView,
      },
      {
        path: 'environments',
        name: 'Environments',
        component: EnvironmentsView,
      },
      {
        path: 'projects',
        name: 'Projects',
        component: ProjectsView,
      },
      {
        path: 'profile',
        name: 'Profile',
        component: ProfileView,
      },
      {
        path: 'settings',
        name: 'Settings',
        component: SettingsView,
      },
    ],
  },

  // ─── Fallback ─────────────────────────────────────────────────────────────
  {
    path: '/:pathMatch(.*)*',
    redirect: '/',
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

  if (isPublic && isAuthenticated) {
    return { name: 'Dashboard' };
  }

  return true;
});

export default router;
