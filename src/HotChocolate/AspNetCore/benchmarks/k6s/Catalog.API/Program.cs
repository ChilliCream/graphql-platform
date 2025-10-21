using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddDbContextPool<CatalogContext>(o =>
    {
        var connectionString = builder.Configuration.GetConnectionString("catalog-db");

        // Build a new connection string with pool settings
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            MaxPoolSize = 400,
            MinPoolSize = 10,
            ConnectionIdleLifetime = 300,
            ConnectionPruningInterval = 10
        };

        o.UseNpgsql(connectionStringBuilder.ConnectionString);
    })
    .AddMigration<CatalogContext, CatalogContextSeed>();

builder.Services
    .AddScoped<BrandService>()
    .AddScoped<ProductService>();

builder
    .AddGraphQL()
    .AddDefaultBatchDispatcher(
        new HotChocolate.Fetching.BatchDispatcherOptions
        {
            EnableParallelBatches = false,
            MaxParallelBatches = 4,
            MaxBatchWaitTimeUs = 50_000
        })
    .AddDiagnosticEventListener<DataLoaderEvents>()
    .AddCatalogTypes()
    .AddPagingArguments()
    .AddQueryContext()
    .AddSorting()
    .AddFiltering();

var app = builder.Build();

app.MapGraphQL();

app.RunWithGraphQLCommands(args);
