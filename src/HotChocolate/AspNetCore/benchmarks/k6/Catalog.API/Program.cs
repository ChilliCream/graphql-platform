using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Transport.Formatters;
using Npgsql;

var errorCount = 0;
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
            MaxBatchWaitTimeUs = 50_000
        })
    .AddDiagnosticEventListener<BenchmarkDataLoaderDiagnosticEventListener>()
    .AddCatalogTypes()
    .AddPagingArguments()
    .AddQueryContext()
    .AddSorting()
    .AddFiltering()
    .AddInstrumentation()
    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
    .UseRequest(next =>
    {
        return async context =>
        {
            await next(context);

            if (context.Result?.ExpectOperationResult() is { Errors.Count: > 0 } result)
            {
                var id = Interlocked.Increment(ref errorCount);
                using var buffer = new PooledArrayWriter();
                JsonResultFormatter.Indented.Format(result, buffer);
                await File.WriteAllBytesAsync(
                    $"/Users/michael/local/hc-4/src/HotChocolate/AspNetCore/benchmarks/k6Catalog.API/errors/{id}.json",
                    buffer.WrittenMemory);
            }
        };
    })
    .UseDefaultPipeline();

var app = builder.Build();

app.MapGraphQL();

app.RunWithGraphQLCommands(args);
