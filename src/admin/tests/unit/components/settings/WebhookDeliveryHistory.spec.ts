import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { mount, flushPromises } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { createRouter, createMemoryHistory } from 'vue-router';
import WebhookDeliveryHistory from '@/components/settings/WebhookDeliveryHistory.vue';
import type { WebhookDeliveryLogResponse } from '@/types/api';

jest.mock('@/api/webhooks');
jest.mock('@/api/client', () => ({
  setAuthTokens: jest.fn(),
  clearAuthTokens: jest.fn(),
  getAccessToken: jest.fn(),
}));

import * as webhooksApi from '@/api/webhooks';

const mockDeliveries: WebhookDeliveryLogResponse[] = [
  {
    id: 1, webhookId: 10, eventType: 'feature.updated', success: true,
    responseStatusCode: 200, responseBody: 'OK', errorMessage: null,
    attemptNumber: 1, attemptedAt: '2026-01-10T10:00:00Z', duration: '00:00:00.123',
  },
  {
    id: 2, webhookId: 10, eventType: 'feature.created', success: false,
    responseStatusCode: 500, responseBody: null, errorMessage: 'Internal Server Error',
    attemptNumber: 3, attemptedAt: '2026-01-11T11:00:00Z', duration: '00:00:01.500',
  },
];

function mountComponent(props: Record<string, unknown> = {}) {
  const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/', component: { template: '<div />' } }] });
  return mount(WebhookDeliveryHistory, {
    props: { envApiKey: 'env-dev', webhookId: 10, ...props },
    global: { plugins: [router] },
  });
}

describe('WebhookDeliveryHistory', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    jest.clearAllMocks();
  });

  describe('WhenMounted', () => {
    it('ThenDeliveriesAreLoaded', async () => {
      jest.mocked(webhooksApi.listWebhookDeliveries).mockResolvedValue(mockDeliveries);

      mountComponent();
      await flushPromises();

      expect(webhooksApi.listWebhookDeliveries).toHaveBeenCalledWith('env-dev', 10);
    });

    it('ThenNoDeliveriesMessageIsShownWhenEmpty', async () => {
      jest.mocked(webhooksApi.listWebhookDeliveries).mockResolvedValue([]);

      const wrapper = mountComponent();
      await flushPromises();

      expect(wrapper.find('[data-testid="no-deliveries-message"]').exists()).toBe(true);
    });

    it('ThenDeliveriesTableIsRendered', async () => {
      jest.mocked(webhooksApi.listWebhookDeliveries).mockResolvedValue(mockDeliveries);

      const wrapper = mountComponent();
      await flushPromises();

      expect(wrapper.find('[data-testid="deliveries-table"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="delivery-row-1"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="delivery-row-2"]').exists()).toBe(true);
    });
  });

  describe('WhenRenderingDeliveryStatuses', () => {
    it('ThenSuccessfulDeliveryShowsSuccessChip', async () => {
      jest.mocked(webhooksApi.listWebhookDeliveries).mockResolvedValue(mockDeliveries);

      const wrapper = mountComponent();
      await flushPromises();

      const successChip = wrapper.find('[data-testid="delivery-status-1"]');
      expect(successChip.exists()).toBe(true);
      expect(successChip.text()).toContain('Success');
    });

    it('ThenFailedDeliveryShowsFailedChip', async () => {
      jest.mocked(webhooksApi.listWebhookDeliveries).mockResolvedValue(mockDeliveries);

      const wrapper = mountComponent();
      await flushPromises();

      const failedChip = wrapper.find('[data-testid="delivery-status-2"]');
      expect(failedChip.exists()).toBe(true);
      expect(failedChip.text()).toContain('Failed');
    });

    it('ThenEventTypesAreDisplayed', async () => {
      jest.mocked(webhooksApi.listWebhookDeliveries).mockResolvedValue(mockDeliveries);

      const wrapper = mountComponent();
      await flushPromises();

      expect(wrapper.text()).toContain('feature.updated');
      expect(wrapper.text()).toContain('feature.created');
    });

    it('ThenAttemptNumbersAreDisplayed', async () => {
      jest.mocked(webhooksApi.listWebhookDeliveries).mockResolvedValue(mockDeliveries);

      const wrapper = mountComponent();
      await flushPromises();

      expect(wrapper.text()).toContain('3'); // attemptNumber of second delivery
    });
  });
});
