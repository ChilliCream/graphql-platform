var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSingleton<UserRepository>();

builder.Services
    .AddGraphQLServer()
    .AddApolloFederation()
    .AddQueryType<QueryType>()
    .RegisterService<UserRepository>();

var app = builder.Build();
app.MapGraphQL();
app.Run();
