# Integration + Playwright smoke suite — implementation summary

Implemented per `integration-smoke_plan.md`. All chunks built, verified against a real local Postgres + API + Vite stack, and the two pre-existing test suites (243 API unit tests, 306 admin Jest tests) still pass.

## Chunk A — Deterministic dev/QA seed admin user + fixed env key
- `src/api/MicCheck.Api/Data/DatabaseSeeder.cs`: now also creates a seed admin user (`admin@miccheck.local` / `MicCheckQa!2026`, Admin role on the `Default` org) and gives the seeded `Development` environment a fixed API key (`env-qa-development`). Still gated behind `IsDevelopment()` in `Program.cs` and the existing idempotency guard.
- New unit tests: `tests/api/MicCheck.Api.Tests.Unit/Data/DatabaseSeederTests.cs` (5 tests).
- Verified live: booted the API against local Postgres, logged in via `POST /api/v1/auth/login` with the seed creds (200 + tokens), and read `/api/v1/flags` with the fixed Development key (200, empty array as expected).

## Chunk B — Expanded API integration suite
Added to `tests/api/MicCheck.Api.Tests.Integration`, reusing the existing black-box HTTP+Npgsql harness:
- `Flags/FlagDisabledTests.cs` — disabled-flag variant of the existing enabled test.
- `Flags/FlagsApiAuthorizationTests.cs` — missing/unknown environment key → 401.
- `Environments/EnvironmentDocumentTests.cs` + `HttpFiles/EnvironmentDocument.http` — SDK bootstrap document endpoint.
- `Identities/IdentityOverrideSeed.cs` + `Identities/IdentityOverrideTests.cs` + `HttpFiles/Identity.http` — identity-override-beats-environment-default precedence.
- `Auth/AuthSeed.cs` + `Auth/AuthTests.cs` + `HttpFiles/Auth.http` — login liveness (valid creds → tokens, bad password → 401). Added `Microsoft.Extensions.Identity.Core` package reference so the seed can hash a password with the same `PasswordHasher<T>` the API uses, without a `ProjectReference` to the API (kept the black-box convention).
- Fixed a latent substring bug (`[..40]` on a string shorter than 40 chars) copied into the new seeds; left the pre-existing `FlagSeed.cs` alone since none of its callers hit the short-string case.
- Verified live: all 8 tests pass against a real Postgres + `dotnet run` API (`dotnet test ... --logger "console;verbosity=normal"` → 8/8 passed).

## Chunk C — Playwright admin e2e suite
New `src/admin/e2e/` (kept out of Jest's `roots`/`testMatch`, no config change needed since `e2e/` isn't under `src/` or `tests/`):
- `playwright.config.ts`, `e2e/global-setup.ts` (logs in once via the real `/login` form with the seed creds, saves `storageState`).
- `e2e/login.e2e.ts`, `e2e/navigation.e2e.ts`, `e2e/context-selection.e2e.ts`, `e2e/features.e2e.ts` (view, create, toggle).
- `package.json`: added `@playwright/test` devDependency and `e2e` / `e2e:install` scripts.
- Two bugs found and fixed while running against the real app: sidebar nav items are clickable `div`s, not `<a>` links (fixed the locator); a newly-created feature's toggle was clicked before its feature-state fetch settled, causing Vuetify's switch to visually revert (added a `waitForLoadState('networkidle')`).
- Verified live: all 6 tests pass against the real Vite dev server + local API (`npx playwright test` → 6/6 passed).

## Chunk D — CI post-deploy smoke job
- `deploy/qa/docker-compose.qa.yml`: bound Postgres to `127.0.0.1:55432` on the QA host so the integration harness can seed/clean directly (off-box still unreachable).
- `scripts/ci/smoke-qa.sh` (new): runs the API integration suite and the Playwright suite against `http://localhost:${QA_ADMIN_PORT}`.
- `scripts/ci/lib.sh`: added `ensure_node()` (mirrors `ensure_dotnet()`) — **the self-hosted `qa` runner has no Node.js today** (confirmed by the existing "avoid actions/checkout, it's Node-based" comment in `deploy-qa.sh`), so `smoke-qa.sh` needed a way to bootstrap `npm`/`npx` the same way it already bootstraps `dotnet`.
- `.github/workflows/ci.yml`: new `smoke-qa` job, `needs: deploy-qa`, runs on `[self-hosted, qa]`, checks out via plain git (matching `deploy-qa`), runs `smoke-qa.sh`.

### Known follow-up risk (not resolved, flagging for the user)
`npm run e2e:install` runs `playwright install --with-deps chromium`, which needs root/passwordless-sudo to apt-install browser OS dependencies. This could not be verified against the actual self-hosted `qa` runner (only tested locally, where `--with-deps` failed for lack of a sudo TTY — the browser itself still installed fine and tests ran). If the `qa` runner also lacks passwordless sudo, the first CI run of `smoke-qa` may fail on that step; the fix would be a one-time manual `sudo npx playwright install-deps chromium` on the runner, or granting the CI user passwordless sudo for that command.

## Verification performed this session
- `dotnet test tests/api/MicCheck.Api.Tests.Unit` — 243/243 passed.
- `dotnet test tests/api/MicCheck.Api.Tests.Integration` against real local Postgres + API — 8/8 passed.
- `npm --prefix src/admin test` (Jest) — 306/306 passed, 28 suites, no collision with `e2e/`.
- `npx playwright test` (from `src/admin`) against real local Vite dev server + API — 6/6 passed.
- `docker compose -f deploy/qa/docker-compose.qa.yml config` — valid.
- `bash -n` on `smoke-qa.sh` and `lib.sh` — valid.

## Not yet done
- The `smoke-qa` CI job has not run on the actual self-hosted `qa` runner (no access to it from this session) — first real run should be watched, especially the Node bootstrap and the Playwright OS-deps risk above.
