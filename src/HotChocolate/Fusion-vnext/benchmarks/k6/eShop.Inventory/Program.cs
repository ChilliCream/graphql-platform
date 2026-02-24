using HotChocolate;

[assembly: Module("InventoryTypes")]

var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL("inventory-api", disableDefaultSecurity: true)
    .AddInventoryTypes();

var app = builder.Build();

app.MapGraphQL();

await app.RunWithGraphQLCommandsAsync(args);
