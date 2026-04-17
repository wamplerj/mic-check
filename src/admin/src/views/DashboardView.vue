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
import { useContextStore } from '@/stores/context';

const contextStore = useContextStore();

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
