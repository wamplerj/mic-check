# MicCheck Admin Site Plan

## Overview

Build a fully functional VueJS 3 + Vuetify 3 + TypeScript single-page admin application for the MicCheck feature flag platform. The site manages the complete lifecycle of feature flags across the Organization → Project → Environment hierarchy exposed by the MicCheck API.

---

## Architecture Decisions

- **State Management**: Pinia (replaces Vuex; first-class Vue 3 support)
- **HTTP Client**: Axios with request/response interceptors for auth token injection and 401 refresh handling
- **Component Library**: Vuetify 3 (already configured)
- **Router**: Vue Router 4 (already configured)
- **Testing**: Jest + Vue Test Utils (already configured)
- **Code Style**: Composition API (`<script setup>`), TypeScript throughout

### Context Model

The app operates in a three-level context:
1. **Organization** — selected at login/org-switcher; stored globally
2. **Project** — selected via project switcher in nav; stored globally
3. **Environment** — selected via environment dropdown in nav; stored globally

All feature, segment, identity, and audit views are scoped to the current org/project/environment context.

---

## Chunk 1: Foundation — API Client, Auth & Stores

### Goals
Set up the core infrastructure that all subsequent chunks depend on: typed API client, Pinia stores, authentication flows, and route guards.

### Tasks

#### 1.1 Install Pinia
- Add `pinia` to `package.json`
- Register Pinia in `main.ts`

#### 1.2 Typed API Client (`src/api/`)
Create an `ApiClient` class using Axios:
- Base URL configured from environment variable (`VITE_API_BASE_URL` or webpack `DefinePlugin`)
- Request interceptor: inject `Authorization: Bearer <token>` for admin calls, or `X-Environment-Key` for flags API calls
- Response interceptor: on 401, attempt token refresh via `POST /api/v1/auth/refresh`; on refresh failure, redirect to `/login`
- Typed methods for each API domain (auth, organizations, projects, environments, features, featureStates, segments, tags, auditLogs, webhooks, identities, apiKeys)
- All responses typed using TypeScript interfaces mirroring the API response models

#### 1.3 Auth Store (`src/stores/auth.ts`)
Pinia store with:
- State: `user`, `accessToken`, `refreshToken`, `expiresAt`, `isAuthenticated`
- Actions: `login(email, password)`, `register(...)`, `logout()`, `refreshToken()`, `loadFromStorage()`
- Persist tokens to `localStorage`; restore on app init

#### 1.4 Context Store (`src/stores/context.ts`)
Pinia store with:
- State: `currentOrganization`, `currentProject`, `currentEnvironment`
- Actions: `setOrganization(org)`, `setProject(project)`, `setEnvironment(env)`
- Persist selections to `localStorage`

#### 1.5 Login & Register Views
- `LoginView.vue`: email/password form → calls auth store `login()` → redirects to dashboard
- `RegisterView.vue`: email, password, first name, last name, organization name → calls `register()` → redirects to dashboard
- Both views displayed without the main `AppLayout` (full-page centered card)
- Client-side validation with Vuetify's built-in form validation

#### 1.6 Route Guards
In `router/index.ts`:
- `beforeEach` guard: if route requires auth and user is not authenticated, redirect to `/login`
- If authenticated user navigates to `/login`, redirect to `/`
- Mark public routes (`/login`, `/register`) with `meta: { public: true }`

#### 1.7 Unit Tests
- Auth store: login success, login failure, token refresh, logout
- API client: interceptors, token injection, refresh on 401

---

## Chunk 2: Navigation & Context Selectors

### Goals
Build the main app shell with a context-aware navigation bar that allows switching between organizations, projects, and environments.

### Tasks

#### 2.1 Update `AppLayout.vue`
- Left sidebar (Vuetify `v-navigation-drawer`) with:
  - MicCheck logo at top
  - Organization selector (dropdown showing org name)
  - Project selector (dropdown scoped to selected org)
  - Environment pill/tab selector (shows all environments for current project)
  - Navigation links: Features, Segments, Identities, Audit Logs
  - Bottom: Settings link
- Top `v-app-bar` with:
  - Page title / breadcrumb
  - User name and avatar icon → `/profile`
  - Gear icon → `/settings`

#### 2.2 Organization Selector Component (`src/components/OrgSelector.vue`)
- Fetches `GET /api/v1/organisations` on mount
- Displays current org name as button; opens dropdown to switch
- On select: updates context store, resets project and environment selection

#### 2.3 Project Selector Component (`src/components/ProjectSelector.vue`)
- Fetches `GET /api/v1/projects?organizationId=` when org changes
- Displays current project name; opens dropdown to switch
- On select: updates context store, resets environment selection

#### 2.4 Environment Tab Bar Component (`src/components/EnvironmentTabBar.vue`)
- Fetches `GET /api/v1/environments?projectId=` when project changes
- Displays environments as tabs (Vuetify `v-tabs`)
- Highlights selected environment; updates context store on click
- "Add Environment" button at the end of the tabs

#### 2.5 Empty States
- If no organization: prompt to create one
- If no project: prompt to create first project
- If no environment: prompt to create first environment

#### 2.6 Update Router
Add routes:
```
/login          → LoginView (public)
/register       → RegisterView (public)
/               → DashboardView (auth required)
/features       → FeaturesView (auth required)
/segments       → SegmentsView (auth required)
/identities     → IdentitiesView (auth required)
/audit-logs     → AuditLogsView (auth required)
/settings       → SettingsView (auth required)
/profile        → ProfileView (auth required)
```

#### 2.7 Dashboard View
Simple landing page showing:
- Selected org/project/environment summary cards
- Quick stats: number of features, segments, enabled flags in current environment
- Links to main sections

#### 2.8 Unit Tests
- OrgSelector: loads orgs, emits selection
- ProjectSelector: loads projects on org change
- EnvironmentTabBar: loads envs, highlights selected, emits selection
- Route guard: unauthenticated redirect, authenticated bypass

---

## Chunk 3: Feature Flags Management

### Goals
Full CRUD for features (project-level) and their per-environment states (toggle on/off, set value).

### Tasks

#### 3.1 Features List View (`FeaturesView.vue`)
- Table/data-grid using Vuetify `v-data-table-server` with server-side pagination
- Columns: Name, Type (STANDARD/MULTIVARIATE), Description, Default Enabled, Created At, Actions
- Per-row toggle switch for the current environment's feature state (`PATCH /api/v1/environments/{apiKey}/featurestates/{id}`)
- Search/filter by name
- "Create Feature" button → opens dialog

#### 3.2 Feature Create/Edit Dialog (`src/components/features/FeatureDialog.vue`)
- Fields:
  - **Name** (text input; validates `^[a-zA-Z0-9_-]+$`, max 150)
  - **Type** (select: STANDARD / MULTIVARIATE)
  - **Initial Value** (text area; shown for MULTIVARIATE; max 20,000 chars)
  - **Description** (text area; optional)
- On create: `POST /api/v1/projects/{projectId}/features`
- On edit: `PUT /api/v1/projects/{projectId}/features/{id}` (name + description only)
- On save: refresh feature list

#### 3.3 Feature Detail Panel / Drawer (`src/components/features/FeatureDetail.vue`)
Slide-out `v-navigation-drawer` or dialog opened on row click:
- **Value tab**: 
  - Toggle enabled/disabled for current environment (`PATCH featurestates/{id}`)
  - Edit value field for current environment
  - Tags chip display and management
- **Settings tab**:
  - Edit name and description
  - `defaultEnabled` toggle (via PATCH feature)
  - Danger zone: Delete feature (confirmation requiring re-type of flag name)

#### 3.4 Tags Management (`src/components/features/TagsChipInput.vue`)
- Inline chip input on feature forms
- Autocomplete from `GET /api/v1/projects/{projectId}/tags`
- Create new tag inline (label + color picker)
- `POST /api/v1/projects/{projectId}/tags` on new tag
- Delete unused tags via `DELETE /api/v1/projects/{projectId}/tags/{id}`

#### 3.5 Feature State Value Editor (`src/components/features/FeatureValueEditor.vue`)
- Detects value type (boolean/number/JSON/string) and renders appropriate input
- JSON values get a code editor (simple `v-textarea` with monospace font)
- Saves via `PUT /api/v1/environments/{apiKey}/featurestates/{id}`

#### 3.6 Unit Tests
- FeaturesView: renders list, pagination, toggle state
- FeatureDialog: validation, create call, edit call
- TagsChipInput: loads existing tags, creates new tag
- FeatureDetail: tab switching, delete confirmation

---

## Chunk 4: Segments

### Goals
Full CRUD for project-level segments with a visual rule builder, and segment-level feature state overrides per environment.

### Tasks

#### 4.1 Segments List View (`SegmentsView.vue`)
- Table: Name, Rule Count, Created At, Actions (Edit, Delete)
- "Create Segment" button → opens Segment Editor
- Delete with confirmation dialog

#### 4.2 Segment Editor Dialog (`src/components/segments/SegmentEditor.vue`)
Full-featured rule builder:
- **Name** field
- **Rules section**: List of rule groups (AND/OR type selector)
  - Each rule group contains **Conditions**:
    - Property (trait key, text input)
    - Operator (select from: Equal, NotEqual, Contains, NotContains, Regex, GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual, IsTrue, IsFalse, In, NotIn, IsSet, IsNotSet, PercentageSplit)
    - Value (text input; hidden for IsTrue/IsFalse/IsSet/IsNotSet operators; percentage slider for PercentageSplit)
  - Add/remove conditions within a group
  - Add/remove rule groups
  - Nested child rules supported (recursive component)
- On save: `POST` or `PUT /api/v1/projects/{projectId}/segments`
- Constraint display: shows "X / 100 conditions used"

#### 4.3 Segment Overrides on Features
Within the Feature Detail drawer (Chunk 3):
- **Overrides tab**: list of segments that have overrides for this feature in current environment
  - Each override shows: Segment Name, Enabled toggle, Value
  - "Add Segment Override" button → select segment from dropdown → configure enabled/value
  - Saves via Feature State API (`featureSegmentId` populated)
  - Delete override button

#### 4.4 Unit Tests
- SegmentsView: list, delete
- SegmentEditor: rule builder add/remove conditions, add/remove groups, validation
- Segment overrides: add, remove, toggle

---

## Chunk 5: Identities

### Goals
Admin view of identities in the current environment, with the ability to view traits and override feature states per identity.

### Tasks

#### 5.1 Identities List View (`IdentitiesView.vue`)
- Server-side paginated table: Identifier, Trait Count, Created At, Actions
- Search by identifier
- Click row → Identity Detail

#### 5.2 Identity Detail View (`src/components/identities/IdentityDetail.vue`)
Slide-out panel or separate route `/identities/{id}`:
- **Traits section**: 
  - Table of key/value traits
  - (Read-only in admin view; traits are set via SDK)
- **Feature Overrides section**:
  - Table of all features with current state for this identity
  - Columns: Feature Name, Environment Default, Identity Override (toggle + value), Override Active
  - "Add Override" — select feature → set enabled/value → `PUT /api/v1/environments/{apiKey}/identities/{id}/featurestates/{featureId}`
  - "Remove Override" → `DELETE /api/v1/environments/{apiKey}/identities/{id}/featurestates/{featureId}`
- **Danger Zone**: Delete identity (`DELETE /api/v1/environments/{apiKey}/identities/{id}`)

#### 5.3 Unit Tests
- IdentitiesView: pagination, search
- IdentityDetail: traits display, override toggle, delete identity

---

## Chunk 6: Audit Logs & Webhooks

### Goals
Read-only audit log viewer with filtering and webhook management at organization and environment level.

### Tasks

#### 6.1 Audit Logs View (`AuditLogsView.vue`)
Accessible from both org-level sidebar and within project/environment context:
- Server-side paginated table:
  - Columns: Timestamp, Actor, Action, Resource Type, Resource ID, Project, Environment
  - The `changes` JSON field expandable inline (show diff)
- Filters (Vuetify filter chips / inputs):
  - Date range picker (start/end)
  - Resource Type (multi-select: Feature, Environment, Segment, Identity, Organization, Project, ApiKey)
  - Action (multi-select: Created, Updated, Deleted)
- Scoped by current context: uses org/project/environment audit-logs endpoint appropriately

#### 6.2 Webhooks Management (`src/components/settings/WebhooksPanel.vue`)
Used in both Settings view (org-level) and Environment settings:
- Table: URL (truncated), Enabled toggle, Created At, Actions
- "Add Webhook" button → Webhook Dialog
- Enabled toggle → `PUT` to update `enabled` field
- Delete with confirmation

#### 6.3 Webhook Dialog (`src/components/settings/WebhookDialog.vue`)
- Fields: URL (required), Secret (optional, masked input), Enabled (toggle)
- On create: `POST /api/v1/organisations/{id}/webhooks` or `POST /api/v1/environments/{apiKey}/webhooks`
- On edit: `PUT`

#### 6.4 Webhook Delivery History (`src/components/settings/WebhookDeliveryHistory.vue`)
- Accessible via "View Deliveries" action on each webhook row
- Table: Timestamp, Status (chip: Success/Failed/Pending), Attempt #, HTTP Response Code
- `GET /api/v1/environments/{apiKey}/webhooks/{id}/deliveries`

#### 6.5 Unit Tests
- AuditLogsView: renders data, filter by resource type, filter by date range
- WebhooksPanel: list, toggle enabled, delete
- WebhookDialog: validation, create, edit
- WebhookDeliveryHistory: renders statuses

---

## Chunk 7: Settings, Permissions & API Keys

### Goals
Organization settings, project-level user permissions, and API key management.

### Tasks

#### 7.1 Settings View (`SettingsView.vue`)
Tabbed layout:

**Tab: Organization**
- Edit organization name → `PUT /api/v1/organisations/{id}`
- Organization webhook management (reuse `WebhooksPanel` component from Chunk 6)
- Audit log link (scoped to org)

**Tab: Project**
- Edit project name and `hideDisabledFlags` toggle → `PUT /api/v1/projects/{id}`
- User Permissions table:
  - Columns: User ID, Is Admin, Permissions (chips)
  - "Add User" button → `POST /api/v1/projects/{id}/user-permissions`
  - Edit permissions inline → `PUT /api/v1/projects/{id}/user-permissions/{userId}`
  - Remove user → `DELETE /api/v1/projects/{id}/user-permissions/{userId}`
  - Permission checkboxes: ViewProject, CreateFeature, EditFeature, DeleteFeature, CreateEnvironment, EditEnvironment, DeleteEnvironment, CreateSegment, EditSegment, DeleteSegment, ManageWebhooks, ViewAuditLog

**Tab: Environments**
- List of environments for current project
- Create environment: name input → `POST /api/v1/environments`
- Clone environment button → dialog asking for new name → `POST /api/v1/environments/{apiKey}/clone`
- Rename environment → `PUT /api/v1/environments/{apiKey}`
- Delete environment (with confirmation) → `DELETE`
- Environment webhook management (reuse `WebhooksPanel`)

**Tab: API Keys**
- Table: Name, Prefix (e.g. `org-key-ab`), Active, Expires At, Created At, Actions (Delete)
- "Create API Key" button → dialog:
  - Name field (required)
  - Expiry date picker (optional)
  - `POST /api/v1/organisations/{id}/api-keys`
  - On success: show `rawKey` once in a copy-to-clipboard dialog with warning "This will not be shown again"
- Delete key → `DELETE`

#### 7.2 Profile View (`ProfileView.vue`)
- Display name and email (read-only for now; no user update endpoint exists in current API)
- Logout button → calls auth store `logout()` → redirect to `/login`
- Display current organization memberships

#### 7.3 Organization Members Panel
In Organization tab:
- List members: `GET /api/v1/organisations/{id}/users` → table of userId + role
- Invite user: `POST /api/v1/organisations/{id}/users/invite` (userId + role)
- Remove member: `DELETE /api/v1/organisations/{id}/users/{userId}`

#### 7.4 Unit Tests
- API key creation: shows raw key once, copy to clipboard
- Permission editor: checkboxes update correctly, admin flag overrides individual permissions
- Environment settings: clone, rename, delete
- Profile: logout flow

---

## Shared Components & Utilities

### `src/components/common/`
- `ConfirmDialog.vue` — reusable confirm dialog (message, confirm label, danger variant)
- `PaginatedTable.vue` — wraps `v-data-table-server` with standard pagination controls
- `CopyTextField.vue` — input with copy-to-clipboard button (used for API keys, env keys)
- `StatusChip.vue` — colored chip for Success/Failed/Pending/Enabled/Disabled
- `DateRangePicker.vue` — reusable date range filter input
- `EmptyState.vue` — centered icon + message + action button for empty lists

### `src/composables/`
- `usePagination.ts` — manages page/pageSize state and server-side data fetching
- `useConfirm.ts` — returns a promise-based confirm dialog trigger
- `useNotifications.ts` — wraps Vuetify `v-snackbar` for success/error toasts

### `src/types/api.ts`
Complete TypeScript interfaces for all API response and request models.

---

## File Structure Target

```
src/admin/src/
├── api/
│   ├── client.ts              # Axios instance + interceptors
│   ├── auth.ts
│   ├── organizations.ts
│   ├── projects.ts
│   ├── environments.ts
│   ├── features.ts
│   ├── featureStates.ts
│   ├── segments.ts
│   ├── tags.ts
│   ├── identities.ts
│   ├── auditLogs.ts
│   ├── webhooks.ts
│   └── apiKeys.ts
├── stores/
│   ├── auth.ts
│   └── context.ts
├── types/
│   └── api.ts
├── composables/
│   ├── usePagination.ts
│   ├── useConfirm.ts
│   └── useNotifications.ts
├── components/
│   ├── common/
│   │   ├── ConfirmDialog.vue
│   │   ├── PaginatedTable.vue
│   │   ├── CopyTextField.vue
│   │   ├── StatusChip.vue
│   │   ├── DateRangePicker.vue
│   │   └── EmptyState.vue
│   ├── nav/
│   │   ├── OrgSelector.vue
│   │   ├── ProjectSelector.vue
│   │   └── EnvironmentTabBar.vue
│   ├── features/
│   │   ├── FeatureDialog.vue
│   │   ├── FeatureDetail.vue
│   │   ├── FeatureValueEditor.vue
│   │   └── TagsChipInput.vue
│   ├── segments/
│   │   ├── SegmentEditor.vue
│   │   ├── RuleGroupEditor.vue
│   │   └── ConditionEditor.vue
│   ├── identities/
│   │   └── IdentityDetail.vue
│   └── settings/
│       ├── WebhooksPanel.vue
│       ├── WebhookDialog.vue
│       ├── WebhookDeliveryHistory.vue
│       └── PermissionsEditor.vue
├── views/
│   ├── LoginView.vue
│   ├── RegisterView.vue
│   ├── DashboardView.vue
│   ├── FeaturesView.vue
│   ├── SegmentsView.vue
│   ├── IdentitiesView.vue
│   ├── AuditLogsView.vue
│   ├── SettingsView.vue
│   └── ProfileView.vue
├── plugins/
│   └── vuetify.ts
├── router/
│   └── index.ts
├── App.vue
└── main.ts
```

---

## Build Order Summary

| Chunk | Description | Depends On |
|-------|-------------|------------|
| 1 | Foundation: API client, Auth, Pinia stores, Login/Register | — |
| 2 | Navigation, context selectors, route guards, Dashboard | Chunk 1 |
| 3 | Feature Flags (list, CRUD, env state toggles, tags) | Chunk 2 |
| 4 | Segments (list, rule builder, overrides) | Chunk 3 |
| 5 | Identities (list, detail, per-identity overrides) | Chunk 3 |
| 6 | Audit Logs & Webhooks | Chunk 2 |
| 7 | Settings, Permissions, API Keys, Profile | Chunk 2 |

Each chunk can be built and committed independently after Chunk 2 is complete.
