using HotChocolate;
using Microsoft.AspNetCore.Server.Kestrel.Core;

[assembly: Module("InventoryTypes")]

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(o =>
{
    o.ConfigureEndpointDefaults(lo => lo.Protocols = HttpProtocols.Http2);
    o.Limits.Http2.MaxStreamsPerConnection = 1000;
    o.Limits.Http2.InitialConnectionWindowSize = 1024 * 1024;
    o.Limits.Http2.InitialStreamWindowSize = 768 * 1024;
});

builder
    .AddGraphQL("inventory-api")
    .AddInventoryTypes();

var app = builder.Build();

app.MapGraphQL();

await app.RunWithGraphQLCommandsAsync(args);
