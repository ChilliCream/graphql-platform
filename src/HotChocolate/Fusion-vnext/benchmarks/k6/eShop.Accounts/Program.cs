using HotChocolate;

[assembly: Module("AccountTypes")]

var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL("accounts-api")
    .AddAccountTypes();

var app = builder.Build();

app.MapGraphQL();

await app.RunWithGraphQLCommandsAsync(args);
