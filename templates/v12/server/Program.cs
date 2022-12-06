var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddServerTypes();

var app = builder.Build();

app.MapGraphQL();

app.Run();
