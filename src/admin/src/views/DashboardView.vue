<template>
  <v-row>
    <v-col cols="12">
      <h1 class="text-h4 mb-4">Dashboard</h1>
      <v-card>
        <v-card-text>
          Welcome to MicCheck. Use the navigation to manage your features,
          environments, and projects.
        </v-card-text>
      </v-card>
    </v-col>
  </v-row>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
export default defineComponent({ name: 'DashboardView' });
</script>
