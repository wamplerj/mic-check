<template>
  <v-app-bar
    color="primary"
    elevation="2"
    height="64"
  >
    <template #prepend>
      <v-app-bar-title class="d-flex align-center">
        <img
          :src="logoUrl"
          alt="MicCheck logo"
          height="40"
          width="40"
          class="mr-3"
          data-testid="app-logo"
        />
        <span class="text-h6 font-weight-bold text-white">MicCheck</span>
      </v-app-bar-title>
    </template>

    <!-- Center: Main navigation links (hidden on xs/sm) -->
    <div class="d-none d-md-flex align-center ga-2 mx-auto">
      <v-btn
        v-for="link in navLinks"
        :key="link.route"
        :to="link.route"
        variant="text"
        color="white"
        rounded="lg"
        :data-testid="`nav-link-${link.label.toLowerCase()}`"
      >
        {{ link.label }}
      </v-btn>
    </div>

    <template #append>
      <div class="d-flex align-center ga-2 mr-2">
        <v-btn
          to="/profile"
          variant="text"
          color="white"
          prepend-icon="mdi-account-circle"
          data-testid="profile-link"
          rounded="lg"
        >
          John Doe
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
</template>

<script lang="ts">
import { defineComponent } from 'vue';
import logoUrl from '@/assets/logo.svg';

export default defineComponent({
  name: 'AppHeader',
  setup() {
    const navLinks = [
      { label: 'Features', route: '/features' },
      { label: 'Environments', route: '/environments' },
      { label: 'Projects', route: '/projects' },
    ];

    return { navLinks, logoUrl };
  },
});
</script>
