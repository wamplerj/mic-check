// ─── Users ────────────────────────────────────────────────────────────────────

export interface UserProfileResponse {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  createdAt: string;
  lastLoginAt: string | null;
}

export interface UpdateProfileRequest {
  firstName: string;
  lastName: string;
  email: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

// ─── Auth ─────────────────────────────────────────────────────────────────────

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  organizationName: string;
}

export interface TokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface RefreshRequest {
  token: string;
}

export interface LogoutRequest {
  token: string;
}

// ─── Pagination ───────────────────────────────────────────────────────────────

export interface PaginatedResponse<T> {
  count: number;
  next: string | null;
  previous: string | null;
  results: T[];
}

// ─── Organizations ────────────────────────────────────────────────────────────

export interface OrganizationResponse {
  id: number;
  name: string;
  createdAt: string;
  isPrimary: boolean;
}

export interface CreateOrganizationRequest {
  name: string;
}

export interface UpdateOrganizationRequest {
  name: string;
}

export interface OrganizationMemberResponse {
  userId: number;
  firstName: string;
  lastName: string;
  email: string;
  role: 'Admin' | 'User';
  lastLoginAt: string | null;
}

export interface InviteUserRequest {
  userId: number;
  role: 'Admin' | 'User';
}

export interface InviteByEmailEntry {
  email: string;
  role: 'Admin' | 'User';
}

export interface InviteUsersByEmailRequest {
  invites: InviteByEmailEntry[];
}

export interface InviteByEmailResult {
  email: string;
  success: boolean;
  error: string | null;
}

export interface InviteTokenResponse {
  token: string;
}

// ─── API Keys ─────────────────────────────────────────────────────────────────

export interface CreateApiKeyRequest {
  name: string;
  expiresAt?: string | null;
}

export interface CreateApiKeyResponse {
  id: number;
  name: string;
  rawKey: string;
  prefix: string;
  expiresAt: string | null;
}

export interface ApiKeyResponse {
  id: number;
  name: string;
  prefix: string;
  isActive: boolean;
  expiresAt: string | null;
  createdAt: string;
}

// ─── Projects ─────────────────────────────────────────────────────────────────

export interface ProjectResponse {
  id: number;
  name: string;
  organizationId: number;
  hideDisabledFlags: boolean;
  createdAt: string;
}

export interface CreateProjectRequest {
  organizationId: number;
  name: string;
}

export interface UpdateProjectRequest {
  name: string;
  hideDisabledFlags: boolean;
}

export type ProjectPermission =
  | 'ViewProject'
  | 'CreateFeature'
  | 'EditFeature'
  | 'DeleteFeature'
  | 'CreateEnvironment'
  | 'EditEnvironment'
  | 'DeleteEnvironment'
  | 'CreateSegment'
  | 'EditSegment'
  | 'DeleteSegment'
  | 'ManageWebhooks'
  | 'ViewAuditLog';

export interface UserPermissionResponse {
  userId: number;
  projectId: number;
  isAdmin: boolean;
  permissions: ProjectPermission[];
}

export interface SetUserPermissionsRequest {
  userId: number;
  isAdmin: boolean;
  permissions: ProjectPermission[];
}

// ─── Environments ─────────────────────────────────────────────────────────────

export interface EnvironmentResponse {
  id: number;
  name: string;
  apiKey: string;
  projectId: number;
  createdAt: string;
}

export interface CreateEnvironmentRequest {
  projectId: number;
  name: string;
}

export interface UpdateEnvironmentRequest {
  name: string;
}

export interface CloneEnvironmentRequest {
  name: string;
}

// ─── Features ─────────────────────────────────────────────────────────────────

export type FeatureType = 'STANDARD' | 'MULTIVARIATE';

export interface FeatureResponse {
  id: number;
  name: string;
  type: FeatureType;
  initialValue: string | null;
  description: string | null;
  defaultEnabled: boolean;
  projectId: number;
  createdAt: string;
}

export interface CreateFeatureRequest {
  name: string;
  type: FeatureType;
  initialValue?: string | null;
  description?: string | null;
}

export interface UpdateFeatureRequest {
  name: string;
  description?: string | null;
}

export interface PatchFeatureRequest {
  name?: string | null;
  description?: string | null;
  defaultEnabled?: boolean | null;
}

// ─── Feature States ───────────────────────────────────────────────────────────

export interface FeatureStateResponse {
  id: number;
  featureId: number;
  environmentId: number;
  identityId: number | null;
  featureSegmentId: number | null;
  enabled: boolean;
  value: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface UpdateFeatureStateRequest {
  enabled: boolean;
  value?: string | null;
}

export interface PatchFeatureStateRequest {
  enabled?: boolean | null;
  value?: string | null;
}

// ─── Segments ─────────────────────────────────────────────────────────────────

export type SegmentConditionOperator =
  | 'Equal'
  | 'NotEqual'
  | 'Contains'
  | 'NotContains'
  | 'Regex'
  | 'GreaterThan'
  | 'GreaterThanOrEqual'
  | 'LessThan'
  | 'LessThanOrEqual'
  | 'IsTrue'
  | 'IsFalse'
  | 'In'
  | 'NotIn'
  | 'IsSet'
  | 'IsNotSet'
  | 'PercentageSplit'
  | 'ModuloValueDivisorRemainder';

export type SegmentRuleType = 'All' | 'Any';

export interface SegmentCondition {
  id?: number;
  property: string;
  operator: SegmentConditionOperator;
  value: string;
}

export interface SegmentRule {
  id?: number;
  type: SegmentRuleType;
  conditions: SegmentCondition[];
  childRules?: SegmentRule[];
}

export interface SegmentResponse {
  id: number;
  name: string;
  projectId: number;
  createdAt: string;
  rules: SegmentRule[];
}

export interface CreateSegmentRequest {
  name: string;
  rules: SegmentRule[];
}

export interface UpdateSegmentRequest {
  name: string;
  rules: SegmentRule[];
}

export interface SegmentSummaryResponse {
  id: number;
  name: string;
}

export interface FeatureSegmentResponse {
  id: number;
  featureId: number;
  segmentId: number;
  segmentName: string;
  environmentId: number;
  priority: number;
  enabled: boolean | null;
  value: string | null;
}

export interface CreateFeatureSegmentRequest {
  segmentId: number;
  priority: number;
  enabled: boolean;
  value: string | null;
}

export interface UpdateFeatureSegmentRequest {
  priority: number;
  enabled: boolean;
  value: string | null;
}

// ─── Tags ─────────────────────────────────────────────────────────────────────

export interface TagResponse {
  id: number;
  label: string;
  color: string;
  projectId: number;
}

export interface CreateTagRequest {
  label: string;
  color: string;
}

// ─── Audit Logs ───────────────────────────────────────────────────────────────

export interface AuditLogResponse {
  id: number;
  resourceType: string;
  resourceId: string;
  action: string;
  changes: string | null;
  organizationId: number;
  projectId: number | null;
  environmentId: number | null;
  actorUserId: number | null;
  actorUserName: string | null;
  createdAt: string;
}

export interface AuditLogFilter {
  resourceType?: string | null;
  action?: string | null;
  from?: string | null;
  to?: string | null;
  page?: number;
  pageSize?: number;
}

// ─── Webhooks ─────────────────────────────────────────────────────────────────

export type WebhookScope = 'Organization' | 'Environment';
export type WebhookDeliveryStatus = 'Pending' | 'Success' | 'Failed';

export interface WebhookResponse {
  id: number;
  url: string;
  secret: string | null;
  scope: WebhookScope;
  enabled: boolean;
  environmentId: number | null;
  organizationId: number | null;
  createdAt: string;
}

export interface CreateWebhookRequest {
  url: string;
  secret?: string | null;
  enabled: boolean;
}

export interface UpdateWebhookRequest {
  url: string;
  secret?: string | null;
  enabled: boolean;
}

export interface WebhookDeliveryLogResponse {
  id: number;
  webhookId: number;
  eventType: string;
  success: boolean;
  responseStatusCode: number | null;
  responseBody: string | null;
  errorMessage: string | null;
  attemptNumber: number;
  attemptedAt: string;
  duration: string;
}

// ─── Identities ───────────────────────────────────────────────────────────────

export interface TraitResponse {
  key: string;
  value: string;
}

export interface UpsertTraitRequest {
  value: string;
}

export interface IdentityResponse {
  id: number;
  identifier: string;
  environmentId: number;
  traits: TraitResponse[];
  createdAt: string;
}

export interface CreateIdentityRequest {
  identifier: string;
}

export interface TraitRequest {
  traitKey: string;
  traitValue: string | number | boolean | null;
}

export interface IdentityRequest {
  identifier: string;
  traits?: TraitRequest[] | null;
}

export interface IdentityWithFlagsResponse {
  traits: TraitResponse[];
  flags: FlagResponse[];
}

// ─── Flags (SDK) ──────────────────────────────────────────────────────────────

export interface FlagFeatureInfo {
  id: number;
  name: string;
  type: FeatureType;
}

export interface FlagResponse {
  id: number;
  feature: FlagFeatureInfo;
  enabled: boolean;
  featureStateValue: string | null;
}

// ─── Feature Usage ─────────────────────────────────────────────────────────────

export interface TopFeatureUsage {
  featureId: number;
  featureName: string;
  count: number;
}

export interface DailyUsage {
  date: string;
  totalCount: number;
  features: TopFeatureUsage[];
}

export interface DashboardUsageResponse {
  topFeaturesLastDay: TopFeatureUsage[];
  dailyUsage: DailyUsage[];
}

// ─── Errors ───────────────────────────────────────────────────────────────────

export interface ApiValidationError {
  errors: Record<string, string[]>;
}

export interface ApiError {
  error: string;
}
