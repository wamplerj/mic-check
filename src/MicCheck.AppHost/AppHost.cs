var builder = DistributedApplication.CreateBuilder(args);

// Pinned to match tests/api/MicCheck.Api.Tests.Integration/local.runsettings and
// src/admin's docker-compose defaults, so those fixed targets work whether the
// stack is run via `docker compose` or via this AppHost.
var postgresUser = builder.AddParameter("postgres-username", "miccheck");
var postgresPassword = builder.AddParameter("postgres-password", "password", secret: true);

var postgres = builder.AddPostgres("postgres", postgresUser, postgresPassword, port: 5432)
    .WithDataVolume("miccheck-pgdata")
    .WithPgAdmin();

var miccheckDb = postgres.AddDatabase("miccheck");

var api = builder.AddProject<Projects.MicCheck_Api>("api")
    .WithReference(miccheckDb)
    .WaitFor(miccheckDb)
    .WithHttpEndpoint(port: 5000, name: "http");

builder.AddViteApp("admin", "../admin", "serve")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(port: 5173, name: "http")
    .WithExternalHttpEndpoints();

builder.Build().Run();
