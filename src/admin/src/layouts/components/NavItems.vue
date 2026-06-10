<script lang="ts" setup>
import VerticalNavLink from '@layouts/components/VerticalNavLink.vue';
import VerticalNavSectionTitle from '@layouts/components/VerticalNavSectionTitle.vue';
import OrgSelector from '@/components/nav/OrgSelector.vue';
import ProjectSelector from '@/components/nav/ProjectSelector.vue';
import EnvironmentTabBar from '@/components/nav/EnvironmentTabBar.vue';
</script>

<template>
  <VerticalNavLink :item="{ title: 'Dashboard', icon: 'ri-dashboard-line', to: '/dashboard' }" />
  <VerticalNavLink :item="{ title: 'Features', icon: 'ri-flag-line', to: '/features' }" />
  <VerticalNavLink :item="{ title: 'Segments', icon: 'ri-group-line', to: '/segments' }" />
  <VerticalNavLink :item="{ title: 'Identities', icon: 'ri-profile-line', to: '/identities' }" />
  <VerticalNavLink :item="{ title: 'Audit Logs', icon: 'ri-history-line', to: '/audit-logs' }" />

  <VerticalNavSectionTitle :item="{ heading: 'Context' }" />
  <div class="nav-context-selectors px-4 d-flex flex-column ga-3">
    <OrgSelector />
    <ProjectSelector />
    <EnvironmentTabBar />
  </div>

  <VerticalNavSectionTitle :item="{ heading: 'Configuration' }" />
  <VerticalNavLink :item="{ title: 'Settings', icon: 'ri-settings-3-line', to: '/settings' }" />
</template>
