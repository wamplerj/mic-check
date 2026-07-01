import type { App } from 'vue';
import { createVuetify } from 'vuetify';
import { VBtn } from 'vuetify/components/VBtn';
import defaults from './defaults';
import { icons } from './icons';
import { themes } from './theme';

import '@core/scss/template/libs/vuetify/index.scss';
import 'vuetify/styles';

function resolveDefaultTheme(): 'light' | 'dark' {
  const stored = localStorage.getItem('mic_theme');
  if (stored === 'dark') return 'dark';
  if (stored === 'system' || stored === null) {
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }
  return 'light';
}

export default function installVuetify(app: App): void {
  const vuetify = createVuetify({
    aliases: {
      IconBtn: VBtn,
    },
    defaults,
    icons,
    theme: {
      defaultTheme: resolveDefaultTheme(),
      themes,
    },
  });

  app.use(vuetify);
}
