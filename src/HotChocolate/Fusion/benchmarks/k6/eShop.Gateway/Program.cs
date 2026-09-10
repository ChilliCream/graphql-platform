using eShop.Gateway;

ThreadPool.SetMinThreads(512, 512);

var builder = WebApplication.CreateBuilder(args);

// Opt-in MemoryArena lifecycle tracing: enabled only when FUSION_ARENA_TRACE holds an output path.
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FUSION_ARENA_TRACE")))
{
    builder.Services.AddHostedService<MemoryArenaEventListener>();
}

builder.Services.AddHttpClient("Fusion")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        MaxConnectionsPerServer = 256,
        EnableMultipleHttp2Connections = true
    });

builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("gateway.far");

var app = builder.Build();

app.MapGraphQLHttp();

app.Run();
