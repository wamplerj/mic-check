import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { setActivePinia, createPinia } from 'pinia';
import { useThemeStore } from '@/stores/theme';

const STORAGE_KEY = 'mic_theme';

function mockMatchMedia(initialMatches: boolean) {
  let changeHandler: ((e: { matches: boolean }) => void) | undefined;
  const mql = {
    matches: initialMatches,
    media: '(prefers-color-scheme: dark)',
    addEventListener: jest.fn((event: string, handler: (e: { matches: boolean }) => void) => {
      if (event === 'change') changeHandler = handler;
    }),
    removeEventListener: jest.fn(),
  };
  window.matchMedia = jest.fn().mockReturnValue(mql) as unknown as typeof window.matchMedia;

  return {
    triggerChange: (matches: boolean) => changeHandler?.({ matches }),
  };
}

describe('ThemeStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    localStorage.clear();
  });

  describe('WhenNoModeIsStoredAndSystemPrefersLight', () => {
    it('ThenModeDefaultsToSystemAndEffectiveThemeIsLight', () => {
      mockMatchMedia(false);

      const themeStore = useThemeStore();

      expect(themeStore.mode).toBe('system');
      expect(themeStore.effectiveTheme).toBe('light');
    });
  });

  describe('WhenNoModeIsStoredAndSystemPrefersDark', () => {
    it('ThenEffectiveThemeIsDark', () => {
      mockMatchMedia(true);

      const themeStore = useThemeStore();

      expect(themeStore.mode).toBe('system');
      expect(themeStore.effectiveTheme).toBe('dark');
    });
  });

  describe('WhenAModeIsAlreadyStored', () => {
    it('ThenTheStoredModeIsUsedAsTheInitialMode', () => {
      localStorage.setItem(STORAGE_KEY, 'dark');
      mockMatchMedia(false);

      const themeStore = useThemeStore();

      expect(themeStore.mode).toBe('dark');
      expect(themeStore.effectiveTheme).toBe('dark');
    });
  });

  describe('WhenSetModeIsCalledWithLight', () => {
    it('ThenModeAndEffectiveThemeAreLightAndPersistedToStorage', () => {
      mockMatchMedia(true);
      const themeStore = useThemeStore();

      themeStore.setMode('light');

      expect(themeStore.mode).toBe('light');
      expect(themeStore.effectiveTheme).toBe('light');
      expect(localStorage.getItem(STORAGE_KEY)).toBe('light');
    });
  });

  describe('WhenSetModeIsCalledWithDark', () => {
    it('ThenModeAndEffectiveThemeAreDarkAndPersistedToStorage', () => {
      mockMatchMedia(false);
      const themeStore = useThemeStore();

      themeStore.setMode('dark');

      expect(themeStore.mode).toBe('dark');
      expect(themeStore.effectiveTheme).toBe('dark');
      expect(localStorage.getItem(STORAGE_KEY)).toBe('dark');
    });
  });

  describe('WhenModeIsSystemAndTheOperatingSystemThemeChanges', () => {
    it('ThenEffectiveThemeReactsToTheChange', () => {
      const { triggerChange } = mockMatchMedia(false);
      const themeStore = useThemeStore();
      expect(themeStore.effectiveTheme).toBe('light');

      triggerChange(true);

      expect(themeStore.effectiveTheme).toBe('dark');
    });
  });

  describe('WhenModeIsExplicitAndTheOperatingSystemThemeChanges', () => {
    it('ThenEffectiveThemeIsUnaffected', () => {
      const { triggerChange } = mockMatchMedia(false);
      const themeStore = useThemeStore();
      themeStore.setMode('light');

      triggerChange(true);

      expect(themeStore.effectiveTheme).toBe('light');
    });
  });
});
