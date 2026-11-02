using HotChocolate;

[assembly: Module("AccountTypes")]

var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL("accounts-api", disableDefaultSecurity: true)
    .AddAccountTypes()
    .AddHttpRequestInterceptor<BenchmarkHttpRequestInterceptor>();

var app = builder.Build();

if (int.TryParse(Environment.GetEnvironmentVariable("BENCHMARK_SUBGRAPH_DELAY_MS"), out var subgraphDelayMs)
    && subgraphDelayMs > 0)
{
    // this will simulate IO latency, it will introduce jitter but this also makes it more realistic
    app.Use(next =>
        async context =>
        {
            await Task.Delay(subgraphDelayMs);
            await next(context);
        });
}

app.MapGraphQLHttp();

await app.RunWithGraphQLCommandsAsync(args);
