ThreadPool.SetMinThreads(512, 512);

var builder = WebApplication.CreateBuilder(args);

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
