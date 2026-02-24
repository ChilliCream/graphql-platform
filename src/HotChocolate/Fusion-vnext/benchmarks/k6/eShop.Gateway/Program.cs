ThreadPool.SetMinThreads(1024, 1024);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("Fusion")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        MaxConnectionsPerServer = 256,
        EnableMultipleHttp2Connections = true,
        InitialHttp2StreamWindowSize = 768 * 1024,
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(10)
    })
    .ConfigureHttpClient(c =>
    {
        c.DefaultRequestVersion = System.Net.HttpVersion.Version20;
        c.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
    });

builder
    .AddGraphQLGateway()
    .ModifyPlannerOptions(o => o.EnableRequestGrouping = true)
    .AddFileSystemConfiguration("./gateway.far");

var app = builder.Build();

app.MapGraphQLHttp();

app.Run();
