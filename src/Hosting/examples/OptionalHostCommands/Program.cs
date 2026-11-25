using HotChocolate.Types;
using Mocha;
using Mocha.Transport.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.AddGraphQL("catalog")
    .AddQueryType(t => t.Name("Query").Field("product").Resolve("Coffee"));
builder.AddGraphQL("reviews")
    .AddQueryType(t => t.Name("Query").Field("rating").Resolve(5));
builder.Services.AddMessageBus().AddInMemory(t => t.Name("orders"));

var app = builder.Build();
app.MapGraphQL("/catalog", "catalog");
app.MapGraphQL("/reviews", "reviews");

return await app.RunWithCommandsAsync(args);
