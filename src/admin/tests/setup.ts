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

// Stub visualViewport which jsdom doesn't implement (required by Vuetify VOverlay)
if (!window.visualViewport) {
  Object.defineProperty(window, 'visualViewport', {
    value: {
      width: 1024,
      height: 768,
      offsetLeft: 0,
      offsetTop: 0,
      pageLeft: 0,
      pageTop: 0,
      scale: 1,
      addEventListener: () => {},
      removeEventListener: () => {},
    },
    writable: true,
  });
}

// Stub CSS.supports
Object.defineProperty(window, 'CSS', {
  value: { supports: () => false },
  writable: true,
});

const vuetify = createVuetify({ components, directives });

config.global.plugins = [vuetify];
