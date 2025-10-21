var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithArgs("-c", "max_connections=500")
    .WithArgs("-c", "shared_buffers=512MB")
    .WithArgs("-c", "effective_cache_size=1GB")
    .WithArgs("-c", "maintenance_work_mem=128MB")
    .WithArgs("-c", "checkpoint_completion_target=0.9")
    .WithArgs("-c", "wal_buffers=16MB")
    .WithArgs("-c", "default_statistics_target=100")
    .WithArgs("-c", "random_page_cost=1.1")
    .WithArgs("-c", "work_mem=32MB");

var database = postgres.AddDatabase("catalog-db");

builder
    .AddProject<Projects.eShop_Catalog_API>("catalog-api")
    .WithReference(database)
    .WaitFor(postgres);

builder.Build().Run();
