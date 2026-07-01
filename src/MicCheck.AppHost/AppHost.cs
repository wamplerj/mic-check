var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres").WithDataVolume("miccheck-pgdata").WithPgAdmin();

var miccheckDb = postgres.AddDatabase("miccheck");

var api = builder.AddProject<Projects.MicCheck_Api>("api").WithReference(miccheckDb).WaitFor(miccheckDb);

builder.AddViteApp("admin", "../admin", "serve")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
