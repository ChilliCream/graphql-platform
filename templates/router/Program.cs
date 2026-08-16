var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHttpClient("fusion");

builder
    .AddGraphQLRouter()
    .AddFileSystemConfiguration("./graph.far");

var app = builder.Build();

app.MapGraphQL();

app.Run();
