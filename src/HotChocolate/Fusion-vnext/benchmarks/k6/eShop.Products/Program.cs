using HotChocolate;

[assembly: Module("ProductTypes")]

var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL("products-api")
    .AddProductTypes();

var app = builder.Build();

app.MapGraphQL();

await app.RunWithGraphQLCommandsAsync(args);
