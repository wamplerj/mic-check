<template>
  <apexchart type="bar" :options="chartOptions" :series="series" height="280" />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { DailyUsage } from '@/types/api';

const props = defineProps<{
  data: DailyUsage[];
}>();

const series = computed(() => [
  {
    name: 'Evaluations',
    data: props.data.map((d) => d.totalCount),
  },
]);

function formatDate(iso: string): string {
  const parts = iso.split('-');
  return parts.length === 3 ? `${parts[1]}/${parts[2]}` : iso;
}

const chartOptions = computed(() => {
  const dailyData = props.data;
  return {
    chart: {
      type: 'bar',
      toolbar: { show: false },
      animations: { enabled: false },
    },
    plotOptions: {
      bar: {
        columnWidth: '55%',
        borderRadius: 4,
      },
    },
    dataLabels: { enabled: false },
    xaxis: {
      categories: dailyData.map((d) => d.date),
      labels: {
        style: { colors: '#8A8D93', fontSize: '12px' },
        formatter: (val: string) => formatDate(val),
      },
      axisBorder: { show: false },
      axisTicks: { show: false },
    },
    yaxis: {
      labels: { style: { colors: '#8A8D93', fontSize: '12px' } },
    },
    colors: ['#8C57FF'],
    grid: { borderColor: '#E0E0E0', strokeDashArray: 4 },
    tooltip: {
      custom: ({ dataPointIndex }: { dataPointIndex: number }) => {
        const day = dailyData[dataPointIndex];
        if (!day) return '';
        const flagRows = day.features
          .slice(0, 10)
          .map(
            (f) =>
              `<div class="apex-day-tooltip__row"><span>${f.featureName}</span><strong>${f.count.toLocaleString()}</strong></div>`,
          )
          .join('');
        const moreCount = day.features.length - 10;
        const moreRow = moreCount > 0 ? `<div class="apex-day-tooltip__more">+${moreCount} more</div>` : '';
        return `
          <div class="apex-day-tooltip">
            <div class="apex-day-tooltip__header">${day.date} &mdash; ${day.totalCount.toLocaleString()} total</div>
            ${flagRows}${moreRow}
          </div>`;
      },
    },
  };
});
</script>

<style>
.apex-day-tooltip {
  padding: 10px 14px;
  font-family: inherit;
  min-width: 200px;
  max-width: 300px;
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.apex-day-tooltip__header {
  font-weight: 600;
  margin-bottom: 8px;
  font-size: 0.82rem;
  color: #2e263d;
  border-bottom: 1px solid #f0f0f0;
  padding-bottom: 6px;
}

.apex-day-tooltip__row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 16px;
  font-size: 0.8rem;
  padding: 2px 0;
  color: #555;
}

.apex-day-tooltip__row strong {
  color: #8c57ff;
  font-weight: 600;
}

.apex-day-tooltip__more {
  font-size: 0.75rem;
  color: #9e9e9e;
  margin-top: 4px;
}
</style>
