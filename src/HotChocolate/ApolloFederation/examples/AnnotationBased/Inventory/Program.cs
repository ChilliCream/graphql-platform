var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddApolloFederation()
    .AddQueryType()
    .AddType<Product>();

var app = builder.Build();
app.MapGraphQL();
app.Run();
