import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { mount, flushPromises, type VueWrapper } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { createRouter, createMemoryHistory } from 'vue-router';
import SegmentsView from '@/views/SegmentsView.vue';
import { useContextStore } from '@/stores/context';
import type { ProjectResponse, SegmentResponse } from '@/types/api';

jest.mock('@/api/segments');
jest.mock('@/api/client', () => ({
  setAuthTokens: jest.fn(),
  clearAuthTokens: jest.fn(),
  getAccessToken: jest.fn(),
}));

import * as segmentsApi from '@/api/segments';

const mockProject: ProjectResponse = {
  id: 10, name: 'Website', organizationId: 1, hideDisabledFlags: false, createdAt: '2026-01-01T00:00:00Z',
};

const dialogStub = { template: '<div><slot /></div>' };

const mockSegments: SegmentResponse[] = [
  {
    id: 1, name: 'beta_users', projectId: 10, createdAt: '2026-01-01T00:00:00Z',
    rules: [{ type: 'All', conditions: [{ property: 'plan', operator: 'Equal', value: 'beta' }] }],
  },
  {
    id: 2, name: 'power_users', projectId: 10, createdAt: '2026-01-01T00:00:00Z',
    rules: [],
  },
];

function mountView() {
  const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/', component: { template: '<div />' } }] });
  return mount(SegmentsView, { global: { plugins: [router], stubs: { 'v-dialog': dialogStub } } });
}

describe('SegmentsView', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    jest.clearAllMocks();
  });

  describe('WhenNoProjectIsSelected', () => {
    it('ThenEmptyStateIsShown', () => {
      const wrapper = mountView();
      expect(wrapper.find('[data-testid="segments-table"]').exists()).toBe(false);
      expect(wrapper.text()).toContain('Select a project');
    });

    it('ThenCreateButtonIsNotVisible', () => {
      const wrapper = mountView();
      expect(wrapper.find('[data-testid="create-segment-btn"]').exists()).toBe(false);
    });
  });

  describe('WhenProjectIsSelected', () => {
    it('ThenSegmentsAreLoaded', async () => {
      jest.mocked(segmentsApi.listSegments).mockResolvedValue(mockSegments);

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '', isPrimary: false });
      contextStore.setProject(mockProject);

      mountView();
      await flushPromises();

      expect(segmentsApi.listSegments).toHaveBeenCalledWith(mockProject.id);
    });

    it('ThenSegmentsTableIsRendered', async () => {
      jest.mocked(segmentsApi.listSegments).mockResolvedValue(mockSegments);

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '', isPrimary: false });
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      expect(wrapper.find('[data-testid="segments-table"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="create-segment-btn"]').exists()).toBe(true);
    });

    it('ThenSegmentNamesAreDisplayed', async () => {
      jest.mocked(segmentsApi.listSegments).mockResolvedValue(mockSegments);

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '', isPrimary: false });
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      expect(wrapper.text()).toContain('beta_users');
      expect(wrapper.text()).toContain('power_users');
    });
  });

  describe('WhenCreateButtonIsClicked', () => {
    it('ThenSegmentEditorIsOpened', async () => {
      jest.mocked(segmentsApi.listSegments).mockResolvedValue([]);

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '', isPrimary: false });
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      await wrapper.find('[data-testid="create-segment-btn"]').trigger('click');
      await wrapper.vm.$nextTick();

      const editor = wrapper.findComponent({ name: 'SegmentEditor' });
      expect(editor.exists()).toBe(true);
    });
  });

  describe('WhenEditButtonIsClicked', () => {
    it('ThenSegmentEditorIsOpenedWithSegment', async () => {
      jest.mocked(segmentsApi.listSegments).mockResolvedValue(mockSegments);

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '', isPrimary: false });
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      await wrapper.find('[data-testid="edit-segment-1"]').trigger('click');
      await wrapper.vm.$nextTick();

      const editor = wrapper.findComponent({ name: 'SegmentEditor' });
      expect(editor.props('segment')).toEqual(mockSegments[0]);
    });
  });

  describe('WhenDeleteConfirmationIsCompleted', () => {
    it('ThenDeleteSegmentIsCalledWhenNameMatches', async () => {
      jest.mocked(segmentsApi.listSegments).mockResolvedValue([...mockSegments]);
      jest.mocked(segmentsApi.deleteSegment).mockResolvedValue(undefined);

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '', isPrimary: false });
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      await wrapper.find('[data-testid="delete-segment-1"]').trigger('click');
      await wrapper.vm.$nextTick();

      expect(wrapper.find('[data-testid="delete-confirm-dialog"]').exists()).toBe(true);

      // Confirm button should be disabled before typing the name
      expect(wrapper.find('[data-testid="confirm-delete-btn"]').attributes('disabled')).toBeDefined();

      // Type segment name to enable button
      await (wrapper.findComponent('[data-testid="delete-confirm-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'beta_users');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="confirm-delete-btn"]').trigger('click');
      await flushPromises();

      expect(segmentsApi.deleteSegment).toHaveBeenCalledWith(mockProject.id, 1);
    });

    it('ThenSegmentIsRemovedFromListAfterDeletion', async () => {
      jest.mocked(segmentsApi.listSegments).mockResolvedValue([...mockSegments]);
      jest.mocked(segmentsApi.deleteSegment).mockResolvedValue(undefined);

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '', isPrimary: false });
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      await wrapper.find('[data-testid="delete-segment-1"]').trigger('click');
      await wrapper.vm.$nextTick();

      await (wrapper.findComponent('[data-testid="delete-confirm-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'beta_users');
      await wrapper.vm.$nextTick();

      await wrapper.find('[data-testid="confirm-delete-btn"]').trigger('click');
      await flushPromises();

      expect(wrapper.text()).not.toContain('beta_users');
    });
  });

  describe('WhenSegmentIsSaved', () => {
    it('ThenNewSegmentIsAddedToList', async () => {
      jest.mocked(segmentsApi.listSegments).mockResolvedValue([]);

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '', isPrimary: false });
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      const newSegment: SegmentResponse = { id: 99, name: 'new_segment', projectId: 10, createdAt: '2026-01-01T00:00:00Z', rules: [] };

      await wrapper.findComponent({ name: 'SegmentEditor' }).vm.$emit('saved', newSegment);
      await wrapper.vm.$nextTick();

      expect(wrapper.text()).toContain('new_segment');
    });

    it('ThenExistingSegmentIsUpdatedInList', async () => {
      jest.mocked(segmentsApi.listSegments).mockResolvedValue([...mockSegments]);

      const contextStore = useContextStore();
      contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '', isPrimary: false });
      contextStore.setProject(mockProject);

      const wrapper = mountView();
      await flushPromises();

      const updated: SegmentResponse = { ...mockSegments[0], name: 'updated_segment' };

      await wrapper.findComponent({ name: 'SegmentEditor' }).vm.$emit('saved', updated);
      await wrapper.vm.$nextTick();

      expect(wrapper.text()).toContain('updated_segment');
      expect(wrapper.text()).not.toContain('beta_users');
    });
  });
});
