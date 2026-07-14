var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHttpClient("fusion");

builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far");

var app = builder.Build();

app.MapGraphQL();

app.Run();
