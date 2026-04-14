import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { mount, flushPromises, type VueWrapper } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { createRouter, createMemoryHistory } from 'vue-router';
import SegmentEditor from '@/components/segments/SegmentEditor.vue';
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

const mockSegment: SegmentResponse = {
  id: 1,
  name: 'beta_users',
  projectId: 10,
  createdAt: '2026-01-01T00:00:00Z',
  rules: [{ type: 'AND', conditions: [{ property: 'plan', operator: 'Equal', value: 'beta' }] }],
};

const dialogStub = { template: '<div><slot /></div>' };

function mountEditor(props: Record<string, unknown> = {}) {
  const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/', component: { template: '<div />' } }] });
  return mount(SegmentEditor, {
    props: { modelValue: true, segment: null, ...props },
    global: { plugins: [router], stubs: { 'v-dialog': dialogStub } },
  });
}

describe('SegmentEditor', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    jest.clearAllMocks();
    const contextStore = useContextStore();
    contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '' });
    contextStore.setProject(mockProject);
  });

  describe('WhenOpenedForCreate', () => {
    it('ThenEditorIsRendered', () => {
      const wrapper = mountEditor();
      expect(wrapper.find('[data-testid="segment-editor"]').exists()).toBe(true);
    });

    it('ThenTitleShowsCreateSegment', () => {
      const wrapper = mountEditor();
      expect(wrapper.text()).toContain('Create Segment');
    });

    it('ThenNameInputIsEmpty', () => {
      const wrapper = mountEditor();
      const nameInput = wrapper.find('[data-testid="segment-name-input"]');
      expect(nameInput.exists()).toBe(true);
    });

    it('ThenNoRulesMessageIsShown', () => {
      const wrapper = mountEditor();
      expect(wrapper.find('[data-testid="no-rules-message"]').exists()).toBe(true);
    });
  });

  describe('WhenOpenedForEdit', () => {
    it('ThenTitleShowsEditSegment', () => {
      const wrapper = mountEditor({ segment: mockSegment });
      expect(wrapper.text()).toContain('Edit Segment');
    });

    it('ThenFormIsPrePopulatedWithSegmentData', () => {
      const wrapper = mountEditor({ segment: mockSegment });
      const nameInput = wrapper.findComponent('[data-testid="segment-name-input"]') as VueWrapper<any>;
      expect(nameInput.props('modelValue')).toBe(mockSegment.name);
    });

    it('ThenExistingRulesAreRendered', () => {
      const wrapper = mountEditor({ segment: mockSegment });
      expect(wrapper.findComponent({ name: 'RuleGroupEditor' }).exists()).toBe(true);
    });
  });

  describe('WhenAddRuleGroupButtonIsClicked', () => {
    it('ThenARuleGroupEditorIsAdded', async () => {
      const wrapper = mountEditor();

      await wrapper.find('[data-testid="add-rule-group-btn"]').trigger('click');
      await wrapper.vm.$nextTick();

      expect(wrapper.findComponent({ name: 'RuleGroupEditor' }).exists()).toBe(true);
    });
  });

  describe('WhenSaveIsClickedWithValidData', () => {
    it('ThenCreateSegmentIsCalledForNewSegment', async () => {
      const created: SegmentResponse = { id: 99, name: 'new_seg', projectId: 10, createdAt: '2026-01-01T00:00:00Z', rules: [] };
      jest.mocked(segmentsApi.createSegment).mockResolvedValue(created);

      const wrapper = mountEditor();

      await (wrapper.findComponent('[data-testid="segment-name-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'new_seg');
      await wrapper.find('[data-testid="save-segment-btn"]').trigger('click');
      await flushPromises();

      expect(segmentsApi.createSegment).toHaveBeenCalledWith(
        mockProject.id,
        expect.objectContaining({ name: 'new_seg' }),
      );
    });

    it('ThenUpdateSegmentIsCalledForExistingSegment', async () => {
      const updated = { ...mockSegment, name: 'updated_name' };
      jest.mocked(segmentsApi.updateSegment).mockResolvedValue(updated);

      const wrapper = mountEditor({ segment: mockSegment });

      await wrapper.find('[data-testid="save-segment-btn"]').trigger('click');
      await flushPromises();

      expect(segmentsApi.updateSegment).toHaveBeenCalledWith(
        mockProject.id,
        mockSegment.id,
        expect.objectContaining({ name: mockSegment.name }),
      );
    });

    it('ThenSavedEventIsEmitted', async () => {
      const created: SegmentResponse = { id: 99, name: 'new_seg', projectId: 10, createdAt: '2026-01-01T00:00:00Z', rules: [] };
      jest.mocked(segmentsApi.createSegment).mockResolvedValue(created);

      const wrapper = mountEditor();

      await (wrapper.findComponent('[data-testid="segment-name-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'new_seg');
      await wrapper.find('[data-testid="save-segment-btn"]').trigger('click');
      await flushPromises();

      expect(wrapper.emitted('saved')).toBeTruthy();
      expect(wrapper.emitted('saved')![0]).toEqual([created]);
    });
  });
});
