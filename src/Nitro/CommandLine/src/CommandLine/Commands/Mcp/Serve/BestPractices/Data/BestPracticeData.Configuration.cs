using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices;

internal static partial class BestPracticeData
{
    private static void AddConfigurationDocuments(List<BestPracticeDocument> docs)
    {
        docs.Add(
            new BestPracticeDocument
            {
                Id = "configuration-performance",
                Title = "Performance Configuration",
                Category = BestPracticeCategory.Configuration,
                Tags = ["hot-chocolate-16", "performance"],
                Styles = ["all"],
                Keywords = "performance tuning optimization throughput memory speed latency production cache warm-up compression",
                Abstract =
                    "Key configuration settings for production performance: execution engine thread pool, DataLoader batch size, response compression, query caching, and warm-up.",
                Body = """
                # Performance Configuration

                ## When to Use

                Apply performance configuration when preparing your Hot Chocolate server for production workloads. These settings optimize execution throughput, memory usage, and response times for high-traffic scenarios.

                Most applications work well with default settings. Tune these parameters when you observe specific performance bottlenecks or when your workload has unusual characteristics (very large responses, many concurrent subscriptions, etc.).

                ## Implementation

                ### Execution Engine Optimization

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes()
                    .ModifyRequestOptions(o =>
                    {
                        // Set execution timeout
                        o.ExecutionTimeout = TimeSpan.FromSeconds(30);
                    });
                ```

                ### DataLoader Configuration

                Configure DataLoader batch behavior:

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes()
                    .ModifyOptions(o =>
                    {
                        // Enable DataLoader diagnostics in development
                    });
                ```

                ### Query Caching

                Enable query document caching to avoid re-parsing the same queries:

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes()
                    .UseAutomaticPersistedQueryPipeline()
                    .AddInMemoryQueryStorage();
                ```

                ### Response Compression

                Enable compression at the ASP.NET Core level:

                ```csharp
                builder.Services.AddResponseCompression(options =>
                {
                    options.EnableForHttps = true;
                    options.MimeTypes = ResponseCompressionDefaults.MimeTypes
                        .Concat(["application/graphql-response+json"]);
                });

                var app = builder.Build();
                app.UseResponseCompression();
                app.MapGraphQL();
                ```

                ### Schema Warm-Up

                Pre-build the schema and executor at startup instead of on first request:

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes()
                    .InitializeOnStartup();
                ```

                ### Complete Performance Configuration

                ```csharp
                var builder = WebApplication.CreateBuilder(args);

                // Response compression
                builder.Services.AddResponseCompression(options =>
                {
                    options.EnableForHttps = true;
                });

                // DbContext with pooling
                builder.Services.AddDbContextPool<AppDbContext>(o =>
                    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

                // GraphQL
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddMutationType()
                    .AddTypes()
                    .AddFiltering()
                    .AddSorting()
                    .AddProjections()
                    .RegisterDbContextFactory<AppDbContext>()
                    .UseAutomaticPersistedQueryPipeline()
                    .AddInMemoryQueryStorage()
                    .SetPagingOptions(new PagingOptions
                    {
                        DefaultPageSize = 10,
                        MaxPageSize = 50,
                        IncludeTotalCount = false  // Disable unless needed — avoids COUNT queries
                    })
                    .ModifyRequestOptions(o =>
                    {
                        o.ExecutionTimeout = TimeSpan.FromSeconds(30);
                        o.IncludeExceptionDetails = false;
                    })
                    .InitializeOnStartup();

                var app = builder.Build();
                app.UseResponseCompression();
                app.MapGraphQL();
                app.Run();
                ```

                ### Projections

                Use `[UseProjection]` to only select the fields that were requested in the query:

                ```csharp
                [QueryType]
                public static class ProductQueries
                {
                    [UsePaging]
                    [UseProjection]
                    [UseFiltering]
                    [UseSorting]
                    public static IQueryable<Product> GetProducts(AppDbContext dbContext)
                        => dbContext.Products;
                }
                ```

                ## Anti-patterns

                **Disabling query caching:**

                ```csharp
                // BAD: Without caching, every request re-parses the query document
                // This wastes CPU on repeated queries
                ```

                **Including TotalCount on every paginated query:**

                ```csharp
                // BAD: IncludeTotalCount = true forces a COUNT(*) query
                // on every paginated request — expensive on large tables
                .SetPagingOptions(new PagingOptions
                {
                    IncludeTotalCount = true  // Only enable when UI needs it
                });
                ```

                **Not using DbContext pooling:**

                ```csharp
                // BAD: AddDbContext creates a new DbContext per scope
                // AddDbContextPool reuses instances from a pool
                builder.Services.AddDbContext<AppDbContext>(...);
                // Use AddDbContextPool for better performance under load
                ```

                **Eager loading all relations:**

                ```csharp
                // BAD: Loading all includes regardless of what the client requested
                public static IQueryable<Product> GetProducts(AppDbContext db)
                    => db.Products
                        .Include(p => p.Category)     // May not be requested
                        .Include(p => p.Reviews)      // May not be requested
                        .Include(p => p.Images);      // May not be requested
                // Use [UseProjection] or DataLoaders instead
                ```

                ## Key Points

                - Use `InitializeOnStartup()` to avoid cold-start latency on first request
                - Enable automatic persisted queries to cache parsed query documents
                - Use `AddDbContextPool` instead of `AddDbContext` for connection pooling
                - Disable `IncludeTotalCount` unless the UI specifically needs it
                - Use `[UseProjection]` to generate efficient SQL that only selects requested fields
                - Enable response compression for large GraphQL responses
                - Set execution timeouts to prevent long-running queries from consuming resources

                ## Related Practices

                - [configuration-server-setup] — For general server configuration
                - [security-production-hardening] — For security settings
                - [dataloader-basic] — For DataLoader optimization
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "configuration-server-setup",
                Title = "AddGraphQLServer() Configuration",
                Category = BestPracticeCategory.Configuration,
                Tags = ["hot-chocolate-16"],
                Styles = ["all"],
                Keywords = "setup configure bootstrap startup initialize register services builder program startup getting-started",
                Abstract =
                    "How to configure the Hot Chocolate GraphQL server via AddGraphQLServer(): type registration, request options, execution options, and environment-specific settings.",
                Body = """
                # AddGraphQLServer() Configuration

                ## When to Use

                Every Hot Chocolate application starts with `AddGraphQLServer()`. Understanding the configuration options is essential for setting up a correct, production-ready GraphQL server.

                This guide covers the most common configuration patterns: registering types, configuring middleware, setting request options, and environment-specific setup.

                ## Implementation

                ### Minimal Configuration

                ```csharp
                var builder = WebApplication.CreateBuilder(args);

                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes();

                var app = builder.Build();
                app.MapGraphQL();
                app.Run();
                ```

                ### Full Production Configuration

                ```csharp
                var builder = WebApplication.CreateBuilder(args);

                // Database
                builder.Services.AddDbContext<AppDbContext>(o =>
                    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

                // Authentication
                builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer();

                builder.Services.AddAuthorization();

                // GraphQL
                var graphql = builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddMutationType()
                    .AddSubscriptionType()
                    .AddTypes()
                    .AddFiltering()
                    .AddSorting()
                    .AddProjections()
                    .AddMutationConventions()
                    .AddAuthorization()
                    .AddGlobalObjectIdentification()
                    .RegisterDbContextFactory<AppDbContext>()
                    .AddInMemorySubscriptions()
                    .SetPagingOptions(new PagingOptions
                    {
                        DefaultPageSize = 10,
                        MaxPageSize = 50,
                        IncludeTotalCount = true
                    })
                    .ModifyRequestOptions(o =>
                    {
                        o.ExecutionTimeout = TimeSpan.FromSeconds(30);
                    });

                // Production-only settings
                if (!builder.Environment.IsDevelopment())
                {
                    graphql
                        .DisableIntrospection()
                        .AddMaxExecutionDepthRule(15);
                }

                var app = builder.Build();

                app.UseAuthentication();
                app.UseAuthorization();
                app.UseWebSockets();
                app.MapGraphQL();

                app.Run();
                ```

                ### Type Registration

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    // Root types
                    .AddQueryType()
                    .AddMutationType()
                    .AddSubscriptionType()
                    // Auto-discover all types with source generation attributes
                    .AddTypes()
                    // Or register individual types
                    .AddType<UserType>()
                    .AddType<OrderType>();
                ```

                ### Request and Execution Options

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .ModifyRequestOptions(o =>
                    {
                        o.ExecutionTimeout = TimeSpan.FromSeconds(30);
                        o.IncludeExceptionDetails = builder.Environment.IsDevelopment();
                    })
                    .SetPagingOptions(new PagingOptions
                    {
                        DefaultPageSize = 10,
                        MaxPageSize = 100,
                        IncludeTotalCount = true
                    });
                ```

                ### Multiple Schemas (Named Schemas)

                ```csharp
                builder.Services
                    .AddGraphQLServer("public")
                    .AddQueryType<PublicQuery>()
                    .AddTypes();

                builder.Services
                    .AddGraphQLServer("admin")
                    .AddQueryType<AdminQuery>()
                    .AddTypes()
                    .AddAuthorization();

                var app = builder.Build();
                app.MapGraphQL("/graphql", "public");
                app.MapGraphQL("/admin/graphql", "admin");
                ```

                ### Error Handling Configuration

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes()
                    .AddErrorFilter<GraphQLErrorFilter>()
                    .ModifyRequestOptions(o =>
                    {
                        o.IncludeExceptionDetails = builder.Environment.IsDevelopment();
                    });
                ```

                ## Anti-patterns

                **Registering types both individually and via AddTypes:**

                ```csharp
                // BAD: Double registration can cause confusing errors
                builder.Services
                    .AddGraphQLServer()
                    .AddTypes()         // Auto-discovers UserType
                    .AddType<UserType>(); // Registered again — may conflict
                ```

                **Putting configuration after MapGraphQL:**

                ```csharp
                // BAD: MapGraphQL freezes the schema — later modifications are ignored
                var app = builder.Build();
                app.MapGraphQL();
                // Any further schema modifications after this point are too late
                ```

                **Not setting execution timeouts:**

                ```csharp
                // BAD: No timeout means a query can run indefinitely
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes();
                // Always set ExecutionTimeout for production
                ```

                ## Key Points

                - `AddGraphQLServer()` is the entry point for all Hot Chocolate configuration
                - Use `AddTypes()` to auto-discover types with source generation attributes
                - Configure pagination defaults with `SetPagingOptions`
                - Set `ExecutionTimeout` for production to prevent runaway queries
                - Use `RegisterDbContextFactory<T>()` for proper DbContext scoping
                - Apply environment-specific settings (disable introspection, depth limits) conditionally
                - Call `app.UseWebSockets()` before `app.MapGraphQL()` if using subscriptions

                ## Related Practices

                - [configuration-performance] — For performance tuning
                - [security-production-hardening] — For production security
                - [resolvers-di] — For service registration
                """
            });
    }
}
