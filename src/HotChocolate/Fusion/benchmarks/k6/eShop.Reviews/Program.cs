using HotChocolate;
using eShop.Reviews;

[assembly: Module("ReviewTypes")]

var builder = WebApplication.CreateBuilder(args);

// Opt-in buffer-pool tracing: enabled only when FUSION_ARENA_TRACE holds an output path.
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FUSION_ARENA_TRACE")))
{
    builder.Services.AddHostedService<MemoryArenaEventListener>();
}

builder.Services
    .AddGraphQLServer("reviews-api", disableDefaultSecurity: true)
    .AddReviewTypes();

var app = builder.Build();

app.MapGraphQL();

await app.RunWithGraphQLCommandsAsync(args);
