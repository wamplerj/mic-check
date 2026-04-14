import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { mount, flushPromises, type VueWrapper } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { createRouter, createMemoryHistory } from 'vue-router';
import WebhooksPanel from '@/components/settings/WebhooksPanel.vue';
import type { WebhookResponse } from '@/types/api';

jest.mock('@/api/webhooks');
jest.mock('@/api/client', () => ({
  setAuthTokens: jest.fn(),
  clearAuthTokens: jest.fn(),
  getAccessToken: jest.fn(),
}));

import * as webhooksApi from '@/api/webhooks';

const mockOrgWebhooks: WebhookResponse[] = [
  {
    id: 1, url: 'https://example.com/hook1', secret: null, scope: 'Organization',
    enabled: true, environmentId: null, organizationId: 1, createdAt: '2026-01-01T00:00:00Z',
  },
  {
    id: 2, url: 'https://example.com/hook2', secret: 'abc', scope: 'Organization',
    enabled: false, environmentId: null, organizationId: 1, createdAt: '2026-01-02T00:00:00Z',
  },
];

const dialogStub = { template: '<div><slot /></div>' };

function mountPanel(props: Record<string, unknown> = {}) {
  const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/', component: { template: '<div />' } }] });
  return mount(WebhooksPanel, {
    props: { orgId: 1, ...props },
    global: { plugins: [router], stubs: { 'v-dialog': dialogStub } },
  });
}

describe('WebhooksPanel', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    jest.clearAllMocks();
  });

  describe('WhenMountedWithOrgScope', () => {
    it('ThenOrgWebhooksAreLoaded', async () => {
      jest.mocked(webhooksApi.listOrgWebhooks).mockResolvedValue(mockOrgWebhooks);

      mountPanel();
      await flushPromises();

      expect(webhooksApi.listOrgWebhooks).toHaveBeenCalledWith(1);
    });

    it('ThenWebhooksTableIsRendered', async () => {
      jest.mocked(webhooksApi.listOrgWebhooks).mockResolvedValue(mockOrgWebhooks);

      const wrapper = mountPanel();
      await flushPromises();

      expect(wrapper.find('[data-testid="webhooks-table"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('https://example.com/hook1');
    });

    it('ThenNoWebhooksMessageIsShownWhenEmpty', async () => {
      jest.mocked(webhooksApi.listOrgWebhooks).mockResolvedValue([]);

      const wrapper = mountPanel();
      await flushPromises();

      expect(wrapper.find('[data-testid="no-webhooks-message"]').exists()).toBe(true);
    });
  });

  describe('WhenMountedWithEnvScope', () => {
    it('ThenEnvWebhooksAreLoaded', async () => {
      jest.mocked(webhooksApi.listEnvWebhooks).mockResolvedValue([]);

      mountPanel({ orgId: undefined, envApiKey: 'env-dev' });
      await flushPromises();

      expect(webhooksApi.listEnvWebhooks).toHaveBeenCalledWith('env-dev');
    });
  });

  describe('WhenEnabledToggleIsChanged', () => {
    it('ThenUpdateOrgWebhookIsCalledWithToggledEnabled', async () => {
      jest.mocked(webhooksApi.listOrgWebhooks).mockResolvedValue(mockOrgWebhooks);
      jest.mocked(webhooksApi.updateOrgWebhook).mockResolvedValue({ ...mockOrgWebhooks[0], enabled: false });

      const wrapper = mountPanel();
      await flushPromises();

      await (wrapper.findComponent(`[data-testid="webhook-enabled-toggle-1"]`) as VueWrapper<any>).vm.$emit('update:modelValue', false);
      await flushPromises();

      expect(webhooksApi.updateOrgWebhook).toHaveBeenCalledWith(
        1, 1,
        expect.objectContaining({ enabled: false }),
      );
    });
  });

  describe('WhenDeleteIsConfirmed', () => {
    it('ThenDeleteOrgWebhookIsCalledAndWebhookIsRemoved', async () => {
      jest.mocked(webhooksApi.listOrgWebhooks).mockResolvedValue([...mockOrgWebhooks]);
      jest.mocked(webhooksApi.deleteOrgWebhook).mockResolvedValue(undefined);

      const wrapper = mountPanel();
      await flushPromises();

      await wrapper.find('[data-testid="delete-webhook-1"]').trigger('click');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="confirm-delete-webhook-btn"]').trigger('click');
      await flushPromises();

      expect(webhooksApi.deleteOrgWebhook).toHaveBeenCalledWith(1, 1);
      expect(wrapper.text()).not.toContain('https://example.com/hook1');
    });
  });

  describe('WhenWebhookIsSaved', () => {
    it('ThenNewWebhookIsAddedToList', async () => {
      jest.mocked(webhooksApi.listOrgWebhooks).mockResolvedValue([]);

      const wrapper = mountPanel();
      await flushPromises();

      const newHook: WebhookResponse = { ...mockOrgWebhooks[0], id: 99, url: 'https://new.example.com/hook' };
      await wrapper.findComponent({ name: 'WebhookDialog' }).vm.$emit('saved', newHook);
      await wrapper.vm.$nextTick();

      expect(wrapper.text()).toContain('https://new.example.com/hook');
    });

    it('ThenExistingWebhookIsUpdatedInList', async () => {
      jest.mocked(webhooksApi.listOrgWebhooks).mockResolvedValue([...mockOrgWebhooks]);

      const wrapper = mountPanel();
      await flushPromises();

      const updated: WebhookResponse = { ...mockOrgWebhooks[0], url: 'https://updated.example.com/hook' };
      await wrapper.findComponent({ name: 'WebhookDialog' }).vm.$emit('saved', updated);
      await wrapper.vm.$nextTick();

      expect(wrapper.text()).toContain('https://updated.example.com/hook');
    });
  });
});
