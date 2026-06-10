import { ref, computed, watch } from 'vue';
import { defineStore } from 'pinia';

type ThemeMode = 'light' | 'dark' | 'system';

const STORAGE_KEY = 'mic_theme';

export const useThemeStore = defineStore('theme', () => {
  const mode = ref<ThemeMode>((localStorage.getItem(STORAGE_KEY) as ThemeMode) ?? 'system');

  const systemDark = ref(window.matchMedia('(prefers-color-scheme: dark)').matches);

  const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
  mediaQuery.addEventListener('change', (e) => {
    systemDark.value = e.matches;
  });

  const effectiveTheme = computed<'light' | 'dark'>(() => {
    if (mode.value === 'system') return systemDark.value ? 'dark' : 'light';
    return mode.value;
  });

  function setMode(newMode: ThemeMode): void {
    mode.value = newMode;
    localStorage.setItem(STORAGE_KEY, newMode);
  }

  return { mode, effectiveTheme, setMode };
});
