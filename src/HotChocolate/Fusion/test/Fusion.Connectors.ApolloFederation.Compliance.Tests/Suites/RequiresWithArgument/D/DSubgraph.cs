using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.RequiresWithArgument.D;

/// <summary>
/// Builds the <c>d</c> Apollo Federation subgraph for the
/// <c>requires-with-argument</c> audit suite. Extends <c>Post</c>
/// with <c>author</c> (via <c>@requires</c> with field arguments)
/// and <c>comments(limit)</c>.
/// </summary>
public static class DSubgraph
{
    public const string Name = "d";

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
            .AddType<CommentType>()
            .AddType<AuthorType>();

        var app = builder.Build();
        app.MapGraphQL();

        await app.StartAsync();

        return new SubgraphHost(Name, app);
    }
}
