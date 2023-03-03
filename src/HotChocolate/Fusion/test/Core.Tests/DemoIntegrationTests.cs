using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Composition.Pipeline;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Schemas.Reviews;
using HotChocolate.Fusion.Schemas.Accounts;
using HotChocolate.Skimmed;
using HotChocolate.Skimmed.Serialization;
using HotChocolate.Utilities.Introspection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Fusion.Composition.WellKnownContextData;
using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Fusion;

public class DemoIntegrationTests
{
    private readonly TestServerFactory _testServerFactory = new();

    [Fact]
    public async Task Authors_And_Reviews_AutoCompose()
    {
        // arrange
        using var reviews = _testServerFactory.Create(
            s => s
                .AddRouting()
                .AddSingleton<ReviewRepository>()
                .AddGraphQLServer()
                .AddQueryType<ReviewQuery>(),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));

        using var accounts = _testServerFactory.Create(
            s => s
                .AddRouting()
                .AddSingleton<UserRepository>()
                .AddGraphQLServer()
                .AddQueryType<AccountQuery>(),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));


        var introspectionClient = new IntrospectionClient();

        var reviewsClient = reviews.CreateClient();
        reviewsClient.BaseAddress = new Uri("http://localhost:5000/graphql");
        var reviewsSchema = await introspectionClient.DownloadSchemaAsync(reviewsClient);

        var accountsClient = accounts.CreateClient();
        accountsClient.BaseAddress = new Uri("http://localhost:5000/graphql");
        var accountsSchema = await introspectionClient.DownloadSchemaAsync(accountsClient);

        var graphComposer = CreateComposer();
        var compositionContext = await graphComposer.ComposeAsync(
            new SubGraphConfiguration("Reviews", reviewsSchema.ToString(), ReviewsExtensionSdl),
            new SubGraphConfiguration("Accounts", accountsSchema.ToString(), AccountsExtensionSdl));
        var fusionGraph = compositionContext.FusionGraph;
        var httpClientDirectiveType = new DirectiveType("httpClient");
        fusionGraph.Directives.Add(
            new Directive(
                httpClientDirectiveType,
                new Argument("subGraph", "Reviews"),
                new Argument("baseAddress", "https://b/graphql")));
        fusionGraph.Directives.Add(
            new Directive(
                httpClientDirectiveType,
                new Argument("subGraph", "Accounts"),
                new Argument("baseAddress", "https://b/graphql")));

        var fusionTypes = fusionGraph.Types
            .Where(t => t.ContextData.ContainsKey(IsFusionType))
            .ToArray();

        foreach (var type in fusionTypes)
        {
            fusionGraph.Types.Remove(type);
        }

        var fusionDirectiveTypes = fusionGraph.DirectiveTypes
            .Where(t => t.ContextData.ContainsKey(IsFusionType))
            .ToArray();

        foreach (var type in fusionDirectiveTypes)
        {
            fusionGraph.DirectiveTypes.Remove(type);
        }

        var serviceConfig = SchemaFormatter.FormatAsString(fusionGraph);
        serviceConfig.MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Authors_And_Reviews_Query_GetUserReviews()
    {
        // arrange
        using var reviews = _testServerFactory.Create(
            s => s
                .AddRouting()
                .AddSingleton<ReviewRepository>()
                .AddGraphQLServer()
                .AddQueryType<ReviewQuery>(),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));

        using var accounts = _testServerFactory.Create(
            s => s
                .AddRouting()
                .AddSingleton<UserRepository>()
                .AddGraphQLServer()
                .AddQueryType<AccountQuery>(),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));


        var introspectionClient = new IntrospectionClient();

        var reviewsClient = reviews.CreateClient();
        reviewsClient.BaseAddress = new Uri("http://localhost:5000/graphql");
        var reviewsSchema = await introspectionClient.DownloadSchemaAsync(reviewsClient);

        var accountsClient = accounts.CreateClient();
        accountsClient.BaseAddress = new Uri("http://localhost:5000/graphql");
        var accountsSchema = await introspectionClient.DownloadSchemaAsync(accountsClient);

        var clients = new Dictionary<string, Func<HttpClient>>
        {
            {
                "Reviews", () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = reviews.CreateClient();
                    httpClient.BaseAddress = new Uri("http://localhost:5000/graphql");
                    return httpClient;
                }
            },
            {
                "Accounts", () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = accounts.CreateClient();
                    httpClient.BaseAddress = new Uri("http://localhost:5000/graphql");
                    return httpClient;
                }
            },
        };

        var graphComposer = CreateComposer();
        var compositionContext = await graphComposer.ComposeAsync(
            new SubGraphConfiguration("Reviews", reviewsSchema.ToString(), ReviewsExtensionSdl),
            new SubGraphConfiguration("Accounts", accountsSchema.ToString(), AccountsExtensionSdl));
        var fusionGraph = compositionContext.FusionGraph;
        var httpClientDirectiveType = new DirectiveType("httpClient");
        fusionGraph.Directives.Add(
            new Directive(
                httpClientDirectiveType,
                new Argument("subGraph", "Reviews"),
                new Argument("baseAddress", "https://b/graphql")));
        fusionGraph.Directives.Add(
            new Directive(
                httpClientDirectiveType,
                new Argument("subGraph", "Accounts"),
                new Argument("baseAddress", "https://b/graphql")));

        var fusionTypes = fusionGraph.Types
            .Where(t => t.ContextData.ContainsKey(IsFusionType))
            .ToArray();

        foreach (var type in fusionTypes)
        {
            fusionGraph.Types.Remove(type);
        }

        var fusionDirectiveTypes = fusionGraph.DirectiveTypes
            .Where(t => t.ContextData.ContainsKey(IsFusionType))
            .ToArray();

        foreach (var type in fusionDirectiveTypes)
        {
            fusionGraph.DirectiveTypes.Remove(type);
        }

        var serviceConfig = SchemaFormatter.FormatAsString(fusionGraph);

        var clientFactory = new RemoteQueryExecutorTests.MockHttpClientFactory(clients);

        var executor = await new ServiceCollection()
            .AddSingleton<IHttpClientFactory>(clientFactory)
            .AddFusionGatewayServer(serviceConfig)
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
        snapshot.Add(serviceConfig, "Service Configuration");

        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Authors_And_Reviews_Query_ReviewsUser()
    {
        // arrange
        using var reviews = _testServerFactory.Create(
            s => s
                .AddRouting()
                .AddSingleton<ReviewRepository>()
                .AddGraphQLServer()
                .AddQueryType<ReviewQuery>(),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));

        using var accounts = _testServerFactory.Create(
            s => s
                .AddRouting()
                .AddSingleton<UserRepository>()
                .AddGraphQLServer()
                .AddQueryType<AccountQuery>(),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));


        var introspectionClient = new IntrospectionClient();

        var reviewsClient = reviews.CreateClient();
        reviewsClient.BaseAddress = new Uri("http://localhost:5000/graphql");
        var reviewsSchema = await introspectionClient.DownloadSchemaAsync(reviewsClient);

        var accountsClient = accounts.CreateClient();
        accountsClient.BaseAddress = new Uri("http://localhost:5000/graphql");
        var accountsSchema = await introspectionClient.DownloadSchemaAsync(accountsClient);

        var clients = new Dictionary<string, Func<HttpClient>>
        {
            {
                "Reviews", () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = reviews.CreateClient();
                    httpClient.BaseAddress = new Uri("http://localhost:5000/graphql");
                    return httpClient;
                }
            },
            {
                "Accounts", () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = accounts.CreateClient();
                    httpClient.BaseAddress = new Uri("http://localhost:5000/graphql");
                    return httpClient;
                }
            },
        };

        var graphComposer = CreateComposer();
        var compositionContext = await graphComposer.ComposeAsync(
            new SubGraphConfiguration("Reviews", reviewsSchema.ToString(), ReviewsExtensionSdl),
            new SubGraphConfiguration("Accounts", accountsSchema.ToString(), AccountsExtensionSdl));
        var fusionGraph = compositionContext.FusionGraph;
        var httpClientDirectiveType = new DirectiveType("httpClient");
        fusionGraph.Directives.Add(
            new Directive(
                httpClientDirectiveType,
                new Argument("subGraph", "Reviews"),
                new Argument("baseAddress", "https://b/graphql")));
        fusionGraph.Directives.Add(
            new Directive(
                httpClientDirectiveType,
                new Argument("subGraph", "Accounts"),
                new Argument("baseAddress", "https://b/graphql")));

        var fusionTypes = fusionGraph.Types
            .Where(t => t.ContextData.ContainsKey(IsFusionType))
            .ToArray();

        foreach (var type in fusionTypes)
        {
            fusionGraph.Types.Remove(type);
        }

        var fusionDirectiveTypes = fusionGraph.DirectiveTypes
            .Where(t => t.ContextData.ContainsKey(IsFusionType))
            .ToArray();

        foreach (var type in fusionDirectiveTypes)
        {
            fusionGraph.DirectiveTypes.Remove(type);
        }

        var serviceConfig = SchemaFormatter.FormatAsString(fusionGraph);

        var clientFactory = new RemoteQueryExecutorTests.MockHttpClientFactory(clients);

        var executor = await new ServiceCollection()
            .AddSingleton<IHttpClientFactory>(clientFactory)
            .AddFusionGatewayServer(serviceConfig)
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
        snapshot.Add(serviceConfig, "Service Configuration");

        await snapshot.MatchAsync();
    }

    private const string AccountsExtensionSdl =
        """
        extend type Query {
          userById(id: Int! @ref(field: "id")): User!
        }
        """;

    private const string ReviewsExtensionSdl =
        """
        extend type Query {
          authorById(id: Int! @ref(field: "id")): Author
          productById(upc: Int! @ref(field: "upc")): Product
        }

        schema
            @rename(coordinate: "Query.authorById", newName: "userById")
            @rename(coordinate: "Author", newName: "User") {
        }
        """;

    private static FusionGraphComposer CreateComposer()
        => new FusionGraphComposer(
            new IEntityEnricher[]
            {
                new RefResolverEntityEnricher()
            },
            new ITypeMergeHandler[]
            {
                new InterfaceTypeMergeHandler(),
                new UnionTypeMergeHandler(),
                new InputObjectTypeMergeHandler(),
                new EnumTypeMergeHandler(),
                new ScalarTypeMergeHandler()
            });
}
