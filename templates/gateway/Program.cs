var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHttpClient("fusion");

builder.Services
    .AddGraphQLGatewayServer()
    .AddFileSystemConfiguration("./gateway.graphql");

var app = builder.Build();

app.MapGraphQL();

app.Run();
