var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSingleton<ProductRepository>();

builder.Services
    .AddGraphQLServer()
    .AddApolloFederation()
    .AddQueryType<QueryType>()
    .RegisterService<ProductRepository>();

var app = builder.Build();
app.MapGraphQL();
app.Run();
