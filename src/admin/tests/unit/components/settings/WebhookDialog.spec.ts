import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { mount, flushPromises, type VueWrapper } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { createRouter, createMemoryHistory } from 'vue-router';
import WebhookDialog from '@/components/settings/WebhookDialog.vue';
import type { WebhookResponse } from '@/types/api';

jest.mock('@/api/webhooks');
jest.mock('@/api/client', () => ({
  setAuthTokens: jest.fn(),
  clearAuthTokens: jest.fn(),
  getAccessToken: jest.fn(),
}));

import * as webhooksApi from '@/api/webhooks';

const mockWebhook: WebhookResponse = {
  id: 1, url: 'https://example.com/hook', secret: 'mysecret', scope: 'Organization',
  enabled: true, environmentId: null, organizationId: 1, createdAt: '2026-01-01T00:00:00Z',
};

const dialogStub = { template: '<div><slot /></div>' };

function mountDialog(props: Record<string, unknown> = {}) {
  const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/', component: { template: '<div />' } }] });
  return mount(WebhookDialog, {
    props: { modelValue: true, orgId: 1, ...props },
    global: { plugins: [router], stubs: { 'v-dialog': dialogStub } },
  });
}

describe('WebhookDialog', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    jest.clearAllMocks();
  });

  describe('WhenOpenedForCreate', () => {
    it('ThenDialogIsRendered', () => {
      const wrapper = mountDialog();
      expect(wrapper.find('[data-testid="webhook-dialog"]').exists()).toBe(true);
    });

    it('ThenTitleShowsAddWebhook', () => {
      const wrapper = mountDialog();
      expect(wrapper.text()).toContain('Add Webhook');
    });

    it('ThenCreateOrgWebhookIsCalledOnSave', async () => {
      const created = { ...mockWebhook, id: 99 };
      jest.mocked(webhooksApi.createOrgWebhook).mockResolvedValue(created);

      const wrapper = mountDialog();

      await (wrapper.findComponent('[data-testid="webhook-url-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'https://example.com/new');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="save-webhook-btn"]').trigger('click');
      await flushPromises();

      expect(webhooksApi.createOrgWebhook).toHaveBeenCalledWith(
        1,
        expect.objectContaining({ url: 'https://example.com/new' }),
      );
    });

    it('ThenSavedEventIsEmitted', async () => {
      const created = { ...mockWebhook, id: 99 };
      jest.mocked(webhooksApi.createOrgWebhook).mockResolvedValue(created);

      const wrapper = mountDialog();

      await (wrapper.findComponent('[data-testid="webhook-url-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'https://example.com/new');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="save-webhook-btn"]').trigger('click');
      await flushPromises();

      expect(wrapper.emitted('saved')).toBeTruthy();
      expect(wrapper.emitted('saved')![0]).toEqual([created]);
    });
  });

  describe('WhenOpenedForEdit', () => {
    it('ThenTitleShowsEditWebhook', () => {
      const wrapper = mountDialog({ webhook: mockWebhook });
      expect(wrapper.text()).toContain('Edit Webhook');
    });

    it('ThenFormIsPrePopulatedWithWebhookData', () => {
      const wrapper = mountDialog({ webhook: mockWebhook });
      const urlInput = wrapper.findComponent('[data-testid="webhook-url-input"]') as VueWrapper<any>;
      expect(urlInput.props('modelValue')).toBe(mockWebhook.url);
    });

    it('ThenUpdateOrgWebhookIsCalledOnSave', async () => {
      const updated = { ...mockWebhook, url: 'https://example.com/updated' };
      jest.mocked(webhooksApi.updateOrgWebhook).mockResolvedValue(updated);

      const wrapper = mountDialog({ webhook: mockWebhook });

      await wrapper.find('[data-testid="save-webhook-btn"]').trigger('click');
      await flushPromises();

      expect(webhooksApi.updateOrgWebhook).toHaveBeenCalledWith(
        1, mockWebhook.id,
        expect.objectContaining({ url: mockWebhook.url }),
      );
    });
  });

  describe('WhenEnvScopedDialogIsUsed', () => {
    it('ThenCreateEnvWebhookIsCalledOnSave', async () => {
      const created = { ...mockWebhook, id: 99, scope: 'Environment' as const };
      jest.mocked(webhooksApi.createEnvWebhook).mockResolvedValue(created);

      const wrapper = mountDialog({ orgId: undefined, envApiKey: 'env-dev' });

      await (wrapper.findComponent('[data-testid="webhook-url-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'https://example.com/new');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="save-webhook-btn"]').trigger('click');
      await flushPromises();

      expect(webhooksApi.createEnvWebhook).toHaveBeenCalledWith(
        'env-dev',
        expect.objectContaining({ url: 'https://example.com/new' }),
      );
    });
  });
});
