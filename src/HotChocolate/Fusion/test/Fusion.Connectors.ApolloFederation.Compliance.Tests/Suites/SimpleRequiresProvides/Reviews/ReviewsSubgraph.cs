using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.SimpleRequiresProvides.Reviews;

/// <summary>
/// Builds the <c>reviews</c> Apollo Federation subgraph for the
/// <c>simple-requires-provides</c> audit suite. Owns the <c>Review</c>
/// entity and contributes <c>reviews</c> to both <c>User</c> and
/// <c>Product</c>.
/// </summary>
public static class ReviewsSubgraph
{
    /// <summary>
    /// The source-schema name by which the gateway addresses this subgraph.
    /// </summary>
    public const string Name = "reviews";

    /// <summary>
    /// Starts the subgraph's <see cref="TestServer"/> and returns a
    /// <see cref="SubgraphHost"/>.
    /// </summary>
    public static async Task<SubgraphHost> BuildAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddRouting()
            .AddGraphQLServer()
            .AddApolloFederation()
            .AddQueryType<QueryType>()
            .AddType<ReviewType>()
            .AddType<UserType>()
            .AddType<ProductType>();

        var app = builder.Build();
        app.MapGraphQL();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
