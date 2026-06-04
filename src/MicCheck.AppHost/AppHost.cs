var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("miccheck-pgdata")
    .WithPgAdmin();

var miccheckDb = postgres.AddDatabase("miccheck");

builder.AddProject<Projects.MicCheck_Api>("api")
    .WithReference(miccheckDb)
    .WaitFor(miccheckDb);

builder.Build().Run();
