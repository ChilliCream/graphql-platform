using HotChocolate;

[assembly: Module("AccountTypes")]

var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL("accounts-api")
    .AddAccountTypes();

var app = builder.Build();

app.MapGraphQLHttp();

await app.RunWithGraphQLCommandsAsync(args);
