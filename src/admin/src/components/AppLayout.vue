<template>
  <v-app :theme="theme">
    <AppHeader />

    <v-navigation-drawer
      v-model="drawer"
      :rail="rail"
      permanent
      color="white"
    >
      <v-list density="compact" nav>
        <v-list-item
          v-for="link in navLinks"
          :key="link.route"
          :to="link.route"
          :prepend-icon="link.icon"
          :title="link.label"
          active-color="primary"
          rounded="lg"
        />
      </v-list>

      <template #append>
        <v-list density="compact" nav>
          <v-list-item
            :prepend-icon="rail ? 'mdi-chevron-right' : 'mdi-chevron-left'"
            title="Collapse"
            @click="rail = !rail"
          />
        </v-list>
      </template>
    </v-navigation-drawer>

    <v-main>
      <v-container fluid class="pa-6">
        <router-view />
      </v-container>
    </v-main>
  </v-app>
</template>

<script lang="ts">
import { defineComponent, ref } from 'vue';
import AppHeader from '@/components/AppHeader.vue';

export default defineComponent({
  name: 'AppLayout',
  components: { AppHeader },
  setup() {
    const drawer = ref(true);
    const rail = ref(false);
    const theme = ref('light');

    const navLinks = [
      { label: 'Dashboard', route: '/', icon: 'mdi-view-dashboard' },
      { label: 'Features', route: '/features', icon: 'mdi-flag-outline' },
      { label: 'Environments', route: '/environments', icon: 'mdi-server-outline' },
      { label: 'Projects', route: '/projects', icon: 'mdi-folder-outline' },
    ];

    return { drawer, rail, theme, navLinks };
  },
});
</script>
