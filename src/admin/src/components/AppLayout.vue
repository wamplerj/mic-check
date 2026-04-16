<template>
  <div>
    <!-- ─── Top App Bar ──────────────────────────────────────────────────── -->
    <v-app-bar color="primary" elevation="2" height="56">
      <v-app-bar-nav-icon
        variant="text"
        color="white"
        @click="rail = !rail"
        data-testid="nav-toggle"
      />

      <v-app-bar-title>
        <span class="text-body-1 font-weight-medium text-white" data-testid="page-title">
          {{ pageTitle }}
        </span>
      </v-app-bar-title>

      <template #append>
        <div class="d-flex align-center ga-1 mr-2">
          <v-btn
            to="/profile"
            variant="text"
            color="white"
            prepend-icon="mdi-account-circle"
            rounded="lg"
            data-testid="profile-link"
          >
            {{ userDisplayName }}
          </v-btn>
          <v-btn
            to="/settings"
            icon
            variant="text"
            color="white"
            data-testid="settings-link"
          >
            <v-icon>mdi-cog-outline</v-icon>
          </v-btn>
        </div>
      </template>
    </v-app-bar>

    <!-- ─── Navigation Drawer ────────────────────────────────────────────── -->
    <v-navigation-drawer
      v-model="drawer"
      :rail="rail"
      permanent
      color="surface"
      width="240"
    >
      <!-- Logo -->
      <router-link to="/dashboard" class="d-flex align-center pa-4 pb-3 text-decoration-none" data-testid="sidebar-logo">
        <img :src="logoUrl" alt="MicCheck" height="32" width="32" class="flex-shrink-0" />
        <span v-if="!rail" class="text-h6 font-weight-bold ml-3 text-high-emphasis">MicCheck</span>
      </router-link>

      <v-divider />

      <!-- Context Selectors (hidden in rail mode) -->
      <div v-if="!rail" class="pa-3 d-flex flex-column ga-3" data-testid="context-selectors">
        <OrgSelector />
        <ProjectSelector />
        <EnvironmentTabBar />
      </div>

      <v-divider v-if="!rail" />

      <!-- Navigation Links -->
      <v-list density="compact" nav class="mt-1">
        <v-list-item
          v-for="link in navLinks"
          :key="link.route"
          :to="link.route"
          :prepend-icon="link.icon"
          :title="link.label"
          active-color="primary"
          rounded="lg"
          :data-testid="`nav-link-${link.label.toLowerCase().replace(' ', '-')}`"
        />
      </v-list>

      <!-- Settings at bottom -->
      <template #append>
        <v-divider />
        <v-list density="compact" nav class="mb-1">
          <v-list-item
            to="/settings"
            prepend-icon="mdi-cog-outline"
            title="Settings"
            active-color="primary"
            rounded="lg"
            data-testid="nav-link-settings"
          />
        </v-list>
      </template>
    </v-navigation-drawer>

    <!-- ─── Main Content ──────────────────────────────────────────────────── -->
    <v-main>
      <v-container fluid class="pa-6">
        <router-view />
      </v-container>
    </v-main>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRoute } from 'vue-router';
import OrgSelector from '@/components/nav/OrgSelector.vue';
import ProjectSelector from '@/components/nav/ProjectSelector.vue';
import EnvironmentTabBar from '@/components/nav/EnvironmentTabBar.vue';
import logoUrl from '@/assets/logo.svg';

const route = useRoute();

const drawer = ref(true);
const rail = ref(false);

// Placeholder until a /me endpoint is available
const userDisplayName = 'Account';

const navLinks = [
  { label: 'Dashboard', route: '/', icon: 'mdi-view-dashboard' },
  { label: 'Features', route: '/features', icon: 'mdi-flag-outline' },
  { label: 'Segments', route: '/segments', icon: 'mdi-account-group-outline' },
  { label: 'Identities', route: '/identities', icon: 'mdi-badge-account-outline' },
  { label: 'Audit Logs', route: '/audit-logs', icon: 'mdi-history' },
];

const routeTitleMap: Record<string, string> = {
  '/': 'Dashboard',
  '/features': 'Features',
  '/segments': 'Segments',
  '/identities': 'Identities',
  '/audit-logs': 'Audit Logs',
  '/settings': 'Settings',
  '/profile': 'Profile',
};

const pageTitle = computed(() => routeTitleMap[route.path] ?? 'MicCheck');
</script>
