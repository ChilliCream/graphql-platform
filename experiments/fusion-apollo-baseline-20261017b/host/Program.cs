using HotChocolate.Fusion.Configuration;
using eShop.Gateway.BenchmarkDiagnostics;

ThreadPool.SetMinThreads(256, 256);

var builder = WebApplication.CreateBuilder(args);

if (Environment.GetEnvironmentVariable("BENCH_DIAGNOSTIC_RUN") == "1")
{
    builder.Services.AddHostedService<FusionPoolDiagnostics>();
}

builder.Services
    .AddHeaderPropagation(
        options =>
        {
            options.Headers.Add("Authorization");
        });

var fusionHttpClient = builder.Services
    .AddHttpClient("fusion")
    // Drop the default LoggingHttpMessageHandler and LoggingScopeHttpMessageHandler. Even with
    // logging disabled they add two async state machines and a scope per subgraph call.
    .RemoveAllLoggers()
    .ConfigurePrimaryHttpMessageHandler(
        () => new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 256
        })
    .AddHeaderPropagation();

if (Environment.GetEnvironmentVariable("BENCH_CAPTURE_PATH") is { } capturePath)
{
    fusionHttpClient.AddHttpMessageHandler(() => new WireCaptureHandler(capturePath));
}

#if FUSION_16_7_OR_GREATER
builder
    .AddGraphQLGateway(disableDefaultSecurity: true)
    .ModifyRequestOptions(options =>
    {
        options.AllowOperationPlanRequests =
            Environment.GetEnvironmentVariable("BENCH_CAPTURE_PATH") is not null;
    })
    .AddFileSystemConfiguration("./gateway.far");
#else
builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far");
#endif

var app = builder.Build();

app.UseHeaderPropagation();
app.MapGraphQLHttp();

app.Run();
