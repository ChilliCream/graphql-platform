var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSingleton<ProductRepository>();

builder.Services
    .AddGraphQLServer()
    .RemoveDirectiveType(typeof(HotChocolate.Types.DeferDirectiveType))
    .RemoveDirectiveType(typeof(HotChocolate.Types.StreamDirectiveType))
    .AddApolloFederation()
    .AddQueryType<Query>()
    .RegisterService<ProductRepository>();

var app = builder.Build();
app.MapGraphQL();
app.Run();
