<template>
  <div>
    <h1 class="text-h5 font-weight-bold mb-6">Profile</h1>

    <v-card rounded="lg" max-width="480" data-testid="profile-card">
      <v-card-text class="pa-6">
        <div class="d-flex align-center ga-4 mb-6">
          <v-avatar color="primary" size="56">
            <v-icon size="32">mdi-account</v-icon>
          </v-avatar>
          <div>
            <p class="text-body-1 font-weight-medium mb-0" data-testid="profile-email">
              {{ authStore.accessToken ? 'Authenticated user' : 'Not signed in' }}
            </p>
            <p class="text-caption text-medium-emphasis mb-0">
              Token expires: {{ expiresDisplay }}
            </p>
          </div>
        </div>

        <v-divider class="mb-4" />

        <div class="d-flex align-center justify-space-between mb-2" v-if="contextStore.currentOrganization">
          <div>
            <p class="text-body-2 font-weight-medium mb-0">Organization</p>
            <p class="text-caption text-medium-emphasis mb-0" data-testid="profile-org-name">
              {{ contextStore.currentOrganization?.name }}
            </p>
          </div>
        </div>

        <v-divider class="my-4" />

        <v-btn
          color="error"
          variant="outlined"
          prepend-icon="mdi-logout"
          block
          :loading="isLoggingOut"
          data-testid="logout-btn"
          @click="onLogout"
        >
          Sign out
        </v-btn>
      </v-card-text>
    </v-card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
import { useContextStore } from '@/stores/context';

const authStore = useAuthStore();
const contextStore = useContextStore();
const router = useRouter();

const isLoggingOut = ref(false);

const expiresDisplay = computed(() => {
  if (!authStore.expiresAt) return 'Unknown';
  return new Date(authStore.expiresAt).toLocaleString(undefined, {
    month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit',
  });
});

async function onLogout(): Promise<void> {
  isLoggingOut.value = true;
  try {
    await authStore.logout();
    await router.push({ name: 'Login' });
  } finally {
    isLoggingOut.value = false;
  }
}
</script>
