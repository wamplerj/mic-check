<template>
  <div>
    <!-- Loading -->
    <div v-if="isLoading" class="d-flex justify-center align-center py-16">
      <v-progress-circular indeterminate color="primary" />
    </div>

    <!-- Not found -->
    <v-card v-else-if="!user" variant="outlined" rounded="lg">
      <v-card-text class="text-center py-10">
        <v-icon size="48" color="medium-emphasis" class="mb-3">ri-user-line</v-icon>
        <p class="text-h6 mb-2">User not found</p>
        <p class="text-body-2 text-medium-emphasis">No user exists with ID {{ userId }}.</p>
        <v-btn class="mt-4" variant="text" color="primary" :to="{ name: 'AuditLogs' }">
          Back to Audit Logs
        </v-btn>
      </v-card-text>
    </v-card>

    <template v-else>
      <div class="d-flex align-center mb-6 ga-3">
        <v-btn icon="ri-arrow-left-line" variant="text" size="small" @click="$router.back()" />
        <h1 class="text-h5 font-weight-bold">User Profile</h1>
      </div>

      <v-row>
        <!-- Avatar + name card -->
        <v-col cols="12" md="4">
          <v-card rounded="lg" class="text-center pa-6">
            <v-avatar size="96" class="mb-4">
              <v-img v-if="gravatarUrl" :src="gravatarUrl" :alt="displayName" />
              <v-icon v-else size="48" color="medium-emphasis">ri-user-line</v-icon>
            </v-avatar>
            <h2 class="text-h6 font-weight-semibold">{{ displayName }}</h2>
            <p class="text-body-2 text-medium-emphasis mt-1">{{ user.email }}</p>
          </v-card>
        </v-col>

        <!-- Details card -->
        <v-col cols="12" md="8">
          <v-card rounded="lg">
            <v-card-title class="pa-5 pb-3 text-body-1 font-weight-semibold">Account Details</v-card-title>
            <v-divider />
            <v-list lines="two" density="compact">
              <v-list-item>
                <template #prepend>
                  <v-icon color="medium-emphasis" class="me-3">ri-user-line</v-icon>
                </template>
                <v-list-item-title class="text-caption text-medium-emphasis">First Name</v-list-item-title>
                <v-list-item-subtitle class="text-body-2">{{ user.firstName }}</v-list-item-subtitle>
              </v-list-item>
              <v-divider />
              <v-list-item>
                <template #prepend>
                  <v-icon color="medium-emphasis" class="me-3">ri-user-line</v-icon>
                </template>
                <v-list-item-title class="text-caption text-medium-emphasis">Last Name</v-list-item-title>
                <v-list-item-subtitle class="text-body-2">{{ user.lastName }}</v-list-item-subtitle>
              </v-list-item>
              <v-divider />
              <v-list-item>
                <template #prepend>
                  <v-icon color="medium-emphasis" class="me-3">ri-mail-line</v-icon>
                </template>
                <v-list-item-title class="text-caption text-medium-emphasis">Email</v-list-item-title>
                <v-list-item-subtitle class="text-body-2">{{ user.email }}</v-list-item-subtitle>
              </v-list-item>
              <v-divider />
              <v-list-item>
                <template #prepend>
                  <v-icon color="medium-emphasis" class="me-3">ri-calendar-line</v-icon>
                </template>
                <v-list-item-title class="text-caption text-medium-emphasis">Member Since</v-list-item-title>
                <v-list-item-subtitle class="text-body-2">{{ formatDate(user.createdAt) }}</v-list-item-subtitle>
              </v-list-item>
              <v-divider />
              <v-list-item>
                <template #prepend>
                  <v-icon color="medium-emphasis" class="me-3">ri-time-line</v-icon>
                </template>
                <v-list-item-title class="text-caption text-medium-emphasis">Last Login</v-list-item-title>
                <v-list-item-subtitle class="text-body-2">
                  {{ user.lastLoginAt ? formatDate(user.lastLoginAt) : 'Never' }}
                </v-list-item-subtitle>
              </v-list-item>
            </v-list>
          </v-card>
        </v-col>
      </v-row>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { getUserById } from '@/api/users';
import type { UserProfileResponse } from '@/types/api';

const route = useRoute();
const userId = Number(route.params.id);

const user = ref<UserProfileResponse | null>(null);
const isLoading = ref(false);
const gravatarUrl = ref<string | null>(null);

const displayName = computed(() =>
  user.value ? `${user.value.firstName} ${user.value.lastName}` : '',
);

async function computeGravatar(email: string): Promise<string> {
  const encoder = new TextEncoder();
  const data = encoder.encode(email.trim().toLowerCase());
  const hashBuffer = await crypto.subtle.digest('SHA-256', data);
  const hashArray = Array.from(new Uint8Array(hashBuffer));
  const hash = hashArray.map((b) => b.toString(16).padStart(2, '0')).join('');
  return `https://www.gravatar.com/avatar/${hash}?d=mp&s=96`;
}

onMounted(async () => {
  isLoading.value = true;
  try {
    user.value = await getUserById(userId);
    gravatarUrl.value = await computeGravatar(user.value.email);
  } catch {
    user.value = null;
  } finally {
    isLoading.value = false;
  }
});

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    year: 'numeric', month: 'long', day: 'numeric',
  });
}
</script>
