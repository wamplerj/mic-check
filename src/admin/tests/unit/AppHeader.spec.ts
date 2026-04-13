import { describe, it, expect, beforeEach } from '@jest/globals';
import { mount, VueWrapper } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import AppHeader from '@/components/AppHeader.vue';

function makeRouter() {
  return createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: { template: '<div />' } },
      { path: '/profile', component: { template: '<div />' } },
      { path: '/settings', component: { template: '<div />' } },
    ],
  });
}

describe('AppHeader', () => {
  let wrapper: VueWrapper;

  beforeEach(async () => {
    const router = makeRouter();
    await router.push('/');

    wrapper = mount(
      { template: '<v-app><AppHeader /></v-app>', components: { AppHeader } },
      { global: { plugins: [router] } },
    );
  });

  it('renders the SVG microphone logo image', () => {
    const logo = wrapper.find('[data-testid="app-logo"]');
    expect(logo.exists()).toBe(true);
    expect(logo.attributes('alt')).toBe('MicCheck logo');
  });

  it('renders the profile link', () => {
    const link = wrapper.find('[data-testid="profile-link"]');
    expect(link.exists()).toBe(true);
  });

  it('renders the settings gear icon link', () => {
    const link = wrapper.find('[data-testid="settings-link"]');
    expect(link.exists()).toBe(true);
  });
});
