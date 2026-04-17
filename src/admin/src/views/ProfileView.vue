<template>
  <div>
    <h1 class="text-h5 font-weight-bold mb-6">Profile</h1>

    <v-row>
      <v-col cols="12" md="6">
        <!-- Profile Info Card -->
        <v-card rounded="lg" class="mb-4" data-testid="profile-card">
          <v-card-text class="pa-6">
            <div class="d-flex align-center ga-4 mb-6">
              <v-avatar size="56">
                <img :src="gravatarUrl" :alt="authStore.displayName" referrerpolicy="no-referrer" />
              </v-avatar>
              <div>
                <p class="text-body-1 font-weight-medium mb-0" data-testid="profile-display-name">
                  {{ authStore.displayName }}
                </p>
                <p class="text-caption text-medium-emphasis mb-0">
                  Token expires: {{ expiresDisplay }}
                </p>
              </div>
            </div>

            <v-divider class="mb-5" />

            <v-form ref="profileFormRef" @submit.prevent="onSaveProfile">
              <v-row dense>
                <v-col cols="6">
                  <v-text-field
                    v-model="profileForm.firstName"
                    label="First name"
                    variant="outlined"
                    density="compact"
                    :rules="[required]"
                    data-testid="first-name-input"
                  />
                </v-col>
                <v-col cols="6">
                  <v-text-field
                    v-model="profileForm.lastName"
                    label="Last name"
                    variant="outlined"
                    density="compact"
                    :rules="[required]"
                    data-testid="last-name-input"
                  />
                </v-col>
              </v-row>

              <v-text-field
                v-model="profileForm.email"
                label="Email address"
                type="email"
                variant="outlined"
                density="compact"
                :rules="[required]"
                :error-messages="profileError ? [profileError] : []"
                class="mb-2"
                data-testid="email-input"
              />

              <v-btn
                type="submit"
                color="primary"
                variant="flat"
                block
                :loading="isSavingProfile"
                data-testid="save-profile-btn"
              >
                Save changes
              </v-btn>

              <v-alert
                v-if="profileSuccess"
                type="success"
                variant="tonal"
                density="compact"
                class="mt-3"
                data-testid="profile-success-alert"
              >
                Profile updated.
              </v-alert>
            </v-form>
          </v-card-text>
        </v-card>

        <!-- Organization Card -->
        <v-card rounded="lg" class="mb-4">
          <v-card-text class="pa-6">
            <p class="text-body-2 font-weight-medium mb-3">Organization</p>
            <OrgSelector />
          </v-card-text>
        </v-card>

        <!-- Sign Out Card -->
        <v-card rounded="lg">
          <v-card-text class="pa-6">
            <v-btn
              color="error"
              variant="outlined"
              prepend-icon="ri-logout-box-line"
              block
              :loading="isLoggingOut"
              data-testid="logout-btn"
              @click="onLogout"
            >
              Sign out
            </v-btn>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" md="6">
        <!-- Change Password Card -->
        <v-card rounded="lg" data-testid="change-password-card">
          <v-card-text class="pa-6">
            <p class="text-body-1 font-weight-medium mb-5">Change password</p>

            <v-form ref="passwordFormRef" @submit.prevent="onChangePassword">
              <v-text-field
                v-model="passwordForm.currentPassword"
                label="Current password"
                :type="showCurrent ? 'text' : 'password'"
                variant="outlined"
                density="compact"
                :append-inner-icon="showCurrent ? 'ri-eye-off-line' : 'ri-eye-line'"
                :rules="[required]"
                class="mb-2"
                data-testid="current-password-input"
                @click:append-inner="showCurrent = !showCurrent"
              />

              <v-text-field
                v-model="passwordForm.newPassword"
                label="New password"
                :type="showNew ? 'text' : 'password'"
                variant="outlined"
                density="compact"
                :append-inner-icon="showNew ? 'ri-eye-off-line' : 'ri-eye-line'"
                :rules="[required, minLength]"
                class="mb-2"
                data-testid="new-password-input"
                @click:append-inner="showNew = !showNew"
              />

              <v-text-field
                v-model="passwordForm.confirmPassword"
                label="Confirm new password"
                :type="showConfirm ? 'text' : 'password'"
                variant="outlined"
                density="compact"
                :append-inner-icon="showConfirm ? 'ri-eye-off-line' : 'ri-eye-line'"
                :rules="[required, passwordsMatch]"
                class="mb-2"
                data-testid="confirm-password-input"
                @click:append-inner="showConfirm = !showConfirm"
              />

              <v-btn
                type="submit"
                color="primary"
                variant="flat"
                block
                :loading="isChangingPassword"
                data-testid="change-password-btn"
              >
                Change password
              </v-btn>

              <v-alert
                v-if="passwordError"
                type="error"
                variant="tonal"
                density="compact"
                class="mt-3"
                data-testid="password-error-alert"
              >
                {{ passwordError }}
              </v-alert>

              <v-alert
                v-if="passwordSuccess"
                type="success"
                variant="tonal"
                density="compact"
                class="mt-3"
                data-testid="password-success-alert"
              >
                Password changed.
              </v-alert>
            </v-form>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
import OrgSelector from '@/components/nav/OrgSelector.vue';
import { getMe, updateMe, changePassword } from '@/api/users';

const authStore = useAuthStore();
const router = useRouter();

// ─── Gravatar ───────────────────────────────────────────────────────────────

const gravatarHash = ref('');

async function computeGravatarHash(email: string): Promise<void> {
  const normalized = email.trim().toLowerCase();
  const encoded = new TextEncoder().encode(normalized);
  const hashBuffer = await crypto.subtle.digest('SHA-256', encoded);
  const hashArray = Array.from(new Uint8Array(hashBuffer));
  gravatarHash.value = hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
}

const gravatarUrl = computed(() =>
  gravatarHash.value
    ? `https://www.gravatar.com/avatar/${gravatarHash.value}?s=112&d=mp`
    : 'https://www.gravatar.com/avatar/?s=112&d=mp'
);

watch(() => authStore.email, (email) => {
  if (email) computeGravatarHash(email);
}, { immediate: true });

// ─── Token expiry display ────────────────────────────────────────────────────

const expiresDisplay = computed(() => {
  if (!authStore.expiresAt) return 'Unknown';
  return new Date(authStore.expiresAt).toLocaleString(undefined, {
    month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit',
  });
});

// ─── Profile form ────────────────────────────────────────────────────────────

const profileFormRef = ref();
const isSavingProfile = ref(false);
const profileError = ref('');
const profileSuccess = ref(false);

const profileForm = reactive({
  firstName: authStore.firstName,
  lastName: authStore.lastName,
  email: authStore.email,
});

onMounted(async () => {
  try {
    const user = await getMe();
    profileForm.firstName = user.firstName;
    profileForm.lastName = user.lastName;
    profileForm.email = user.email;
  } catch {
    // Falls back to JWT-decoded values already in the form
  }
});

async function onSaveProfile(): Promise<void> {
  const { valid } = await profileFormRef.value.validate();
  if (!valid) return;

  isSavingProfile.value = true;
  profileError.value = '';
  profileSuccess.value = false;
  try {
    const updated = await updateMe({
      firstName: profileForm.firstName,
      lastName: profileForm.lastName,
      email: profileForm.email,
    });
    authStore.updateProfile(updated.firstName, updated.lastName, updated.email);
    profileSuccess.value = true;
  } catch (err: unknown) {
    const status = (err as { response?: { status?: number } })?.response?.status;
    profileError.value = status === 409 ? 'Email is already in use.' : 'Failed to save. Please try again.';
  } finally {
    isSavingProfile.value = false;
  }
}

// ─── Password form ───────────────────────────────────────────────────────────

const passwordFormRef = ref();
const isChangingPassword = ref(false);
const passwordError = ref('');
const passwordSuccess = ref(false);
const showCurrent = ref(false);
const showNew = ref(false);
const showConfirm = ref(false);

const passwordForm = reactive({
  currentPassword: '',
  newPassword: '',
  confirmPassword: '',
});

const required = (v: string) => !!v || 'Required.';
const minLength = (v: string) => v.length >= 8 || 'Minimum 8 characters.';
const passwordsMatch = (v: string) => v === passwordForm.newPassword || 'Passwords do not match.';

async function onChangePassword(): Promise<void> {
  const { valid } = await passwordFormRef.value.validate();
  if (!valid) return;

  isChangingPassword.value = true;
  passwordError.value = '';
  passwordSuccess.value = false;
  try {
    await changePassword({
      currentPassword: passwordForm.currentPassword,
      newPassword: passwordForm.newPassword,
    });
    passwordSuccess.value = true;
    passwordForm.currentPassword = '';
    passwordForm.newPassword = '';
    passwordForm.confirmPassword = '';
    passwordFormRef.value.resetValidation();
  } catch (err: unknown) {
    const status = (err as { response?: { status?: number } })?.response?.status;
    passwordError.value = status === 400 ? 'Current password is incorrect.' : 'Failed to change password. Please try again.';
  } finally {
    isChangingPassword.value = false;
  }
}

// ─── Logout ──────────────────────────────────────────────────────────────────

const isLoggingOut = ref(false);

async function onLogout(): Promise<void> {
  isLoggingOut.value = true;
  try {
    await authStore.logout();
    await router.push({ name: 'Login' });
  } finally {
    isLoggingOut.value = false;
  }
}
</script>
