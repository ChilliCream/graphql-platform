using HotChocolate;

[assembly: Module("ReviewTypes")]

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer("reviews-api", disableDefaultSecurity: true)
    .AddReviewTypes();

var app = builder.Build();

app.MapGraphQL();

await app.RunWithGraphQLCommandsAsync(args);
