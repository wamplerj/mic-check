import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { mount, flushPromises, DOMWrapper, type VueWrapper } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { createRouter, createMemoryHistory } from 'vue-router';
import TagsChipInput from '@/components/features/TagsChipInput.vue';
import { useContextStore } from '@/stores/context';
import type { ProjectResponse, TagResponse, FeatureResponse } from '@/types/api';

jest.mock('@/api/tags');
jest.mock('@/api/features');
jest.mock('@/api/client', () => ({
  setAuthTokens: jest.fn(),
  clearAuthTokens: jest.fn(),
  getAccessToken: jest.fn(),
}));

import * as tagsApi from '@/api/tags';
import * as featuresApi from '@/api/features';

const mockProject: ProjectResponse = {
  id: 10, name: 'Website', organizationId: 1, hideDisabledFlags: false, createdAt: '2026-01-01T00:00:00Z',
};
const backendTag: TagResponse = { id: 1, label: 'backend', color: '#ff0000', projectId: 10 };
const frontendTag: TagResponse = { id: 2, label: 'frontend', color: '#0000ff', projectId: 10 };
const mockTags: TagResponse[] = [backendTag, frontendTag];

function mockFeature(tags: TagResponse[]): FeatureResponse {
  return {
    id: 1, name: 'dark_mode', type: 'STANDARD', initialValue: null, description: null,
    defaultEnabled: false, projectId: 10, createdAt: '2026-01-01T00:00:00Z', tags,
  };
}

function mountComponent(feature: FeatureResponse) {
  const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/', component: { template: '<div />' } }] });
  return mount(TagsChipInput, { props: { feature }, global: { plugins: [router] } });
}

describe('TagsChipInput', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    jest.clearAllMocks();
    const contextStore = useContextStore();
    contextStore.setOrganization({ id: 1, name: 'Acme', createdAt: '', isPrimary: false });
    contextStore.setProject(mockProject);
  });

  describe('WhenComponentMounts', () => {
    it('ThenTagsAreFetchedFromApi', async () => {
      jest.mocked(tagsApi.listTags).mockResolvedValue(mockTags);
      mountComponent(mockFeature([]));
      await flushPromises();
      expect(tagsApi.listTags).toHaveBeenCalledWith(mockProject.id);
    });

    it('ThenAssignedTagChipsAreRendered', async () => {
      jest.mocked(tagsApi.listTags).mockResolvedValue(mockTags);
      const wrapper = mountComponent(mockFeature([backendTag]));
      await flushPromises();

      expect(wrapper.find('[data-testid="tag-chip-backend"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="tag-chip-frontend"]').exists()).toBe(false);
    });

    it('ThenEmptyMessageIsShownWhenNoTagsAssigned', async () => {
      jest.mocked(tagsApi.listTags).mockResolvedValue(mockTags);
      const wrapper = mountComponent(mockFeature([]));
      await flushPromises();

      expect(wrapper.find('[data-testid="tags-empty-message"]').exists()).toBe(true);
    });
  });

  describe('WhenAddingANewTagName', () => {
    it('ThenCreateTagAndAssignAreBothCalled', async () => {
      const newTag: TagResponse = { id: 3, label: 'api', color: '#00ff00', projectId: 10 };
      const updatedFeature = mockFeature([newTag]);
      jest.mocked(tagsApi.listTags).mockResolvedValue(mockTags);
      jest.mocked(tagsApi.createTag).mockResolvedValue(newTag);
      jest.mocked(featuresApi.assignFeatureTag).mockResolvedValue(updatedFeature);

      const wrapper = mountComponent(mockFeature([]));
      await flushPromises();

      const input = wrapper.findComponent('[data-testid="tag-name-input"]') as VueWrapper<any>;
      await input.vm.$emit('update:modelValue', 'api');
      await wrapper.find('[data-testid="add-tag-btn"]').trigger('click');
      await flushPromises();

      expect(tagsApi.createTag).toHaveBeenCalledWith(mockProject.id, expect.objectContaining({ label: 'api' }));
      expect(featuresApi.assignFeatureTag).toHaveBeenCalledWith(mockProject.id, 1, newTag.id);
    });

    it('ThenUpdatedEventIsEmitted', async () => {
      const newTag: TagResponse = { id: 3, label: 'api', color: '#00ff00', projectId: 10 };
      const updatedFeature = mockFeature([newTag]);
      jest.mocked(tagsApi.listTags).mockResolvedValue(mockTags);
      jest.mocked(tagsApi.createTag).mockResolvedValue(newTag);
      jest.mocked(featuresApi.assignFeatureTag).mockResolvedValue(updatedFeature);

      const wrapper = mountComponent(mockFeature([]));
      await flushPromises();

      const input = wrapper.findComponent('[data-testid="tag-name-input"]') as VueWrapper<any>;
      await input.vm.$emit('update:modelValue', 'api');
      await wrapper.find('[data-testid="add-tag-btn"]').trigger('click');
      await flushPromises();

      expect(wrapper.emitted('updated')?.[0]).toEqual([updatedFeature]);
    });
  });

  describe('WhenTypedNameMatchesAnExistingTag', () => {
    it('ThenExistingTagIsAssignedInsteadOfCreatingANewOne', async () => {
      const updatedFeature = mockFeature([backendTag]);
      jest.mocked(tagsApi.listTags).mockResolvedValue(mockTags);
      jest.mocked(featuresApi.assignFeatureTag).mockResolvedValue(updatedFeature);

      const wrapper = mountComponent(mockFeature([]));
      await flushPromises();

      const input = wrapper.findComponent('[data-testid="tag-name-input"]') as VueWrapper<any>;
      await input.vm.$emit('update:modelValue', 'backend');
      await wrapper.find('[data-testid="add-tag-btn"]').trigger('click');
      await flushPromises();

      expect(tagsApi.createTag).not.toHaveBeenCalled();
      expect(featuresApi.assignFeatureTag).toHaveBeenCalledWith(mockProject.id, 1, backendTag.id);
    });
  });

  describe('WhenTypingTwoOrMoreCharacters', () => {
    it('ThenMatchingUnassignedTagsAreSuggested', async () => {
      jest.mocked(tagsApi.listTags).mockResolvedValue(mockTags);

      const wrapper = mountComponent(mockFeature([]));
      await flushPromises();

      const input = wrapper.findComponent('[data-testid="tag-name-input"]') as VueWrapper<any>;
      await input.vm.$emit('update:modelValue', 'ba');
      await wrapper.vm.$nextTick();

      expect(input.props('items')).toEqual(['backend']);
    });

    it('ThenNoSuggestionsAreShownBelowTwoCharacters', async () => {
      jest.mocked(tagsApi.listTags).mockResolvedValue(mockTags);

      const wrapper = mountComponent(mockFeature([]));
      await flushPromises();

      const input = wrapper.findComponent('[data-testid="tag-name-input"]') as VueWrapper<any>;
      await input.vm.$emit('update:modelValue', 'b');
      await wrapper.vm.$nextTick();

      expect(input.props('items')).toEqual([]);
    });
  });

  describe('WhenChoosingATagColour', () => {
    it('ThenSwatchClickOpensTheColourDialog', async () => {
      jest.mocked(tagsApi.listTags).mockResolvedValue(mockTags);
      const wrapper = mountComponent(mockFeature([]));
      await flushPromises();

      expect(wrapper.find('[data-testid="color-dialog"]').exists()).toBe(false);
      await wrapper.find('[data-testid="tag-color-swatch"]').trigger('click');
      await flushPromises();

      expect(wrapper.findComponent({ name: 'VDialog' }).props('modelValue')).toBe(true);
    });

    it('ThenFifteenPresetColoursAreOffered', async () => {
      jest.mocked(tagsApi.listTags).mockResolvedValue(mockTags);
      const wrapper = mountComponent(mockFeature([]));
      await flushPromises();

      await wrapper.find('[data-testid="tag-color-swatch"]').trigger('click');
      await flushPromises();

      const presets = new DOMWrapper(document.body).findAll('[data-testid^="preset-color-"]');
      const distinctColors = new Set(presets.map((p) => p.attributes('data-testid')));
      expect(distinctColors.size).toBe(15);
    });

    it('ThenSelectingAPresetUpdatesTheSwatchAndClosesTheDialog', async () => {
      jest.mocked(tagsApi.listTags).mockResolvedValue(mockTags);
      const wrapper = mountComponent(mockFeature([]));
      await flushPromises();

      await wrapper.find('[data-testid="tag-color-swatch"]').trigger('click');
      await flushPromises();

      const presets = new DOMWrapper(document.body).findAll('[data-testid="preset-color-#4CAF50"]');
      await presets[presets.length - 1].trigger('click');
      await wrapper.vm.$nextTick();
      await flushPromises();

      expect(wrapper.find('[data-testid="tag-color-swatch"]').attributes('style')).toContain('background: rgb(76, 175, 80)');
    });
  });

  describe('WhenTagChipIsClosed', () => {
    it('ThenRemoveFeatureTagApiIsCalled', async () => {
      const updatedFeature = mockFeature([]);
      jest.mocked(tagsApi.listTags).mockResolvedValue(mockTags);
      jest.mocked(featuresApi.removeFeatureTag).mockResolvedValue(updatedFeature);

      const wrapper = mountComponent(mockFeature([backendTag]));
      await flushPromises();

      const chip = wrapper.findComponent('[data-testid="tag-chip-backend"]') as VueWrapper<any>;
      await chip.vm.$emit('click:close');
      await flushPromises();

      expect(featuresApi.removeFeatureTag).toHaveBeenCalledWith(mockProject.id, 1, backendTag.id);
      expect(wrapper.emitted('updated')?.[0]).toEqual([updatedFeature]);
    });
  });
});
