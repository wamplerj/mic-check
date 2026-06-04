# MicCheck.Api.Tests.Integration

Black-box integration tests. Targets a running MicCheck.Api instance by URL and seeds/cleans data directly in the Postgres database.

## Running against an environment

Two settings are required: `BaseUrl` and `DbConnectionString`.

### Via .runsettings (recommended for local + CI)

```
dotnet test --settings local.runsettings
```

Copy `local.runsettings` to e.g. `staging.runsettings` and override the values for other environments.

### Via command-line

```
dotnet test -- TestRunParameters.Parameter\(name=\"BaseUrl\",value=\"https://staging.example.com\"\) ^
             TestRunParameters.Parameter\(name=\"DbConnectionString\",value=\"Host=...\"\)
```

### Via environment variables (overrides .runsettings)

```
MICCHECK_API_BASE_URL=...
MICCHECK_DB_CONNECTION_STRING=...
MICCHECK_ACCEPT_ANY_SERVER_CERTIFICATE=true|false
```

## Patterns

- Tests seed their own data **directly in the database**, never through the API.
- Per-test data is seeded in `[SetUp]`. Reusable cross-test data goes in `[OneTimeSetUp]`.
- Cleanup runs in `[TearDown]` / `[OneTimeTearDown]`, also directly in the database.
- HTTP requests are defined in `HttpFiles/*.http` and executed via `FlagApiHttpClient`.
- Assertion failure messages include a fresh snapshot of the relevant rows in the database.
