var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");

var database = postgres.AddDatabase("catalog-db");

builder
    .AddProject<Projects.eShop_Catalog_API>("catalog-api")
    .WithReference(database)
    .WaitFor(postgres);

builder.Build().Run();
