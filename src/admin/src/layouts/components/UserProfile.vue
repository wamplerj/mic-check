<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/auth';

const authStore = useAuthStore();
const router = useRouter();

const gravatarHash = ref('');

async function computeGravatarHash(email: string): Promise<void> {
  const normalized = email.trim().toLowerCase();
  const encoded = new TextEncoder().encode(normalized);
  const hashBuffer = await crypto.subtle.digest('SHA-256', encoded);
  const hashArray = Array.from(new Uint8Array(hashBuffer));
  gravatarHash.value = hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
}

watch(() => authStore.email, email => {
  if (email) computeGravatarHash(email);
}, { immediate: true });

const gravatarUrl = computed(() =>
  gravatarHash.value
    ? `https://www.gravatar.com/avatar/${gravatarHash.value}?s=80&d=mp`
    : 'https://www.gravatar.com/avatar/?s=80&d=mp'
);

const isLoggingOut = ref(false);

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

<template>
  <VBadge
    dot
    location="bottom right"
    offset-x="3"
    offset-y="3"
    color="success"
    bordered
  >
    <VAvatar
      class="cursor-pointer"
      color="primary"
      variant="tonal"
      size="38"
    >
      <VImg :src="gravatarUrl" :alt="authStore.displayName" referrerpolicy="no-referrer" />

      <VMenu activator="parent" width="230" location="bottom end" offset="14px">
        <VList>
          <VListItem>
            <template #prepend>
              <VListItemAction start>
                <VBadge dot location="bottom right" offset-x="3" offset-y="3" color="success">
                  <VAvatar color="primary" variant="tonal" size="38">
                    <VImg :src="gravatarUrl" :alt="authStore.displayName" referrerpolicy="no-referrer" />
                  </VAvatar>
                </VBadge>
              </VListItemAction>
            </template>
            <VListItemTitle class="font-weight-semibold">
              {{ authStore.displayName }}
            </VListItemTitle>
            <VListItemSubtitle>{{ authStore.email }}</VListItemSubtitle>
          </VListItem>

          <VDivider class="my-2" />

          <VListItem :to="{ path: '/profile' }">
            <template #prepend>
              <VIcon class="me-2" icon="ri-user-line" size="22" />
            </template>
            <VListItemTitle>Profile</VListItemTitle>
          </VListItem>

          <VListItem :to="{ path: '/settings' }">
            <template #prepend>
              <VIcon class="me-2" icon="ri-settings-4-line" size="22" />
            </template>
            <VListItemTitle>Settings</VListItemTitle>
          </VListItem>

          <VDivider class="my-2" />

          <VListItem :disabled="isLoggingOut" @click="onLogout">
            <template #prepend>
              <VIcon class="me-2" icon="ri-logout-box-r-line" size="22" />
            </template>
            <VListItemTitle>Sign out</VListItemTitle>
          </VListItem>
        </VList>
      </VMenu>
    </VAvatar>
  </VBadge>
</template>
