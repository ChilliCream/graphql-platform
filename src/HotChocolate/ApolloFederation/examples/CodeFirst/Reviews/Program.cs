var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ReviewRepository>();
builder.Services.AddSingleton<UserRepository>();

builder.Services
    .AddGraphQLServer()
    .AddApolloFederation()
    .AddQueryType()
    .AddType<Review>()
    .RegisterService<ReviewRepository>()
    .RegisterService<UserRepository>();

var app = builder.Build();

app.MapGraphQL();

app.Run();