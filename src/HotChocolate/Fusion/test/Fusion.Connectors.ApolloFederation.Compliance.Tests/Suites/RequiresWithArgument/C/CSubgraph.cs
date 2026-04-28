using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.RequiresWithArgument.C;

/// <summary>
/// Builds the <c>c</c> Apollo Federation subgraph for the
/// <c>requires-with-argument</c> audit suite. Owns <c>Post</c> and
/// <c>Comment</c> entities with a <c>feed</c> query field.
/// </summary>
public static class CSubgraph
{
    public const string Name = "c";

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
            .AddType<CommentType>();

        var app = builder.Build();
        app.MapGraphQL();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
