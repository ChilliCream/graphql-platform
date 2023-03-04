using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Schemas.Accounts;
using HotChocolate.Fusion.Schemas.Reviews;
using HotChocolate.Skimmed.Serialization;
using HotChocolate.Utilities.Introspection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Fusion;

public class DemoIntegrationTests
{
    private readonly TestServerFactory _testServerFactory = new();

    [Fact]
    public async Task Authors_And_Reviews_AutoCompose()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            });

        // assert
        SchemaFormatter
            .FormatAsString(fusionGraph)
            .MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Authors_And_Reviews_Query_GetUserReviews()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsString(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query GetUser {
                users {
                    name
                    reviews {
                        body
                        author {
                            name
                        }
                    }
                }
            }
            """);


        // act
        var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .Create());

        // assert
        var snapshot = new Snapshot();

        snapshot.Add(request, "User Request");

        if (result.ContextData is not null &&
            result.ContextData.TryGetValue("queryPlan", out var value) &&
            value is QueryPlan queryPlan)
        {
            snapshot.Add(queryPlan, "QueryPlan");
        }

        snapshot.Add(result, "Result");
        snapshot.Add(SchemaFormatter.FormatAsString(fusionGraph), "Fusion Graph");

        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Authors_And_Reviews_Query_ReviewsUser()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsString(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query GetUser {
                a: reviews {
                    body
                    author {
                        name
                    }
                }
                b: reviews {
                    body
                    author {
                        name
                    }
                }
                users {
                    name
                    reviews {
                        body
                        author {
                            name
                        }
                    }
                }
            }
            """);


        // act
        var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .Create());

        // assert
        var snapshot = new Snapshot();

        snapshot.Add(request, "User Request");

        if (result.ContextData is not null &&
            result.ContextData.TryGetValue("queryPlan", out var value) &&
            value is QueryPlan queryPlan)
        {
            snapshot.Add(queryPlan, "QueryPlan");
        }

        snapshot.Add(result, "Result");
        snapshot.Add(SchemaFormatter.FormatAsString(fusionGraph), "Fusion Graph");

        await snapshot.MatchAsync();
    }

    private const string AccountsExtensionSdl =
        """
        extend type Query {
          userById(id: Int! @is(field: "id")): User!
        }
        """;

    private const string ReviewsExtensionSdl =
        """
        extend type Query {
          authorById(id: Int! @is(field: "id")): Author
          productById(upc: Int! @is(field: "upc")): Product
        }

        schema
            @rename(coordinate: "Query.authorById", newName: "userById")
            @rename(coordinate: "Author", newName: "User") {
        }
        """;
}
