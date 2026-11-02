using HotChocolate.AspNetCore;
using HotChocolate.ApolloFederation;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddApolloFederation(FederationVersion.Federation27)
    // The cost analysis of this repository rates the gateway's entity requests above the default
    // limit, so it runs without enforcing the limit.
    .ModifyCostOptions(o => o.EnforceCostLimits = false)
    // Fusion sends transport-level request batches (a JSON array of operations) to
    // subgraphs; HotChocolate 16 rejects those with HC0009 unless batching is enabled.
    .ModifyServerOptions(o => o.Batching = AllowedBatching.All)
    .AddQueryType<eShop.Products.Query>()
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

app.MapGraphQL();
await app.RunAsync();
