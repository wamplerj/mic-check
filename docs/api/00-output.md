# MicCheck.Api ASP.NET Project — Implementation Output

All source files have been created. The `dotnet` CLI is not on the PATH in the current shell session, so the build and tests could not be run. See setup instructions below.

## Getting Started

```bash
# If dotnet is not on your PATH, install the SDK:
wget https://dot.net/v1/dotnet-install.sh && bash dotnet-install.sh --channel 10.0
source ~/.bashrc

# Then from the repo root:
cd /home/james/src/mic-check
dotnet build MicCheck.slnx   # 0 errors expected
dotnet test MicCheck.slnx    # 7 tests expected to pass
```

---

## Files Created

### Solution (`/`)
- `MicCheck.slnx` — XML solution file with `/src/` and `/tests/` solution folders

### API Project (`src/api/MicCheck.Api/`)
- `MicCheck.Api.csproj` — `net10.0`, nullable enabled, LangVersion latest
- `Program.cs` — OpenAPI + Scalar UI + JWT bearer auth + `ITokenService` DI registration
- `appsettings.json` — JWT config (`SecretKey`, `Issuer`, `Audience`, `ExpiryMinutes`)
- `appsettings.Development.json` — debug-level logging for development
- `Auth/TokenRequest.cs` — `record TokenRequest(string Username, string Password)`
- `Auth/TokenResponse.cs` — `record TokenResponse(string Token, DateTime ExpiresAt)`
- `Auth/ITokenService.cs` — service interface
- `Auth/TokenService.cs` — JWT generation using `System.IdentityModel.Tokens.Jwt`
- `Auth/AuthEndpoints.cs` — `POST /auth/token`, 400 on empty credentials, 200 + token on success

### Test Project (`tests/api/MicCheck.Api.Tests.Unit/`)
- `MicCheck.Api.Tests.Unit.csproj` — NUnit 4.5.1 + Moq 4.20.72 + `Microsoft.AspNetCore.Mvc.Testing`
- `Auth/TokenServiceTests.cs` — 4 BDD tests directly against `TokenService`
- `Auth/AuthEndpointsTests.cs` — 3 BDD tests via `WebApplicationFactory<Program>` with mocked `ITokenService`

---

## Stack

| Concern | Technology |
|---|---|
| Framework | .NET 10 (`net10.0`) |
| API style | ASP.NET Minimal API |
| OpenAPI document | `Microsoft.AspNetCore.OpenApi` 10.0.5 |
| OpenAPI UI | `Scalar.AspNetCore` 2.5.14 (available at `/scalar/v1` in Development) |
| JWT | `System.IdentityModel.Tokens.Jwt` 8.16.0 + `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.5 |
| Test framework | NUnit 4.5.1 + NUnit3TestAdapter 6.2.0 + Microsoft.NET.Test.Sdk 18.3.0 |
| Mocking | Moq 4.20.72 |

---

## Endpoints

| Method | Path | Description |
|---|---|---|
| `POST` | `/auth/token` | Returns a JWT for the provided username/password |
| `GET` | `/openapi/v1.json` | OpenAPI document (Development only) |
| `GET` | `/scalar/v1` | Scalar API UI (Development only) |

---

## Test Cases

| Class | Test | Coverage |
|---|---|---|
| `TokenServiceTests` | `WhenAUsernameIsProvided_ThenATokenIsReturned` | Token is non-empty |
| `TokenServiceTests` | `WhenATokenIsGenerated_ThenItContainsTheUsernameAsSubjectClaim` | JWT `sub` claim |
| `TokenServiceTests` | `WhenATokenIsGenerated_ThenItHasAFutureExpiry` | Expiry is in the future |
| `TokenServiceTests` | `WhenATokenIsGenerated_ThenItHasTheConfiguredIssuer` | JWT `iss` claim |
| `AuthEndpointsTests` | `WhenUsernameIsEmpty_ThenBadRequestIsReturned` | 400 on empty username |
| `AuthEndpointsTests` | `WhenPasswordIsEmpty_ThenBadRequestIsReturned` | 400 on empty password |
| `AuthEndpointsTests` | `WhenValidCredentialsAreProvided_ThenOkIsReturnedWithToken` | 200 + token body |
