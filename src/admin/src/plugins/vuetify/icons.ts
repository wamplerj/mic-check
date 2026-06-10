import { Icon } from '@iconify/vue';
import type { IconAliases, IconProps } from 'vuetify';
import { h } from 'vue';

const aliases: Partial<IconAliases> = {
  info: 'ri-error-warning-line',
  success: 'ri-checkbox-circle-line',
  warning: 'ri-alert-line',
  error: 'ri-error-warning-line',
  calendar: 'ri-calendar-2-line',
  collapse: 'ri-arrow-up-s-line',
  complete: 'ri-check-line',
  cancel: 'ri-close-line',
  close: 'ri-close-line',
  delete: 'ri-close-circle-fill',
  clear: 'ri-close-line',
  prev: 'ri-arrow-left-s-line',
  next: 'ri-arrow-right-s-line',
  delimiter: 'ri-circle-line',
  sort: 'ri-arrow-up-line',
  expand: 'ri-arrow-down-s-line',
  menu: 'ri-menu-line',
  subgroup: 'ri-arrow-down-s-fill',
  dropdown: 'ri-arrow-down-s-line',
  edit: 'ri-pencil-line',
  ratingEmpty: 'ri-star-line',
  ratingFull: 'ri-star-fill',
  ratingHalf: 'ri-star-half-line',
  loading: 'ri-refresh-line',
  first: 'ri-skip-back-mini-line',
  last: 'ri-skip-forward-mini-line',
  unfold: 'ri-split-cells-vertical',
  file: 'ri-attachment-2',
  plus: 'ri-add-line',
  minus: 'ri-subtract-line',
  sortAsc: 'ri-arrow-up-line',
  sortDesc: 'ri-arrow-down-line',
};

export const iconify = {
  component: (props: IconProps) =>
    h(Icon, { icon: props.icon as string, style: 'font-size: inherit; line-height: inherit;' }),
};

export const icons = {
  defaultSet: 'iconify',
  aliases,
  sets: { iconify },
};
