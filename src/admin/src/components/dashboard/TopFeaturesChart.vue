<template>
  <apexchart type="bar" :options="chartOptions" :series="series" height="280" />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { TopFeatureUsage } from '@/types/api';

const props = defineProps<{
  data: TopFeatureUsage[];
}>();

const series = computed(() => [
  {
    name: 'Evaluations',
    data: props.data.map((f) => f.count),
  },
]);

const chartOptions = computed(() => ({
  chart: {
    type: 'bar',
    toolbar: { show: false },
    animations: { enabled: false },
  },
  plotOptions: {
    bar: {
      horizontal: true,
      borderRadius: 4,
      dataLabels: { position: 'top' },
    },
  },
  dataLabels: { enabled: false },
  xaxis: {
    categories: props.data.map((f) => f.featureName),
    labels: { style: { colors: '#8A8D93', fontSize: '12px' } },
    axisBorder: { show: false },
    axisTicks: { show: false },
  },
  yaxis: {
    labels: { style: { colors: '#8A8D93', fontSize: '12px' } },
  },
  colors: ['#8C57FF'],
  grid: { borderColor: '#E0E0E0', strokeDashArray: 4, yaxis: { lines: { show: false } } },
  tooltip: {
    y: { formatter: (val: number) => `${val.toLocaleString()} evaluations` },
  },
}));
</script>
