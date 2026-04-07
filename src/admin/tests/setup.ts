import { config } from '@vue/test-utils';
import { createVuetify } from 'vuetify';
import * as components from 'vuetify/components';
import * as directives from 'vuetify/directives';

// Stub ResizeObserver which jsdom doesn't implement
global.ResizeObserver = class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
};

// Stub CSS.supports
Object.defineProperty(window, 'CSS', {
  value: { supports: () => false },
  writable: true,
});

const vuetify = createVuetify({ components, directives });

config.global.plugins = [vuetify];
