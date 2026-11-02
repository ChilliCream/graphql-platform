using eShop.Gateway;
using eShop.Gateway.BenchmarkDiagnostics;

var builder = WebApplication.CreateBuilder(args);

if (Environment.GetEnvironmentVariable("BENCH_DIAGNOSTIC_RUN") == "1")
{
    builder.Services.AddHostedService<FusionPoolDiagnostics>();
}

// Opt-in MemoryArena lifecycle tracing: enabled only when FUSION_ARENA_TRACE holds an output path.
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FUSION_ARENA_TRACE")))
{
    builder.Services.AddHostedService<MemoryArenaEventListener>();
}

builder.Services
    .AddHeaderPropagation(options => options.Headers.Add("Authorization"));

builder.Services
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

builder
    .AddGraphQLGateway(disableDefaultSecurity: true)
    .AddFileSystemConfiguration("./gateway.far");

var app = builder.Build();

app.UseHeaderPropagation();
app.MapGraphQLHttp();

app.Run();
