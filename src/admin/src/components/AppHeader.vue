<template>
  <v-app-bar color="primary" elevation="2" height="56">
    <template #prepend>
      <v-app-bar-title class="d-flex align-center">
        <img
          :src="logoUrl"
          alt="MicCheck logo"
          height="32"
          width="32"
          class="mr-3"
          data-testid="app-logo"
        />
        <span class="text-h6 font-weight-bold text-white">MicCheck</span>
      </v-app-bar-title>
    </template>

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
          Account
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
    return { logoUrl };
  },
});
</script>
