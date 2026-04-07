import { describe, it, expect, beforeEach } from '@jest/globals';
import { mount, VueWrapper } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import AppHeader from '@/components/AppHeader.vue';

function makeRouter() {
  return createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: { template: '<div />' } },
      { path: '/features', component: { template: '<div />' } },
      { path: '/environments', component: { template: '<div />' } },
      { path: '/projects', component: { template: '<div />' } },
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

    // v-app-bar requires a v-app layout context — wrap the component under test
    wrapper = mount(
      { template: '<v-app><AppHeader /></v-app>', components: { AppHeader } },
      { global: { plugins: [router] } },
    );
  });

  it('renders the Features navigation link', () => {
    const link = wrapper.find('[data-testid="nav-link-features"]');
    expect(link.exists()).toBe(true);
    expect(link.text()).toBe('Features');
  });

  it('renders the Environments navigation link', () => {
    const link = wrapper.find('[data-testid="nav-link-environments"]');
    expect(link.exists()).toBe(true);
    expect(link.text()).toBe('Environments');
  });

  it('renders the Projects navigation link', () => {
    const link = wrapper.find('[data-testid="nav-link-projects"]');
    expect(link.exists()).toBe(true);
    expect(link.text()).toBe('Projects');
  });

  it('renders the SVG microphone logo image', () => {
    const logo = wrapper.find('[data-testid="app-logo"]');
    expect(logo.exists()).toBe(true);
    expect(logo.attributes('alt')).toBe('MicCheck logo');
  });

  it('renders the profile link with John Doe text', () => {
    const link = wrapper.find('[data-testid="profile-link"]');
    expect(link.exists()).toBe(true);
    expect(link.text()).toContain('John Doe');
  });

  it('renders the settings gear icon link', () => {
    const link = wrapper.find('[data-testid="settings-link"]');
    expect(link.exists()).toBe(true);
  });
});
