using HotChocolate.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("Fusion")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        MaxConnectionsPerServer = 256,
        EnableMultipleHttp2Connections = true
    });

builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far");

var app = builder.Build();

#if RELEASE
app.MapGraphQLHttp();
#else
app.MapGraphQL().WithOptions(new GraphQLServerOptions { Tool = { ServeMode = GraphQLToolServeMode.Insider } });
#endif

app.Run();
