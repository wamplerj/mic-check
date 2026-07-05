# Plan: Integration + Playwright smoke suite that doubles as post-deploy QA validation

## Context

Today the repo has **one** integration test (`FlagEnabledTests` — seeds an enabled flag in Postgres, hits `GET /api/v1/flags`, asserts enabled) and **zero** browser/e2e tests. Unit coverage is already deep for the risky logic — flag/feature evaluation, segment evaluation (28 tests), webhooks, auth token/API-key/permission logic, audit. Per the 80–90% unit / 10–20% integration rule, we do **not** re-test that logic at integration. Instead we add a thin layer covering the **most important, most visible, real-Postgres-backed** flows that unit tests (all InMemory EF) cannot prove, and wire it to run **after `deploy-qa`** so every QA deploy is validated end-to-end.

Decisions locked with the user:
- **E2E hits the real deployed stack** (not mocked) — `http://localhost:${QA_ADMIN_PORT}` in CI, `http://localhost:5173` local dev.
- **Add a fixed dev/QA seed admin user** (QA DB is wiped `down -v` each deploy and reseeded on startup; seeding is already `IsDevelopment()`-guarded and QA runs `ASPNETCORE_ENVIRONMENT=Development`).
- **Add a post-deploy `smoke-qa` CI job** running the API integration suite + Playwright against QA.

Key constraints discovered:
- Integration harness is **black-box**: HTTP to a running API + **direct Npgsql** seed/cleanup (`Common/TestDatabase.cs`). It spins nothing up; needs a live API **and** direct DB reachability.
- QA compose (`deploy/qa/docker-compose.qa.yml`) publishes **only** the admin port; API + Postgres are network-internal. The smoke job runs on the **same self-hosted `qa` host** (localhost). So the DB port must be reachable from the host for direct seeding → publish Postgres bound to `127.0.0.1` on the QA host.
- `/health` + `/alive` + Scalar/OpenAPI are **Development-only** (`ServiceDefaults/Extensions.cs:113`). QA is Development so `/health` works there, but smoke assertions should lean on real auth/flag reads, not `/health`.
- Seeder (`Data/DatabaseSeeder.cs`) currently creates Org → Project → 3 Environments with **random** `env-{guid}` API keys and **no user**. For HTTP-only smoke we make the admin user + one env key **deterministic**.

## Implementation — 4 independently buildable/committable chunks

### Chunk A — Deterministic dev/QA seed admin user + fixed env key
Files: `src/api/MicCheck.Api/Data/DatabaseSeeder.cs`, unit test in `tests/api/MicCheck.Api.Tests.Unit/Data/`.

- Inject `IPasswordHasher<User>` into `DatabaseSeeder` (mirror `AuthService.RegisterAsync` at `Common/Security/Authorization/AuthService.cs:54-82`: create `User{ IsActive=true, PasswordHash=hasher.HashPassword(...) }`, then `OrganizationUser{ Role=OrganizationRole.Admin }` linking it to the seeded `Default` org).
  - Creds: `admin@miccheck.local` / `MicCheckQa!2026` (login verify has no complexity rule, so any value works; keep it in one shared constants spot referenced by tests).
- Give the seeded **Development** environment a **deterministic** `ApiKey` (e.g. `env-qa-development`) instead of a random guid; leave Staging/Production random. Lets HTTP-only smoke read `/flags` with a known key without direct DB access.
- Keep the existing `if (Organizations.AnyAsync) return;` idempotency guard — user + env are created in the same first-run block. No prod risk: invocation is already gated by `app.Environment.IsDevelopment()` (`Program.cs:160-166`).
- Unit test: seed against InMemory EF twice → asserts admin user exists once, is Admin on Default org, password verifies, Development env key is the fixed value.

### Chunk B — Expand API integration suite (`tests/api/MicCheck.Api.Tests.Integration`)
Reuse the existing harness verbatim: `Common/FlagApiHttpClient.cs` (drives `###`-delimited `.http` files with `{{var}}` substitution), `Common/TestDatabase.cs` (Npgsql seed/cleanup + `SnapshotRowsAsync` for failure dumps), `Common/IntegrationTestSettings.cs` (env-var / `.runsettings` config), and the `FlagSeed` INSERT…RETURNING + cascade-delete-by-Organization pattern (`Flags/FlagSeed.cs`). Keep DTOs redefined locally (no `ProjectReference` to the API) per the current convention. New `.http` files under `HttpFiles/` are auto-copied (`csproj` `CopyToOutputDirectory=PreserveNewest`).

Add these high-value scenarios (each the real Postgres wire contract, not re-covered logic):
1. **Flag disabled** — seed `Enabled=false`; assert `/flags` reports the feature `Enabled=false` (complements the existing enabled test; cheap variant of `FlagSeed`).
2. **Environment-document bootstrap** — new `HttpFiles/EnvironmentDocument.http` (`GET /api/v1/environment-document`, `X-Environment-Key`); assert 200 and the seeded feature state + project segment are present (shape: `Environments/EnvironmentDocumentResponse.cs`). This is the edge/client-SDK bootstrap path — high blast radius, untested.
3. **Identity override precedence** — new seed adding an identity-override FeatureState (and/or a segment override) + `HttpFiles/Identity.http` (`POST /api/v1/identity` with identifier, `X-Environment-Key`); assert the evaluated value reflects identity > segment > env-default precedence (`Features/FeatureEvaluationService.cs:43-146`). Richest runtime logic against real data.
4. **Auth liveness** — new `HttpFiles/Auth.http` (`POST /api/v1/auth/login`): bad creds → 401; the deterministic seed creds → 200 + tokens. Proves app + DB + auth are up without depending on `/health`.
5. **Unauthorized guard** — `GET /api/v1/flags` with no / wrong `X-Environment-Key` → 401 (validates `EnvironmentKeyAuthenticationHandler`).

### Chunk C — Playwright admin e2e suite (new)
Location `src/admin/e2e/` (distinct from Jest, which grabs `*.spec.ts` under `tests/`; name specs `*.e2e.ts` and set Jest `roots`/Playwright `testDir` so they never collide). Add dev deps `@playwright/test`, a `playwright.config.ts` (`testDir: 'e2e'`, `baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:5173'`, chromium project, `globalSetup` for auth). Add `package.json` scripts: `"e2e": "playwright test"`, `"e2e:install": "playwright install --with-deps chromium"`.

The app is already richly instrumented with `data-testid` — drive real UI, no selectors guesswork. Auth: `globalSetup` logs in once via the real `/login` form (`login-form`/`email-input`/`password-input`/`login-submit`) with the Chunk-A seed creds and saves `storageState` for reuse.

Journeys (ordered most→least important; the seed provides `Default` org, `My Project`, and `Development/Staging/Production` envs, so context is selectable without creating anything):
1. **Login** — valid creds redirect off `/login`; invalid creds surface `login-error` (`views/LoginView.vue`).
2. **App shell + nav** — sidebar renders and routes (Dashboard/Features/Segments/Identities/Audit Logs/Settings) navigate (`layouts/components/NavItems.vue`).
3. **Select project + environment context** — `project-selector`/`project-select` + `environment-tab-bar`/`env-select` (`components/nav/ProjectSelector.vue`, `EnvironmentTabBar.vue`); required before feature screens work (context persists to localStorage via `stores/context.ts`).
4. **View feature flags** — `features-table` renders for the selected project (`views/FeaturesView.vue`).
5. **Create a feature flag** — `create-feature-btn` → `FeatureDialog` (`feature-name-input`, `feature-type-select`, `feature-dialog-save`) → row appears (`components/features/FeatureDialog.vue`, API `api/features.ts`). Exercises full write path to real DB.
6. **Toggle a feature flag** — row switch `toggle-${id}` (or `feature-enabled-toggle` in `FeatureDetail.vue`); reload → state persisted (validates `api/featureStates.ts` → real Postgres).

Secondary/optional (add if cheap): create project, create environment via the sidebar dialogs.

### Chunk D — CI post-deploy smoke job
Files: `deploy/qa/docker-compose.qa.yml`, `scripts/ci/smoke-qa.sh` (new), `.github/workflows/ci.yml`.

- **DB reachability**: add `ports: ["127.0.0.1:55432:5432"]` to the `db` service in the QA compose (localhost-bound only — the runner host is the sole consumer; not exposed off-box). Lets the integration harness seed Postgres directly.
- New `scripts/ci/smoke-qa.sh` (matches the repo's "every CI step is a reproducible script" convention):
  - API integration: `dotnet test tests/api/MicCheck.Api.Tests.Integration -c Release --logger trx` with env `MICCHECK_API_BASE_URL=http://localhost:${QA_ADMIN_PORT}` and `MICCHECK_DB_CONNECTION_STRING=Host=localhost;Port=55432;Database=miccheck;Username=miccheck;Password=password`.
  - Playwright: `npm --prefix src/admin ci` (or reuse install), `npm --prefix src/admin run e2e:install`, then `E2E_BASE_URL=http://localhost:${QA_ADMIN_PORT} npm --prefix src/admin run e2e`.
- `.github/workflows/ci.yml`: add job `smoke-qa` (`needs: deploy-qa`, `runs-on: [self-hosted, qa]`, env `QA_ADMIN_PORT: ${{ vars.QA_ADMIN_PORT }}`) that checks out (plain-git step, matching `deploy-qa`) and runs `./scripts/ci/smoke-qa.sh`. Deploy is now gated by real end-to-end validation.

## Files touched (summary)
- Modify: `src/api/MicCheck.Api/Data/DatabaseSeeder.cs`, `deploy/qa/docker-compose.qa.yml`, `.github/workflows/ci.yml`, `src/admin/package.json`.
- Add (API tests): `HttpFiles/EnvironmentDocument.http`, `HttpFiles/Identity.http`, `HttpFiles/Auth.http`, new `*Tests.cs` + seed helpers under `Flags/` (or a new `Identities/`, `Environments/`, `Auth/` folder) in `tests/api/MicCheck.Api.Tests.Integration`.
- Add (unit): seeder test under `tests/api/MicCheck.Api.Tests.Unit/Data/`.
- Add (e2e): `src/admin/playwright.config.ts`, `src/admin/e2e/*.e2e.ts`, `src/admin/e2e/global-setup.ts`.
- Add (CI): `scripts/ci/smoke-qa.sh`.
- Per CLAUDE.md, also drop this plan at `docs/plans/api/integration-smoke_plan.md` and an `_output.md` summary after implementation.

## Verification
- **Chunk A**: `dotnet test tests/api/MicCheck.Api.Tests.Unit` (new seeder test green). Boot API locally (`docker compose up`), confirm login with seed creds returns tokens and the Development env key equals the fixed value.
- **Chunk B**: bring up local stack (Postgres on `:5432`, API on `:5000`), `dotnet test tests/api/MicCheck.Api.Tests.Integration --settings local.runsettings` — all new scenarios green; failure messages show live DB snapshots.
- **Chunk C**: `npm --prefix src/admin run serve` (API on :5000), `E2E_BASE_URL=http://localhost:5173 npm --prefix src/admin run e2e` — all journeys pass headed and headless.
- **Chunk D**: push to `build-runner-fix`; watch CI — `smoke-qa` runs after `deploy-qa`, both `dotnet test` and Playwright green against `http://localhost:${QA_ADMIN_PORT}`. Confirm a deliberately-broken deploy (e.g. bad DB creds) makes `smoke-qa` fail.
