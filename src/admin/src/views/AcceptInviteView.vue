<template>
  <div class="d-flex align-center justify-center" style="min-height: 100vh">
    <v-card rounded="lg" max-width="400" width="100%" class="ma-4">
      <v-card-text class="pa-8 text-center">
        <v-progress-circular v-if="state === 'loading'" indeterminate color="primary" class="mb-4" />

        <template v-else-if="state === 'success'">
          <v-icon color="success" size="48" class="mb-4">ri-check-line</v-icon>
          <h2 class="text-h6 font-weight-bold mb-2">You're in!</h2>
          <p class="text-body-2 text-medium-emphasis mb-6">You have successfully joined the organization.</p>
          <v-btn color="primary" variant="flat" @click="$router.push('/dashboard')">Go to Dashboard</v-btn>
        </template>

        <template v-else-if="state === 'not-authenticated'">
          <v-icon color="info" size="48" class="mb-4">ri-login-box-line</v-icon>
          <h2 class="text-h6 font-weight-bold mb-2">Sign in to accept invite</h2>
          <p class="text-body-2 text-medium-emphasis mb-6">You need to be logged in to accept this invitation.</p>
          <v-btn color="primary" variant="flat" @click="$router.push({ name: 'Login', query: { redirect: $route.fullPath } })">Sign In</v-btn>
        </template>

        <template v-else>
          <v-icon color="error" size="48" class="mb-4">ri-error-warning-line</v-icon>
          <h2 class="text-h6 font-weight-bold mb-2">Invalid invite link</h2>
          <p class="text-body-2 text-medium-emphasis mb-6">This invite link is invalid or has expired.</p>
          <v-btn color="primary" variant="flat" @click="$router.push('/dashboard')">Go to Dashboard</v-btn>
        </template>
      </v-card-text>
    </v-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { acceptInvite } from '@/api/organizations';
import { getAccessToken } from '@/api/client';

type State = 'loading' | 'success' | 'error' | 'not-authenticated';

const route = useRoute();
const state = ref<State>('loading');

onMounted(async () => {
  const token = route.params.token as string;

  if (!getAccessToken()) {
    state.value = 'not-authenticated';
    return;
  }

  try {
    await acceptInvite(token);
    state.value = 'success';
  } catch {
    state.value = 'error';
  }
});
</script>
