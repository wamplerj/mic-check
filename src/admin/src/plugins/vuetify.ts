import 'vuetify/styles';
import { createVuetify } from 'vuetify';
import { aliases, mdi } from 'vuetify/iconsets/mdi';

export default createVuetify({
  icons: {
    defaultSet: 'mdi',
    aliases,
    sets: { mdi },
  },
  theme: {
    defaultTheme: 'light',
    themes: {
      light: {
        colors: {
          primary: '#1565C0',
          secondary: '#546E7A',
          accent: '#00ACC1',
          error: '#D32F2F',
          info: '#0288D1',
          success: '#388E3C',
          warning: '#F57C00',
          background: '#F5F7FA',
          surface: '#FFFFFF',
        },
      },
    },
  },
});
