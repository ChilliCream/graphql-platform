using HotChocolate;
using eShop.Reviews;

[assembly: Module("ReviewTypes")]

var builder = WebApplication.CreateBuilder(args);

// Opt-in buffer-pool tracing: enabled only when FUSION_ARENA_TRACE holds an output path.
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FUSION_ARENA_TRACE")))
{
    builder.Services.AddHostedService<MemoryArenaEventListener>();
}

builder
    .AddGraphQL("reviews-api", disableDefaultSecurity: true)
    .AddReviewTypes()
    .AddHttpRequestInterceptor<BenchmarkHttpRequestInterceptor>();

var app = builder.Build();

if (int.TryParse(Environment.GetEnvironmentVariable("BENCHMARK_SUBGRAPH_DELAY_MS"), out var subgraphDelayMs)
    && subgraphDelayMs > 0)
{
    app.Use(next =>
        async context =>
        {
            await Task.Delay(subgraphDelayMs);
            await next(context);
        });
}

app.MapGraphQLHttp();

await app.RunWithGraphQLCommandsAsync(args);
