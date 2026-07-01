<template>
  <v-row>
    <v-col cols="12">
      <h1 class="text-h4 mb-4">Environments</h1>
      <v-card>
        <v-card-text>
          Configure deployment environments such as production, staging, and development.
        </v-card-text>
      </v-card>
    </v-col>
  </v-row>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
export default defineComponent({ name: 'EnvironmentsView' });
</script>
