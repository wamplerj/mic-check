import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { mount, flushPromises, type VueWrapper } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { createRouter, createMemoryHistory } from 'vue-router';
import TagsChipInput from '@/components/features/TagsChipInput.vue';
import { useContextStore } from '@/stores/context';
import type { ProjectResponse, TagResponse } from '@/types/api';

jest.mock('@/api/tags');
jest.mock('@/api/client', () => ({
  setAuthTokens: jest.fn(),
  clearAuthTokens: jest.fn(),
  getAccessToken: jest.fn(),
}));

import * as tagsApi from '@/api/tags';

const mockProject: ProjectResponse = {
  id: 10, name: 'Website', organizationId: 1, hideDisabledFlags: false, createdAt: '2026-01-01T00:00:00Z',
};
const mockTags: TagResponse[] = [
  { id: 1, label: 'backend', color: '#ff0000', projectId: 10 },
  { id: 2, label: 'frontend', color: '#0000ff', projectId: 10 },
];

function mountComponent() {
  const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/', component: { template: '<div />' } }] });
  return mount(TagsChipInput, { global: { plugins: [router] } });
}

describe('TagsChipInput', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    jest.clearAllMocks();
    const contextStore = useContextStore();
    contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '' });
    contextStore.setProject(mockProject);
  });

  describe('WhenComponentMounts', () => {
    it('ThenTagsAreFetchedFromApi', async () => {
      jest.mocked(tagsApi.listTags).mockResolvedValue(mockTags);
      mountComponent();
      await flushPromises();
      expect(tagsApi.listTags).toHaveBeenCalledWith(mockProject.id);
    });

    it('ThenExistingTagChipsAreRendered', async () => {
      jest.mocked(tagsApi.listTags).mockResolvedValue(mockTags);
      const wrapper = mountComponent();
      await flushPromises();

      expect(wrapper.find('[data-testid="tag-chip-backend"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="tag-chip-frontend"]').exists()).toBe(true);
    });

    it('ThenEmptyMessageIsShownWhenNoTagsExist', async () => {
      jest.mocked(tagsApi.listTags).mockResolvedValue([]);
      const wrapper = mountComponent();
      await flushPromises();

      expect(wrapper.find('[data-testid="tags-empty-message"]').exists()).toBe(true);
    });
  });

  describe('WhenCreateTagIsSubmitted', () => {
    it('ThenCreateTagApiIsCalled', async () => {
      const newTag: TagResponse = { id: 3, label: 'api', color: '#00ff00', projectId: 10 };
      jest.mocked(tagsApi.listTags).mockResolvedValue(mockTags);
      jest.mocked(tagsApi.createTag).mockResolvedValue(newTag);

      const wrapper = mountComponent();
      await flushPromises();

      // Open the create form
      await wrapper.find('[data-testid="create-tag-btn"]').trigger('click');
      await wrapper.vm.$nextTick();

      // Fill in the label
      await (wrapper.findComponent('[data-testid="new-tag-label-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'api');

      // Submit
      await wrapper.find('[data-testid="confirm-create-tag-btn"]').trigger('click');
      await flushPromises();

      expect(tagsApi.createTag).toHaveBeenCalledWith(
        mockProject.id,
        expect.objectContaining({ label: 'api' }),
      );
    });

    it('ThenNewTagAppearsInTheList', async () => {
      const newTag: TagResponse = { id: 3, label: 'api', color: '#00ff00', projectId: 10 };
      jest.mocked(tagsApi.listTags).mockResolvedValue([]);
      jest.mocked(tagsApi.createTag).mockResolvedValue(newTag);

      const wrapper = mountComponent();
      await flushPromises();

      await wrapper.find('[data-testid="create-tag-btn"]').trigger('click');
      await wrapper.vm.$nextTick();
      await (wrapper.findComponent('[data-testid="new-tag-label-input"]') as VueWrapper<any>).vm.$emit('update:modelValue', 'api');
      await wrapper.find('[data-testid="confirm-create-tag-btn"]').trigger('click');
      await flushPromises();

      expect(wrapper.find('[data-testid="tag-chip-api"]').exists()).toBe(true);
    });
  });

  describe('WhenTagChipIsDeleted', () => {
    it('ThenDeleteTagApiIsCalled', async () => {
      jest.mocked(tagsApi.listTags).mockResolvedValue(mockTags);
      jest.mocked(tagsApi.deleteTag).mockResolvedValue(undefined);

      const wrapper = mountComponent();
      await flushPromises();

      const chip = wrapper.findComponent('[data-testid="tag-chip-backend"]') as VueWrapper<any>;
      await chip.vm.$emit('click:close');
      await flushPromises();

      expect(tagsApi.deleteTag).toHaveBeenCalledWith(mockProject.id, mockTags[0].id);
    });

    it('ThenDeletedTagIsRemovedFromTheList', async () => {
      jest.mocked(tagsApi.listTags).mockResolvedValue(mockTags);
      jest.mocked(tagsApi.deleteTag).mockResolvedValue(undefined);

      const wrapper = mountComponent();
      await flushPromises();

      const chip = wrapper.findComponent('[data-testid="tag-chip-backend"]') as VueWrapper<any>;
      await chip.vm.$emit('click:close');
      await flushPromises();

      expect(wrapper.find('[data-testid="tag-chip-backend"]').exists()).toBe(false);
    });
  });
});
