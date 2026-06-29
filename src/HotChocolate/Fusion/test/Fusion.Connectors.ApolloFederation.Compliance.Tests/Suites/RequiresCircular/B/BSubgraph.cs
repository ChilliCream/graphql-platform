using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Suites.RequiresCircular.B;

/// <summary>
/// Builds subgraph <c>b</c> for the <c>requires-circular</c> suite.
/// Owns <c>Post.author</c> and <c>Post.byNovice</c>
/// (<c>@requires(fields: "author { yearsOfExperience }")</c>).
/// </summary>
public static class BSubgraph
{
    public const string Name = "b";

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
