using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.RequiresCircular.A;

/// <summary>
/// Builds subgraph <c>a</c> for the <c>requires-circular</c> suite.
/// Owns <c>Query.feed</c>, <c>Author</c> (all fields), and
/// <c>Post.byExpert</c> (<c>@requires(fields: "byNovice")</c>).
/// </summary>
public static class ASubgraph
{
    public const string Name = "a";

    public static async Task<SubgraphHost> BuildAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddRouting()
            .AddGraphQLServer()
            .AddApolloFederation()
            .AddQueryType<QueryType>()
            .AddType<PostType>()
            .AddType<AuthorType>();

        var app = builder.Build();
        app.MapGraphQL();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
