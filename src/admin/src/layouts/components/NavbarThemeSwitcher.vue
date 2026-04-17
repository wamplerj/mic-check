<script setup lang="ts">
import { watch } from 'vue';
import { useTheme } from 'vuetify';
import ThemeSwitcher from '@core/components/ThemeSwitcher.vue';
import type { ThemeSwitcherTheme } from '@layouts/types';

const themes: ThemeSwitcherTheme[] = [
  { name: 'light', icon: 'ri-sun-line' },
  { name: 'dark', icon: 'ri-moon-clear-line' },
];

const { global: globalTheme } = useTheme();

watch(() => globalTheme.name.value, (name) => {
  localStorage.setItem('mic_theme', name);
});
</script>

<template>
  <ThemeSwitcher :themes="themes" />
</template>
