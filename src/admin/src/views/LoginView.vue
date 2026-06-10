<template>
  <v-container fluid class="fill-height auth-bg">
    <v-row justify="center" align="center" class="fill-height">
      <v-col cols="12" sm="8" md="5" lg="4" xl="3">
        <div class="d-flex justify-center mb-6">
          <img :src="logoUrl" alt="MicCheck logo" height="52" width="52" class="mr-3" />
          <span class="text-h4 font-weight-bold align-self-center">MicCheck</span>
        </div>

        <v-card elevation="4" rounded="lg">
          <v-card-title class="pa-6 pb-2 text-h6">Sign in</v-card-title>
          <v-card-text class="pa-6 pt-2">
            <v-alert
              v-if="errorMessage"
              type="error"
              variant="tonal"
              class="mb-4"
              data-testid="login-error"
            >
              {{ errorMessage }}
            </v-alert>

            <v-form ref="formRef" @submit.prevent="onSubmit" data-testid="login-form">
              <v-text-field
                v-model="email"
                label="Email"
                type="email"
                autocomplete="email"
                variant="outlined"
                density="comfortable"
                class="mb-3"
                :rules="[rules.required, rules.email]"
                data-testid="email-input"
              />
              <v-text-field
                v-model="password"
                label="Password"
                :type="showPassword ? 'text' : 'password'"
                autocomplete="current-password"
                variant="outlined"
                density="comfortable"
                class="mb-4"
                :rules="[rules.required]"
                :append-inner-icon="showPassword ? 'ri-eye-off-line' : 'ri-eye-line'"
                @click:append-inner="showPassword = !showPassword"
                data-testid="password-input"
              />
              <v-btn
                type="submit"
                color="primary"
                size="large"
                block
                :loading="isLoading"
                data-testid="login-submit"
              >
                Sign in
              </v-btn>
            </v-form>
          </v-card-text>
          <v-divider />
          <v-card-text class="pa-4 text-center">
            <span class="text-body-2 text-medium-emphasis">Don't have an account?</span>
            <v-btn
              variant="text"
              color="primary"
              size="small"
              to="/register"
              class="ml-1"
              data-testid="register-link"
            >
              Register
            </v-btn>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
import logoUrl from '@/assets/logo.svg';

const router = useRouter();
const authStore = useAuthStore();

const formRef = ref<{ validate: () => Promise<{ valid: boolean }> } | null>(null);
const email = ref('');
const password = ref('');
const showPassword = ref(false);
const isLoading = ref(false);
const errorMessage = ref('');

const rules = {
  required: (v: string) => !!v || 'This field is required',
  email: (v: string) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v) || 'Enter a valid email address',
};

async function onSubmit(): Promise<void> {
  errorMessage.value = '';
  const form = formRef.value;
  if (!form) return;

  const { valid } = await form.validate();
  if (!valid) return;

  isLoading.value = true;
  try {
    await authStore.login(email.value, password.value);
    await router.push('/');
  } catch {
    errorMessage.value = 'Invalid email or password. Please try again.';
  } finally {
    isLoading.value = false;
  }
}
</script>

<style scoped>
.auth-bg {
  background: rgb(var(--v-theme-background));
}
</style>
