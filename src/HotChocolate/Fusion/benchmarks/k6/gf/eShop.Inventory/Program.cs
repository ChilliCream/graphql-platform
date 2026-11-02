using HotChocolate;

[assembly: Module("InventoryTypes")]

var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL("inventory-api", disableDefaultSecurity: true)
    .AddInventoryTypes()
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
