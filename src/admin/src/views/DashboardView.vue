<template>
  <div>
    <h1 class="text-h5 font-weight-bold mb-4">Dashboard</h1>

    <!-- Context summary -->
    <v-row class="mb-4" data-testid="context-summary">
      <v-col cols="12" sm="4">
        <v-card
          :color="contextStore.currentOrganization ? 'primary' : undefined"
          :variant="contextStore.currentOrganization ? 'tonal' : 'outlined'"
          rounded="lg"
          data-testid="org-card"
          class="context-card"
        >
          <v-card-item>
            <template #prepend>
              <v-icon :color="contextStore.currentOrganization ? 'primary' : 'medium-emphasis'">
                ri-building-line
              </v-icon>
            </template>
            <v-card-title class="text-body-2 text-medium-emphasis">Organization</v-card-title>
            <v-card-subtitle class="text-body-1 font-weight-medium mt-1">
              {{ contextStore.currentOrganization?.name ?? 'Not selected' }}
            </v-card-subtitle>
          </v-card-item>
        </v-card>
      </v-col>

      <v-col cols="12" sm="4">
        <v-card
          :color="contextStore.currentProject ? 'primary' : undefined"
          :variant="contextStore.currentProject ? 'tonal' : 'outlined'"
          rounded="lg"
          data-testid="project-card"
          class="context-card"
        >
          <v-card-item>
            <template #prepend>
              <v-icon :color="contextStore.currentProject ? 'primary' : 'medium-emphasis'">
                ri-folder-line
              </v-icon>
            </template>
            <v-card-title class="text-body-2 text-medium-emphasis">Project</v-card-title>
            <v-card-subtitle class="text-body-1 font-weight-medium mt-1">
              {{ contextStore.currentProject?.name ?? 'Not selected' }}
            </v-card-subtitle>
          </v-card-item>
        </v-card>
      </v-col>

      <v-col cols="12" sm="4">
        <v-card
          :color="contextStore.currentEnvironment ? 'primary' : undefined"
          :variant="contextStore.currentEnvironment ? 'tonal' : 'outlined'"
          rounded="lg"
          data-testid="environment-card"
          class="context-card"
        >
          <v-card-item>
            <template #prepend>
              <v-icon :color="contextStore.currentEnvironment ? 'primary' : 'medium-emphasis'">
                ri-server-line
              </v-icon>
            </template>
            <v-card-title class="text-body-2 text-medium-emphasis">Environment</v-card-title>
            <v-card-subtitle class="text-body-1 font-weight-medium mt-1">
              {{ contextStore.currentEnvironment?.name ?? 'Not selected' }}
            </v-card-subtitle>
          </v-card-item>
        </v-card>
      </v-col>
    </v-row>

    <!-- Usage charts (requires environment) -->
    <template v-if="contextStore.currentEnvironment">
      <v-row class="mb-4">
        <!-- Top 10 features last day -->
        <v-col cols="12" md="6">
          <v-card rounded="lg" data-testid="top-features-chart-card">
            <v-card-item class="pb-0">
              <v-card-title class="text-body-1 font-weight-medium">Top Features (Last Day)</v-card-title>
              <v-card-subtitle class="text-caption">Most evaluated flags in the last 24 hours</v-card-subtitle>
            </v-card-item>
            <v-card-text class="pt-2">
              <div v-if="isLoadingUsage" class="d-flex align-center justify-center" style="height:280px">
                <v-progress-circular indeterminate color="primary" />
              </div>
              <div v-else-if="usageError" class="d-flex align-center justify-center text-medium-emphasis" style="height:280px">
                <span class="text-caption">Failed to load usage data</span>
              </div>
              <div v-else-if="!usageData || usageData.topFeaturesLastDay.length === 0" class="d-flex align-center justify-center text-medium-emphasis" style="height:280px">
                <div class="text-center">
                  <v-icon size="36" color="medium-emphasis" class="mb-2">ri-bar-chart-line</v-icon>
                  <p class="text-caption">No evaluations recorded yet</p>
                </div>
              </div>
              <TopFeaturesChart v-else :data="usageData.topFeaturesLastDay" />
            </v-card-text>
          </v-card>
        </v-col>

        <!-- Usage by day -->
        <v-col cols="12" md="6">
          <v-card rounded="lg" data-testid="daily-usage-chart-card">
            <v-card-item class="pb-0">
              <v-card-title class="text-body-1 font-weight-medium">Evaluations by Day</v-card-title>
              <v-card-subtitle class="text-caption">Hover a bar to see per-flag counts · Last 14 days</v-card-subtitle>
            </v-card-item>
            <v-card-text class="pt-2">
              <div v-if="isLoadingUsage" class="d-flex align-center justify-center" style="height:280px">
                <v-progress-circular indeterminate color="primary" />
              </div>
              <div v-else-if="usageError" class="d-flex align-center justify-center text-medium-emphasis" style="height:280px">
                <span class="text-caption">Failed to load usage data</span>
              </div>
              <div v-else-if="!usageData || usageData.dailyUsage.length === 0" class="d-flex align-center justify-center text-medium-emphasis" style="height:280px">
                <div class="text-center">
                  <v-icon size="36" color="medium-emphasis" class="mb-2">ri-bar-chart-2-line</v-icon>
                  <p class="text-caption">No evaluations recorded yet</p>
                </div>
              </div>
              <DailyUsageChart v-else :data="usageData.dailyUsage" />
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>
    </template>

    <!-- Quick-nav cards -->
    <v-row v-if="contextStore.currentProject">
      <v-col
        v-for="section in sections"
        :key="section.route"
        cols="12"
        sm="6"
        md="4"
        lg="3"
      >
        <v-card
          :to="section.route"
          rounded="lg"
          hover
          data-testid="section-card"
          class="section-card"
        >
          <v-card-item>
            <template #prepend>
              <v-icon color="primary" size="28">{{ section.icon }}</v-icon>
            </template>
            <v-card-title class="text-body-1 font-weight-medium">{{ section.label }}</v-card-title>
            <v-card-subtitle class="text-caption">{{ section.description }}</v-card-subtitle>
          </v-card-item>
        </v-card>
      </v-col>
    </v-row>

    <!-- Empty state when no org selected -->
    <v-card v-else-if="!contextStore.currentOrganization" variant="outlined" rounded="lg">
      <v-card-text class="text-center py-10">
        <v-icon size="48" color="medium-emphasis" class="mb-4">ri-building-line</v-icon>
        <p class="text-h6 mb-2">Get started</p>
        <p class="text-body-2 text-medium-emphasis">
          Select or create an organization using the sidebar to begin managing your feature flags.
        </p>
      </v-card-text>
    </v-card>

    <!-- Empty state when org selected but no project -->
    <v-card v-else variant="outlined" rounded="lg">
      <v-card-text class="text-center py-10">
        <v-icon size="48" color="medium-emphasis" class="mb-4">ri-folder-line</v-icon>
        <p class="text-h6 mb-2">Select a project</p>
        <p class="text-body-2 text-medium-emphasis">
          Choose a project from the sidebar to start managing feature flags, segments, and identities.
        </p>
      </v-card-text>
    </v-card>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useContextStore } from '@/stores/context';
import { getUsageDashboard } from '@/api/usage';
import TopFeaturesChart from '@/components/dashboard/TopFeaturesChart.vue';
import DailyUsageChart from '@/components/dashboard/DailyUsageChart.vue';
import type { DashboardUsageResponse } from '@/types/api';

const contextStore = useContextStore();

const usageData = ref<DashboardUsageResponse | null>(null);
const isLoadingUsage = ref(false);
const usageError = ref(false);

async function loadUsage(): Promise<void> {
  const envId = contextStore.currentEnvironment?.id;
  if (!envId) {
    usageData.value = null;
    return;
  }
  isLoadingUsage.value = true;
  usageError.value = false;
  try {
    usageData.value = await getUsageDashboard(envId);
  } catch {
    usageError.value = true;
  } finally {
    isLoadingUsage.value = false;
  }
}

watch(() => contextStore.currentEnvironment, loadUsage, { immediate: true });

const sections = [
  {
    label: 'Features',
    description: 'Manage feature flags and their values per environment',
    route: '/features',
    icon: 'ri-flag-line',
  },
  {
    label: 'Segments',
    description: 'Define user segments to target specific audiences',
    route: '/segments',
    icon: 'ri-group-line',
  },
  {
    label: 'Identities',
    description: 'View and override flags for individual users',
    route: '/identities',
    icon: 'ri-profile-line',
  },
  {
    label: 'Audit Logs',
    description: 'Review a full history of changes across the project',
    route: '/audit-logs',
    icon: 'ri-history-line',
  },
];
</script>

<style scoped>
:deep(.section-card .v-card-item) {
  align-items: flex-start;
}

:deep(.context-card .v-card-item) {
  align-items: flex-start;
}
</style>
