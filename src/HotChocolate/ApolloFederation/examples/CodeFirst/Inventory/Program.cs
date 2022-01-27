var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddApolloFederation()
    .AddQueryType()
    .AddType<ProductType>();

var app = builder.Build();
app.MapGraphQL();
app.Run();
