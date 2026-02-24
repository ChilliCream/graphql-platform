ThreadPool.SetMinThreads(1024, 1024);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("Fusion")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        MaxConnectionsPerServer = 256,
        EnableMultipleHttp2Connections = true
    });

builder
    .AddGraphQLGateway()
    .ModifyPlannerOptions(o => o.EnableRequestGrouping = true)
    .AddFileSystemConfiguration("./gateway.far");

var app = builder.Build();

app.MapGraphQLHttp();

app.Run();
