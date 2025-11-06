using HotChocolate;
using HotChocolate.AspNetCore;

[assembly: Module("ReviewTypes")]

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer("reviews-api")
    .AddReviewTypes();

var app = builder.Build();

app.MapGraphQL();

await app.RunWithGraphQLCommandsAsync(args);
