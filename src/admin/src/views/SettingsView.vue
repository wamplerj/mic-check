<template>
  <div>
    <h1 class="text-h5 font-weight-bold mb-4">Settings</h1>

    <v-alert
      v-if="!contextStore.currentOrganization"
      type="info"
      variant="tonal"
      density="compact"
    >
      Select an organization to manage settings.
    </v-alert>

    <template v-else>
      <v-tabs v-model="activeTab" color="primary" class="mb-4">
        <v-tab value="organization" data-testid="tab-organization">Organization</v-tab>
        <v-tab value="project" data-testid="tab-project" :disabled="!contextStore.currentProject">Project</v-tab>
        <v-tab value="environments" data-testid="tab-environments" :disabled="!contextStore.currentProject">Environments</v-tab>
        <v-tab value="api-keys" data-testid="tab-api-keys">API Keys</v-tab>
      </v-tabs>

      <v-tabs-window v-model="activeTab">
        <!-- ── Organization tab ───────────────────────────────────────── -->
        <v-tabs-window-item value="organization" class="pt-2">
          <v-alert v-if="orgError" type="error" variant="tonal" density="compact" closable class="mb-4" @click:close="orgError = null">{{ orgError }}</v-alert>
          <v-alert v-if="orgSuccess" type="success" variant="tonal" density="compact" closable class="mb-4" @click:close="orgSuccess = null">{{ orgSuccess }}</v-alert>

          <!-- Org name -->
          <v-card rounded="lg" class="mb-4">
            <v-card-title class="text-body-1 font-weight-medium pa-4 pb-2">Organization Name</v-card-title>
            <v-card-text class="pa-4 pt-0">
              <div class="d-flex align-center ga-3">
                <v-text-field
                  v-model="orgNameInput"
                  variant="outlined"
                  density="compact"
                  hide-details
                  style="max-width: 360px"
                  data-testid="org-name-input"
                />
                <v-btn
                  color="primary"
                  variant="flat"
                  size="small"
                  :loading="isSavingOrg"
                  data-testid="save-org-name-btn"
                  @click="onSaveOrgName"
                >
                  Save
                </v-btn>
              </div>
            </v-card-text>
          </v-card>

          <!-- Org members -->
          <v-card rounded="lg" class="mb-4">
            <v-card-title class="d-flex align-center justify-space-between pa-4 pb-2">
              <span class="text-body-1 font-weight-medium">Members</span>
              <v-btn
                size="small"
                color="primary"
                variant="tonal"
                prepend-icon="ri-user-add-line"
                data-testid="add-member-btn"
                @click="openAddMemberDialog"
              >
                Add Member
              </v-btn>
            </v-card-title>
            <v-card-text class="pa-4 pt-0">
              <!-- Invite link -->
              <div class="d-flex align-center ga-2 mb-4">
                <v-text-field
                  :model-value="inviteLink"
                  label="Invite link"
                  variant="outlined"
                  density="compact"
                  hide-details
                  readonly
                  :loading="isLoadingInviteLink"
                  data-testid="invite-link-input"
                />
                <v-btn
                  :icon="inviteLinkCopied ? 'ri-check-line' : 'ri-file-copy-line'"
                  :color="inviteLinkCopied ? 'success' : undefined"
                  variant="text"
                  size="small"
                  :loading="isRegeneratingLink"
                  data-testid="copy-invite-link-btn"
                  @click="onCopyInviteLink"
                />
              </div>

              <v-table v-if="orgMembers.length > 0" density="compact" data-testid="members-table">
                <thead>
                  <tr>
                    <th>First Name</th>
                    <th>Last Name</th>
                    <th>Email</th>
                    <th style="width:100px">Role</th>
                    <th style="width:140px">Last Login</th>
                    <th style="width:60px"></th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="member in orgMembers" :key="member.userId" :data-testid="`member-row-${member.userId}`">
                    <td class="text-body-2">{{ member.firstName }}</td>
                    <td class="text-body-2">{{ member.lastName }}</td>
                    <td class="text-body-2">{{ member.email }}</td>
                    <td><v-chip size="x-small" :color="member.role === 'Admin' ? 'primary' : 'default'" variant="tonal">{{ member.role }}</v-chip></td>
                    <td class="text-caption text-medium-emphasis">{{ member.lastLoginAt ? formatDate(member.lastLoginAt) : '—' }}</td>
                    <td>
                      <v-btn icon="ri-delete-bin-line" size="x-small" variant="text" color="error"
                        :data-testid="`remove-member-${member.userId}`" @click="openConfirm('member', member.userId, `${member.firstName} ${member.lastName}`)" />
                    </td>
                  </tr>
                </tbody>
              </v-table>
              <p v-else class="text-body-2 text-medium-emphasis" data-testid="no-members-message">No members found.</p>
            </v-card-text>
          </v-card>

          <!-- Org webhooks -->
          <v-card rounded="lg">
            <v-card-text class="pa-4">
              <WebhooksPanel :org-id="contextStore.currentOrganization?.id" />
            </v-card-text>
          </v-card>
        </v-tabs-window-item>

        <!-- ── Project tab ─────────────────────────────────────────────── -->
        <v-tabs-window-item value="project" class="pt-2">
          <v-alert v-if="projectError" type="error" variant="tonal" density="compact" closable class="mb-4" @click:close="projectError = null">{{ projectError }}</v-alert>
          <v-alert v-if="projectSuccess" type="success" variant="tonal" density="compact" closable class="mb-4" @click:close="projectSuccess = null">{{ projectSuccess }}</v-alert>

          <v-card rounded="lg" class="mb-4" v-if="contextStore.currentProject">
            <v-card-title class="text-body-1 font-weight-medium pa-4 pb-2">Project Settings</v-card-title>
            <v-card-text class="pa-4 pt-0">
              <div class="d-flex align-center ga-3 mb-3">
                <v-text-field
                  v-model="projectNameInput"
                  label="Project name"
                  variant="outlined"
                  density="compact"
                  hide-details
                  style="max-width: 360px"
                  data-testid="project-name-input"
                />
              </div>
              <div class="d-flex align-center justify-space-between mb-4" style="max-width: 360px">
                <div>
                  <p class="text-body-2 font-weight-medium mb-0">Hide disabled flags</p>
                  <p class="text-caption text-medium-emphasis mb-0">Hide flags that are off in all environments</p>
                </div>
                <v-switch
                  v-model="hideDisabledFlags"
                  color="primary"
                  hide-details
                  density="compact"
                  data-testid="hide-disabled-flags-toggle"
                />
              </div>
              <v-btn
                color="primary"
                variant="flat"
                size="small"
                :loading="isSavingProject"
                data-testid="save-project-btn"
                @click="onSaveProject"
              >
                Save project settings
              </v-btn>
            </v-card-text>
          </v-card>

          <!-- User Permissions -->
          <v-card rounded="lg" v-if="contextStore.currentProject">
            <v-card-title class="text-body-1 font-weight-medium pa-4 pb-2">User Permissions</v-card-title>
            <v-card-text class="pa-4 pt-0">
              <v-table v-if="permissions.length > 0" density="compact" class="mb-4" data-testid="permissions-table">
                <thead>
                  <tr>
                    <th style="width: 100px">User ID</th>
                    <th style="width: 100px">Admin</th>
                    <th>Permissions</th>
                    <th style="width: 60px"></th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="perm in permissions" :key="perm.userId" :data-testid="`permission-row-${perm.userId}`">
                    <td class="text-body-2">{{ perm.userId }}</td>
                    <td>
                      <v-switch
                        :model-value="perm.isAdmin"
                        color="primary"
                        hide-details
                        density="compact"
                        :data-testid="`perm-admin-toggle-${perm.userId}`"
                        @update:model-value="onToggleAdmin(perm, $event)"
                      />
                    </td>
                    <td>
                      <div class="d-flex flex-wrap ga-1">
                        <v-chip
                          v-for="p in perm.permissions"
                          :key="p"
                          size="x-small"
                          variant="tonal"
                          color="primary"
                        >{{ p }}</v-chip>
                        <span v-if="perm.isAdmin" class="text-caption text-medium-emphasis">All permissions (admin)</span>
                      </div>
                    </td>
                    <td>
                      <v-btn icon="ri-delete-bin-line" size="x-small" variant="text" color="error"
                        :data-testid="`remove-permission-${perm.userId}`" @click="openConfirm('permission', perm.userId, `User ${perm.userId}`)" />
                    </td>
                  </tr>
                </tbody>
              </v-table>
              <p v-else class="text-body-2 text-medium-emphasis mb-4" data-testid="no-permissions-message">No user permissions configured.</p>

              <!-- Add user permissions -->
              <v-btn
                size="small"
                color="primary"
                variant="tonal"
                prepend-icon="ri-add-line"
                data-testid="add-permission-btn"
                @click="showAddPermission = !showAddPermission"
              >
                Add user
              </v-btn>

              <v-expand-transition>
                <v-card v-if="showAddPermission" variant="outlined" rounded="lg" class="mt-3 pa-3" data-testid="add-permission-form">
                  <div class="d-flex align-center ga-2 mb-3 flex-wrap">
                    <v-text-field
                      v-model="newPermUserId"
                      label="User ID"
                      variant="outlined"
                      density="compact"
                      hide-details
                      type="number"
                      style="max-width: 120px"
                      data-testid="new-perm-user-id-input"
                    />
                    <v-switch
                      v-model="newPermIsAdmin"
                      label="Admin"
                      color="primary"
                      hide-details
                      density="compact"
                      data-testid="new-perm-admin-toggle"
                    />
                  </div>
                  <div class="d-flex flex-wrap ga-2 mb-3">
                    <v-checkbox
                      v-for="p in allPermissions"
                      :key="p"
                      :label="p"
                      :model-value="newPermissions.includes(p)"
                      :disabled="newPermIsAdmin"
                      color="primary"
                      hide-details
                      density="compact"
                      :data-testid="`new-perm-checkbox-${p}`"
                      @update:model-value="toggleNewPermission(p, $event)"
                    />
                  </div>
                  <v-btn
                    color="primary"
                    variant="flat"
                    size="small"
                    :disabled="!newPermUserId"
                    :loading="isAddingPermission"
                    data-testid="save-new-permission-btn"
                    @click="onAddPermission"
                  >
                    Add
                  </v-btn>
                </v-card>
              </v-expand-transition>
            </v-card-text>
          </v-card>
        </v-tabs-window-item>

        <!-- ── Environments tab ───────────────────────────────────────── -->
        <v-tabs-window-item value="environments" class="pt-2">
          <v-alert v-if="envError" type="error" variant="tonal" density="compact" closable class="mb-4" @click:close="envError = null">{{ envError }}</v-alert>

          <!-- Create environment -->
          <v-card rounded="lg" class="mb-4">
            <v-card-title class="text-body-1 font-weight-medium pa-4 pb-2">Create Environment</v-card-title>
            <v-card-text class="pa-4 pt-0">
              <div class="d-flex align-center ga-3">
                <v-text-field
                  v-model="newEnvName"
                  label="Environment name"
                  variant="outlined"
                  density="compact"
                  hide-details
                  style="max-width: 280px"
                  data-testid="new-env-name-input"
                />
                <v-btn
                  color="primary"
                  variant="flat"
                  size="small"
                  :disabled="!newEnvName"
                  :loading="isCreatingEnv"
                  data-testid="create-env-btn"
                  @click="onCreateEnvironment"
                >
                  Create
                </v-btn>
              </div>
            </v-card-text>
          </v-card>

          <!-- Environments list -->
          <v-card rounded="lg">
            <v-card-text class="pa-4">
              <div v-if="environments.length === 0" class="text-center py-4 text-medium-emphasis text-body-2" data-testid="no-environments-message">
                No environments yet. Create one above.
              </div>

              <v-expansion-panels v-else variant="accordion" data-testid="environments-list">
                <v-expansion-panel
                  v-for="env in environments"
                  :key="env.id"
                  :data-testid="`env-panel-${env.id}`"
                >
                  <v-expansion-panel-title>
                    <div class="d-flex align-center ga-2 flex-grow-1">
                      <span class="text-body-2 font-weight-medium">{{ env.name }}</span>
                      <v-chip size="x-small" variant="tonal" color="secondary">{{ env.apiKey }}</v-chip>
                    </div>
                    <div class="d-flex ga-1 mr-2" @click.stop>
                      <v-btn icon="ri-file-copy-line" size="x-small" variant="text" :data-testid="`clone-env-${env.id}`" @click.stop="openCloneDialog(env)" />
                      <v-btn icon="ri-edit-line" size="x-small" variant="text" :data-testid="`rename-env-${env.id}`" @click.stop="openRenameDialog(env)" />
                      <v-btn icon="ri-delete-bin-line" size="x-small" variant="text" color="error" :data-testid="`delete-env-${env.id}`" @click.stop="openDeleteEnvConfirm(env)" />
                    </div>
                  </v-expansion-panel-title>
                  <v-expansion-panel-text>
                    <WebhooksPanel :env-api-key="env.apiKey" />
                  </v-expansion-panel-text>
                </v-expansion-panel>
              </v-expansion-panels>
            </v-card-text>
          </v-card>
        </v-tabs-window-item>

        <!-- ── API Keys tab ───────────────────────────────────────────── -->
        <v-tabs-window-item value="api-keys" class="pt-2">
          <v-alert v-if="apiKeyError" type="error" variant="tonal" density="compact" closable class="mb-4" @click:close="apiKeyError = null">{{ apiKeyError }}</v-alert>

          <div class="d-flex align-center justify-space-between mb-4">
            <p class="text-body-1 font-weight-medium mb-0">API Keys</p>
            <v-btn
              size="small"
              color="primary"
              variant="tonal"
              prepend-icon="ri-add-line"
              data-testid="create-api-key-btn"
              @click="showApiKeyDialog = true"
            >
              Create API Key
            </v-btn>
          </div>

          <v-card rounded="lg">
            <div v-if="apiKeys.length === 0" class="text-center py-8 text-medium-emphasis text-body-2" data-testid="no-api-keys-message">
              No API keys. Create one to allow programmatic access.
            </div>
            <v-table v-else density="compact" data-testid="api-keys-table">
              <thead>
                <tr>
                  <th>Name</th>
                  <th style="width: 120px">Prefix</th>
                  <th style="width: 80px">Active</th>
                  <th style="width: 140px">Expires</th>
                  <th style="width: 50px"></th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="key in apiKeys" :key="key.id" :data-testid="`api-key-row-${key.id}`">
                  <td class="text-body-2 font-weight-medium">{{ key.name }}</td>
                  <td class="text-body-2 font-mono text-medium-emphasis">{{ key.prefix }}</td>
                  <td>
                    <v-chip :color="key.isActive ? 'success' : 'error'" size="x-small" variant="tonal">
                      {{ key.isActive ? 'Active' : 'Revoked' }}
                    </v-chip>
                  </td>
                  <td class="text-caption text-medium-emphasis">{{ key.expiresAt ? formatDate(key.expiresAt) : 'Never' }}</td>
                  <td>
                    <v-btn icon="ri-delete-bin-line" size="x-small" variant="text" color="error"
                      :data-testid="`delete-api-key-${key.id}`" @click="openConfirm('apiKey', key.id, key.name)" />
                  </td>
                </tr>
              </tbody>
            </v-table>
          </v-card>
        </v-tabs-window-item>
      </v-tabs-window>
    </template>

    <!-- Generic confirmation dialog (member / permission / api key) -->
    <v-dialog v-model="showConfirmDialog" max-width="400" persistent>
      <v-card rounded="lg" data-testid="confirm-delete-dialog">
        <v-card-title class="text-body-1 font-weight-bold pa-4 pb-2">Confirm Delete</v-card-title>
        <v-card-text class="pa-4 pt-0">
          <p class="text-body-2">
            Delete <strong>{{ confirmLabel }}</strong>? This cannot be undone.
          </p>
        </v-card-text>
        <v-card-actions class="pa-4 pt-0">
          <v-spacer />
          <v-btn variant="text" :disabled="isConfirmDeleting" @click="closeConfirm">Cancel</v-btn>
          <v-btn color="error" variant="flat" :loading="isConfirmDeleting" data-testid="confirm-delete-btn" @click="onConfirmDelete">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Clone environment dialog -->
    <v-dialog v-model="showCloneDialog" max-width="400">
      <v-card rounded="lg" data-testid="clone-env-dialog">
        <v-card-title class="text-body-1 font-weight-bold pa-4 pb-2">Clone Environment</v-card-title>
        <v-card-text class="pa-4 pt-0">
          <p class="text-body-2 mb-3">Cloning: <strong>{{ cloningEnv?.name }}</strong></p>
          <v-text-field
            v-model="cloneEnvName"
            label="New environment name"
            variant="outlined"
            density="compact"
            hide-details
            data-testid="clone-env-name-input"
          />
        </v-card-text>
        <v-card-actions class="pa-4 pt-0">
          <v-spacer />
          <v-btn variant="text" @click="showCloneDialog = false">Cancel</v-btn>
          <v-btn color="primary" variant="flat" :disabled="!cloneEnvName" :loading="isCloning" data-testid="confirm-clone-btn" @click="onCloneEnvironment">Clone</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Rename environment dialog -->
    <v-dialog v-model="showRenameDialog" max-width="400">
      <v-card rounded="lg" data-testid="rename-env-dialog">
        <v-card-title class="text-body-1 font-weight-bold pa-4 pb-2">Rename Environment</v-card-title>
        <v-card-text class="pa-4 pt-0">
          <v-text-field
            v-model="renameEnvName"
            label="New name"
            variant="outlined"
            density="compact"
            hide-details
            data-testid="rename-env-name-input"
          />
        </v-card-text>
        <v-card-actions class="pa-4 pt-0">
          <v-spacer />
          <v-btn variant="text" @click="showRenameDialog = false">Cancel</v-btn>
          <v-btn color="primary" variant="flat" :disabled="!renameEnvName" :loading="isRenaming" data-testid="confirm-rename-btn" @click="onRenameEnvironment">Save</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Delete environment dialog -->
    <v-dialog v-model="showDeleteEnvDialog" max-width="400" persistent>
      <v-card rounded="lg" data-testid="delete-env-dialog">
        <v-card-title class="text-body-1 font-weight-bold pa-4 pb-2">Delete Environment</v-card-title>
        <v-card-text class="pa-4 pt-0">
          <p class="text-body-2">Delete <strong>{{ deletingEnv?.name }}</strong>? This cannot be undone.</p>
        </v-card-text>
        <v-card-actions class="pa-4 pt-0">
          <v-spacer />
          <v-btn variant="text" :disabled="isDeletingEnv" @click="showDeleteEnvDialog = false">Cancel</v-btn>
          <v-btn color="error" variant="flat" :loading="isDeletingEnv" data-testid="confirm-delete-env-btn" @click="onDeleteEnvironment">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Create API Key dialog -->
    <v-dialog v-model="showApiKeyDialog" max-width="420">
      <v-card rounded="lg" data-testid="create-api-key-dialog">
        <v-card-title class="text-body-1 font-weight-bold pa-4 pb-2">Create API Key</v-card-title>
        <v-card-text class="pa-4 pt-0">
          <v-text-field
            v-model="newKeyName"
            label="Key name"
            variant="outlined"
            density="compact"
            class="mb-3"
            hide-details
            data-testid="api-key-name-input"
          />
          <v-text-field
            v-model="newKeyExpiry"
            label="Expires (optional)"
            variant="outlined"
            density="compact"
            hide-details
            type="date"
            data-testid="api-key-expiry-input"
          />
        </v-card-text>
        <v-card-actions class="pa-4 pt-0">
          <v-spacer />
          <v-btn variant="text" @click="showApiKeyDialog = false">Cancel</v-btn>
          <v-btn color="primary" variant="flat" :disabled="!newKeyName" :loading="isCreatingKey" data-testid="save-api-key-btn" @click="onCreateApiKey">Create</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Add Member dialog -->
    <v-dialog v-model="showAddMemberDialog" max-width="520" persistent>
      <v-card rounded="lg" data-testid="add-member-dialog">
        <v-card-title class="text-body-1 font-weight-bold pa-4 pb-2">Add Members</v-card-title>
        <v-card-text class="pa-4 pt-0">
          <div
            v-for="(entry, i) in addMemberEntries"
            :key="i"
            class="d-flex align-start ga-2 mb-2"
          >
            <v-text-field
              v-model="entry.email"
              label="Email"
              variant="outlined"
              density="compact"
              :rules="[validateEmail]"
              :data-testid="`add-member-email-${i}`"
            />
            <v-select
              v-model="entry.role"
              :items="['User', 'Admin']"
              label="Role"
              variant="outlined"
              density="compact"
              hide-details
              style="max-width: 120px"
              :data-testid="`add-member-role-${i}`"
            />
            <v-btn
              v-if="addMemberEntries.length > 1"
              icon="ri-close-line"
              size="x-small"
              variant="text"
              @click="removeMemberRow(i)"
            />
          </div>

          <v-btn
            size="small"
            variant="text"
            prepend-icon="ri-add-line"
            class="mt-1"
            data-testid="add-another-member-btn"
            @click="addMemberRow"
          >
            Add another
          </v-btn>

          <div v-if="addMemberResults.length > 0" class="mt-3">
            <div
              v-for="result in addMemberResults"
              :key="result.email"
              class="d-flex align-center ga-2 text-body-2 mb-1"
            >
              <v-icon :color="result.success ? 'success' : 'error'" size="16">
                {{ result.success ? 'ri-check-line' : 'ri-error-warning-line' }}
              </v-icon>
              <span>{{ result.email }}</span>
              <span v-if="!result.success" class="text-error">— {{ result.error }}</span>
            </div>
          </div>
        </v-card-text>
        <v-card-actions class="pa-4 pt-0">
          <v-spacer />
          <v-btn variant="text" :disabled="isAddingMembers" @click="showAddMemberDialog = false">
            {{ addMemberResults.length > 0 ? 'Close' : 'Cancel' }}
          </v-btn>
          <v-btn
            v-if="addMemberResults.length === 0"
            color="primary"
            variant="flat"
            :loading="isAddingMembers"
            :disabled="!addMemberEntries.some(e => e.email.trim()) || addMemberEntries.some(e => e.email.trim() && validateEmail(e.email) !== true)"
            data-testid="confirm-add-members-btn"
            @click="onAddMembers"
          >
            Add
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Raw key reveal dialog (shown once after creation) -->
    <v-dialog v-model="showRawKeyDialog" max-width="480" persistent>
      <v-card rounded="lg" data-testid="raw-key-dialog">
        <v-card-title class="pa-4 pb-2 d-flex align-center ga-2">
          <v-icon color="warning">ri-alert-line</v-icon>
          <span class="text-body-1 font-weight-bold">Save your API key</span>
        </v-card-title>
        <v-card-text class="pa-4 pt-0">
          <p class="text-body-2 mb-3">This key will only be shown once. Copy it now.</p>
          <v-text-field
            :model-value="createdRawKey"
            variant="outlined"
            density="compact"
            readonly
            hide-details
            :append-inner-icon="rawKeyCopied ? 'ri-check-line' : 'ri-file-copy-line'"
            data-testid="raw-key-field"
            @click:append-inner="copyRawKey"
          />
          <p v-if="rawKeyCopied" class="text-caption text-success mt-1" data-testid="copy-success-msg">Copied to clipboard!</p>
        </v-card-text>
        <v-card-actions class="pa-4 pt-0">
          <v-spacer />
          <v-btn color="primary" variant="flat" data-testid="close-raw-key-btn" @click="showRawKeyDialog = false">Done</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue';
import { updateOrganization, listOrganizationMembers, removeOrganizationMember, inviteOrganizationMembersByEmail, getInviteLink, regenerateInviteLink } from '@/api/organizations';
import { updateProject, listProjectUserPermissions, setProjectUserPermissions, updateProjectUserPermissions, removeProjectUserPermissions } from '@/api/projects';
import { listEnvironments, createEnvironment, updateEnvironment, deleteEnvironment, cloneEnvironment } from '@/api/environments';
import { listApiKeys, createApiKey, deleteApiKey } from '@/api/apiKeys';
import { useContextStore } from '@/stores/context';
import WebhooksPanel from '@/components/settings/WebhooksPanel.vue';
import type { OrganizationMemberResponse, UserPermissionResponse, EnvironmentResponse, ApiKeyResponse, ProjectPermission, InviteByEmailResult } from '@/types/api';
import { validateEmail } from '@/utils/validation';

const contextStore = useContextStore();

const activeTab = ref('organization');

// ── Shared confirm dialog ─────────────────────────────────────────────────────
type ConfirmType = 'member' | 'permission' | 'apiKey';
const showConfirmDialog = ref(false);
const confirmType = ref<ConfirmType>('member');
const confirmId = ref(0);
const confirmLabel = ref('');
const isConfirmDeleting = ref(false);

function openConfirm(type: ConfirmType, id: number, label: string): void {
  confirmType.value = type;
  confirmId.value = id;
  confirmLabel.value = label;
  showConfirmDialog.value = true;
}

function closeConfirm(): void {
  showConfirmDialog.value = false;
}

async function onConfirmDelete(): Promise<void> {
  isConfirmDeleting.value = true;
  try {
    if (confirmType.value === 'member') await onRemoveMember(confirmId.value);
    else if (confirmType.value === 'permission') await onRemovePermission(confirmId.value);
    else if (confirmType.value === 'apiKey') await onDeleteApiKey(confirmId.value);
    closeConfirm();
  } finally {
    isConfirmDeleting.value = false;
  }
}

// ── Organization ──────────────────────────────────────────────────────────────
const orgNameInput = ref('');
const isSavingOrg = ref(false);
const orgError = ref<string | null>(null);
const orgSuccess = ref<string | null>(null);
const orgMembers = ref<OrganizationMemberResponse[]>([]);

// Invite link
const inviteLink = ref('');
const isLoadingInviteLink = ref(false);
const isRegeneratingLink = ref(false);
const inviteLinkCopied = ref(false);

// Add member dialog
interface AddMemberEntry { email: string; role: 'Admin' | 'User' }
const showAddMemberDialog = ref(false);
const addMemberEntries = ref<AddMemberEntry[]>([{ email: '', role: 'User' }]);
const isAddingMembers = ref(false);
const addMemberResults = ref<InviteByEmailResult[]>([]);

watch(() => contextStore.currentOrganization, (org) => {
  orgNameInput.value = org?.name ?? '';
  if (org) {
    loadOrgMembers();
    loadInviteLink();
  } else {
    inviteLink.value = '';
  }
}, { immediate: true });

async function loadOrgMembers(): Promise<void> {
  const orgId = contextStore.currentOrganization?.id;
  if (!orgId) return;
  try {
    orgMembers.value = await listOrganizationMembers(orgId);
  } catch {
    orgError.value = 'Failed to load members.';
  }
}

async function onSaveOrgName(): Promise<void> {
  const orgId = contextStore.currentOrganization?.id;
  if (!orgId) return;
  isSavingOrg.value = true;
  orgError.value = null;
  orgSuccess.value = null;
  try {
    await updateOrganization(orgId, { name: orgNameInput.value });
    orgSuccess.value = 'Organization name updated.';
  } catch {
    orgError.value = 'Failed to update organization name.';
  } finally {
    isSavingOrg.value = false;
  }
}

async function loadInviteLink(): Promise<void> {
  const orgId = contextStore.currentOrganization?.id;
  if (!orgId) return;
  isLoadingInviteLink.value = true;
  try {
    const { token } = await getInviteLink(orgId);
    inviteLink.value = `${window.location.origin}/accept-invite/${token}`;
  } catch {
    // non-critical
  } finally {
    isLoadingInviteLink.value = false;
  }
}

async function onCopyInviteLink(): Promise<void> {
  const orgId = contextStore.currentOrganization?.id;
  if (!orgId) return;
  try {
    await navigator.clipboard.writeText(inviteLink.value);
    inviteLinkCopied.value = true;
    setTimeout(() => { inviteLinkCopied.value = false; }, 2000);
  } catch {
    // clipboard unavailable
  }
  isRegeneratingLink.value = true;
  try {
    const { token } = await regenerateInviteLink(orgId);
    inviteLink.value = `${window.location.origin}/accept-invite/${token}`;
  } catch {
    // non-critical
  } finally {
    isRegeneratingLink.value = false;
  }
}

function openAddMemberDialog(): void {
  addMemberEntries.value = [{ email: '', role: 'User' }];
  addMemberResults.value = [];
  showAddMemberDialog.value = true;
}

function addMemberRow(): void {
  addMemberEntries.value.push({ email: '', role: 'User' });
}

function removeMemberRow(index: number): void {
  addMemberEntries.value.splice(index, 1);
}

async function onAddMembers(): Promise<void> {
  const orgId = contextStore.currentOrganization?.id;
  if (!orgId) return;
  const valid = addMemberEntries.value.filter((e) => e.email.trim());
  if (!valid.length) return;
  isAddingMembers.value = true;
  addMemberResults.value = [];
  try {
    addMemberResults.value = await inviteOrganizationMembersByEmail(orgId, { invites: valid });
    await loadOrgMembers();
  } catch {
    orgError.value = 'Failed to add members.';
  } finally {
    isAddingMembers.value = false;
  }
}

async function onRemoveMember(userId: number): Promise<void> {
  const orgId = contextStore.currentOrganization?.id;
  if (!orgId) return;
  orgError.value = null;
  try {
    await removeOrganizationMember(orgId, userId);
    orgMembers.value = orgMembers.value.filter((m) => m.userId !== userId);
  } catch {
    orgError.value = 'Failed to remove member.';
  }
}

// ── Project ───────────────────────────────────────────────────────────────────
const projectNameInput = ref('');
const hideDisabledFlags = ref(false);
const isSavingProject = ref(false);
const projectError = ref<string | null>(null);
const projectSuccess = ref<string | null>(null);
const permissions = ref<UserPermissionResponse[]>([]);
const showAddPermission = ref(false);
const newPermUserId = ref('');
const newPermIsAdmin = ref(false);
const newPermissions = ref<ProjectPermission[]>([]);
const isAddingPermission = ref(false);

const allPermissions: ProjectPermission[] = [
  'ViewProject', 'CreateFeature', 'EditFeature', 'DeleteFeature',
  'CreateEnvironment', 'EditEnvironment', 'DeleteEnvironment',
  'CreateSegment', 'EditSegment', 'DeleteSegment',
  'ManageWebhooks', 'ViewAuditLog',
];

watch(() => contextStore.currentProject, (project) => {
  projectNameInput.value = project?.name ?? '';
  hideDisabledFlags.value = project?.hideDisabledFlags ?? false;
  if (project) loadPermissions();
}, { immediate: true });

async function loadPermissions(): Promise<void> {
  const projectId = contextStore.currentProject?.id;
  if (!projectId) return;
  try {
    permissions.value = await listProjectUserPermissions(projectId);
  } catch {
    projectError.value = 'Failed to load permissions.';
  }
}

async function onSaveProject(): Promise<void> {
  const projectId = contextStore.currentProject?.id;
  if (!projectId) return;
  isSavingProject.value = true;
  projectError.value = null;
  projectSuccess.value = null;
  try {
    await updateProject(projectId, { name: projectNameInput.value, hideDisabledFlags: hideDisabledFlags.value });
    projectSuccess.value = 'Project settings saved.';
  } catch {
    projectError.value = 'Failed to save project settings.';
  } finally {
    isSavingProject.value = false;
  }
}

async function onToggleAdmin(perm: UserPermissionResponse, isAdmin: boolean | null): Promise<void> {
  if (isAdmin === null) return;
  const projectId = contextStore.currentProject?.id;
  if (!projectId) return;
  projectError.value = null;
  try {
    const updated = await updateProjectUserPermissions(projectId, perm.userId, {
      userId: perm.userId,
      isAdmin,
      permissions: isAdmin ? [] : perm.permissions,
    });
    const idx = permissions.value.findIndex((p) => p.userId === perm.userId);
    if (idx >= 0) permissions.value[idx] = updated;
  } catch {
    projectError.value = 'Failed to update permissions.';
  }
}

async function onRemovePermission(userId: number): Promise<void> {
  const projectId = contextStore.currentProject?.id;
  if (!projectId) return;
  projectError.value = null;
  try {
    await removeProjectUserPermissions(projectId, userId);
    permissions.value = permissions.value.filter((p) => p.userId !== userId);
  } catch {
    projectError.value = 'Failed to remove permission.';
  }
}

function toggleNewPermission(perm: ProjectPermission, checked: boolean | null): void {
  if (checked === null) return;
  if (checked && !newPermissions.value.includes(perm)) {
    newPermissions.value = [...newPermissions.value, perm];
  } else if (!checked) {
    newPermissions.value = newPermissions.value.filter((p) => p !== perm);
  }
}

async function onAddPermission(): Promise<void> {
  const projectId = contextStore.currentProject?.id;
  if (!projectId || !newPermUserId.value) return;
  isAddingPermission.value = true;
  projectError.value = null;
  try {
    const saved = await setProjectUserPermissions(projectId, {
      userId: Number(newPermUserId.value),
      isAdmin: newPermIsAdmin.value,
      permissions: newPermIsAdmin.value ? [] : newPermissions.value,
    });
    permissions.value = [...permissions.value, saved];
    newPermUserId.value = '';
    newPermIsAdmin.value = false;
    newPermissions.value = [];
    showAddPermission.value = false;
  } catch {
    projectError.value = 'Failed to add user permissions.';
  } finally {
    isAddingPermission.value = false;
  }
}

// ── Environments ──────────────────────────────────────────────────────────────
const environments = ref<EnvironmentResponse[]>([]);
const newEnvName = ref('');
const isCreatingEnv = ref(false);
const envError = ref<string | null>(null);

const showCloneDialog = ref(false);
const cloningEnv = ref<EnvironmentResponse | null>(null);
const cloneEnvName = ref('');
const isCloning = ref(false);

const showRenameDialog = ref(false);
const renamingEnv = ref<EnvironmentResponse | null>(null);
const renameEnvName = ref('');
const isRenaming = ref(false);

const showDeleteEnvDialog = ref(false);
const deletingEnv = ref<EnvironmentResponse | null>(null);
const isDeletingEnv = ref(false);

watch(() => contextStore.currentProject, (project) => {
  if (project) loadEnvironments();
  else environments.value = [];
}, { immediate: true });

async function loadEnvironments(): Promise<void> {
  const projectId = contextStore.currentProject?.id;
  if (!projectId) return;
  envError.value = null;
  try {
    environments.value = await listEnvironments(projectId);
  } catch {
    envError.value = 'Failed to load environments.';
  }
}

async function onCreateEnvironment(): Promise<void> {
  const projectId = contextStore.currentProject?.id;
  if (!projectId || !newEnvName.value) return;
  isCreatingEnv.value = true;
  envError.value = null;
  try {
    const created = await createEnvironment({ projectId, name: newEnvName.value });
    environments.value.push(created);
    newEnvName.value = '';
  } catch {
    envError.value = 'Failed to create environment.';
  } finally {
    isCreatingEnv.value = false;
  }
}

function openCloneDialog(env: EnvironmentResponse): void {
  cloningEnv.value = env;
  cloneEnvName.value = '';
  showCloneDialog.value = true;
}

function openRenameDialog(env: EnvironmentResponse): void {
  renamingEnv.value = env;
  renameEnvName.value = env.name;
  showRenameDialog.value = true;
}

function openDeleteEnvConfirm(env: EnvironmentResponse): void {
  deletingEnv.value = env;
  showDeleteEnvDialog.value = true;
}

async function onCloneEnvironment(): Promise<void> {
  if (!cloningEnv.value || !cloneEnvName.value) return;
  isCloning.value = true;
  envError.value = null;
  try {
    const cloned = await cloneEnvironment(cloningEnv.value.apiKey, { name: cloneEnvName.value });
    environments.value.push(cloned);
    showCloneDialog.value = false;
  } catch {
    envError.value = 'Failed to clone environment.';
  } finally {
    isCloning.value = false;
  }
}

async function onRenameEnvironment(): Promise<void> {
  if (!renamingEnv.value || !renameEnvName.value) return;
  isRenaming.value = true;
  envError.value = null;
  try {
    const updated = await updateEnvironment(renamingEnv.value.apiKey, { name: renameEnvName.value });
    const idx = environments.value.findIndex((e) => e.id === renamingEnv.value!.id);
    if (idx >= 0) environments.value[idx] = updated;
    showRenameDialog.value = false;
  } catch {
    envError.value = 'Failed to rename environment.';
  } finally {
    isRenaming.value = false;
  }
}

async function onDeleteEnvironment(): Promise<void> {
  if (!deletingEnv.value) return;
  isDeletingEnv.value = true;
  envError.value = null;
  try {
    await deleteEnvironment(deletingEnv.value.apiKey);
    environments.value = environments.value.filter((e) => e.id !== deletingEnv.value!.id);
    showDeleteEnvDialog.value = false;
  } catch {
    envError.value = 'Failed to delete environment.';
  } finally {
    isDeletingEnv.value = false;
  }
}

// ── API Keys ──────────────────────────────────────────────────────────────────
const apiKeys = ref<ApiKeyResponse[]>([]);
const showApiKeyDialog = ref(false);
const newKeyName = ref('');
const newKeyExpiry = ref('');
const isCreatingKey = ref(false);
const apiKeyError = ref<string | null>(null);

const showRawKeyDialog = ref(false);
const createdRawKey = ref('');
const rawKeyCopied = ref(false);

watch(() => contextStore.currentOrganization, (org) => {
  if (org) loadApiKeys();
  else apiKeys.value = [];
}, { immediate: true });

async function loadApiKeys(): Promise<void> {
  const orgId = contextStore.currentOrganization?.id;
  if (!orgId) return;
  apiKeyError.value = null;
  try {
    apiKeys.value = await listApiKeys(orgId);
  } catch {
    apiKeyError.value = 'Failed to load API keys.';
  }
}

async function onCreateApiKey(): Promise<void> {
  const orgId = contextStore.currentOrganization?.id;
  if (!orgId || !newKeyName.value) return;
  isCreatingKey.value = true;
  apiKeyError.value = null;
  try {
    const result = await createApiKey(orgId, {
      name: newKeyName.value,
      expiresAt: newKeyExpiry.value ? `${newKeyExpiry.value}T00:00:00Z` : null,
    });
    apiKeys.value.push({
      id: result.id,
      name: result.name,
      prefix: result.prefix,
      isActive: true,
      expiresAt: result.expiresAt ? String(result.expiresAt) : null,
      createdAt: new Date().toISOString(),
    });
    createdRawKey.value = result.rawKey;
    rawKeyCopied.value = false;
    showApiKeyDialog.value = false;
    newKeyName.value = '';
    newKeyExpiry.value = '';
    showRawKeyDialog.value = true;
  } catch {
    apiKeyError.value = 'Failed to create API key.';
  } finally {
    isCreatingKey.value = false;
  }
}

async function onDeleteApiKey(keyId: number): Promise<void> {
  const orgId = contextStore.currentOrganization?.id;
  if (!orgId) return;
  apiKeyError.value = null;
  try {
    await deleteApiKey(orgId, keyId);
    apiKeys.value = apiKeys.value.filter((k) => k.id !== keyId);
  } catch {
    apiKeyError.value = 'Failed to delete API key.';
  }
}

async function copyRawKey(): Promise<void> {
  try {
    await navigator.clipboard.writeText(createdRawKey.value);
    rawKeyCopied.value = true;
  } catch {
    // clipboard unavailable in some contexts
  }
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
}

onMounted(() => {
  if (contextStore.currentOrganization) {
    orgNameInput.value = contextStore.currentOrganization.name;
    loadOrgMembers();
    loadApiKeys();
    loadInviteLink();
  }
  if (contextStore.currentProject) {
    projectNameInput.value = contextStore.currentProject.name;
    hideDisabledFlags.value = contextStore.currentProject.hideDisabledFlags;
    loadPermissions();
    loadEnvironments();
  }
});
</script>
