var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

app.Run();
