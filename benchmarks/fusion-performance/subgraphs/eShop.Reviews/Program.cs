[assembly: Module("ReviewTypes")]

ThreadPool.SetMinThreads(256, 256);

var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL("reviews-api", disableDefaultSecurity: true)
    .AddReviewTypes()
    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
    .AddHttpRequestInterceptor<BenchmarkHttpRequestInterceptor>();

var app = builder.Build();

if (string.Equals(Environment.GetEnvironmentVariable("BENCHMARK_SIMULATE_LATENCY"), "1"))
{
    app.Use(static next =>
        async context =>
        {
            await Task.Delay(4);
            await next(context);
        });
}

app.MapGraphQLHttp().WithOptions(o => o.MaxBatchSize = 25000);

await app.RunWithGraphQLCommandsAsync(args);
