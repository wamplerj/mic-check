# MicCheck

Open source feature flag management platform. Manage projects, environments, feature flags, segments, and identities across your apps.

Built with .NET (API) and Vue.js + Vuetify (admin UI).

## Structure

```
src/
  api/MicCheck.Api/        .NET API — REST endpoints, organized by feature (Features, Projects, Environments,
                            Segments, Identities, Organizations, Webhooks, Audit, Users)
  admin/                    Vue.js + Vuetify admin SPA
  MicCheck.AppHost/         .NET Aspire orchestration host for local dev
  MicCheck.ServiceDefaults/ Shared .NET Aspire service defaults (telemetry, health checks)
tests/
  api/MicCheck.Api.Tests.Unit/         Unit tests
  api/MicCheck.Api.Tests.Integration/  Integration tests
docs/
  admin/                    Admin implementation plans/output docs
  api/                      API implementation plans/output docs
```

Code in the API is organized by feature area (e.g. `Features`, `Segments`, `Identities`) rather than by technical layer.

## Running locally

`docker-compose.yml` provides supporting services. `MicCheck.AppHost` (.NET Aspire) orchestrates the API and admin app for local development.

```sh
./dev-build.sh
```

## Testing

```sh
dotnet test
```

## License

MIT — see [LICENSE](LICENSE).
