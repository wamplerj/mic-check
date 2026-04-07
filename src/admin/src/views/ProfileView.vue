<template>
  <v-row>
    <v-col cols="12">
      <h1 class="text-h4 mb-4">Profile</h1>
      <v-card>
        <v-card-text>
          Manage your account details and preferences. Logged in as John Doe.
        </v-card-text>
      </v-card>
    </v-col>
  </v-row>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
export default defineComponent({ name: 'ProfileView' });
</script>
