using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Fusion.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Skimmed.Serialization;
using static HotChocolate.Fusion.Shared.DemoProjectSchemaExtensions;
using static HotChocolate.Language.Utf8GraphQLParser;
using static HotChocolate.Fusion.TestHelper;

namespace HotChocolate.Fusion;

// TODO: Test what happens if subgraph doesn't return data
// TODO: ResolveByKey tests
// TODO: Nested Object on sequential resolve

public class SubgraphErrorTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Resolve_Parallel_Entry_Resolver_Returns_Error_For_One_Service()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync("a", builder =>
        {
            builder
                .AddResolverMocking()
                .AddTestDirectives()
                .AddDocumentFromString(
                    """
                    type Query {
                      viewer: Viewer @error
                    }

                    type Viewer {
                      name: String
                    }
                    """);
        });

        var subgraphB = await TestSubgraph.CreateAsync("b", builder =>
        {
            builder
                .AddResolverMocking()
                .AddTestDirectives()
                .AddDocumentFromString(
                    """
                    type Query {
                      viewer: Viewer
                    }

                    type Viewer {
                      userId: ID
                    }
                    """);
        });

        using var subgraphs = new TestSubgraphCollection(output) { Subgraphs = [subgraphA, subgraphB], };

        // act
        var fusionGraph = await subgraphs.ComposeFusionGraphAsync();
        var executor = await subgraphs.GetExecutor(fusionGraph);

        var request = Parse(
            """
            query {
              viewer {
                userId
                name
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
        CollectErrorSnapshotData(snapshot, request, result);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Resolve_Parallel_Entry_Resolver_Returns_Error_For_Both_Services()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync("a", builder =>
        {
            builder
                .AddResolverMocking()
                .AddTestDirectives()
                .AddDocumentFromString(
                    """
                    type Query {
                      viewer: Viewer @error
                    }

                    type Viewer {
                      name: String
                    }
                    """);
        });

        var subgraphB = await TestSubgraph.CreateAsync("b", builder =>
        {
            builder
                .AddResolverMocking()
                .AddTestDirectives()
                .AddDocumentFromString(
                    """
                    type Query {
                      viewer: Viewer @error
                    }

                    type Viewer {
                      userId: ID
                    }
                    """);
        });

        using var subgraphs = new TestSubgraphCollection(output) { Subgraphs = [subgraphA, subgraphB], };

        // act
        var fusionGraph = await subgraphs.ComposeFusionGraphAsync();
        var executor = await subgraphs.GetExecutor(fusionGraph);

        var request = Parse(
            """
            query {
              viewer {
                userId
                name
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
        CollectErrorSnapshotData(snapshot, request, result);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Resolve_Parallel_Nested_Object_Field_Of_One_Service_Returns_Error()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync("a", builder =>
        {
            builder
                .AddResolverMocking()
                .AddTestDirectives()
                .AddDocumentFromString(
                    """
                    type Query {
                      viewer: Viewer
                    }

                    type Viewer {
                      name: String
                      obj: SomeObject @error
                    }

                    type SomeObject {
                      aField: String
                    }
                    """);
        });

        var subgraphB = await TestSubgraph.CreateAsync("b", builder =>
        {
            builder
                .AddResolverMocking()
                .AddTestDirectives()
                .AddDocumentFromString(
                    """
                    type Query {
                      viewer: Viewer
                    }

                    type Viewer {
                      userId: ID
                      obj: SomeObject
                    }

                    type SomeObject {
                      bField: String
                    }
                    """);
        });

        using var subgraphs = new TestSubgraphCollection(output) { Subgraphs = [subgraphA, subgraphB], };

        // act
        var fusionGraph = await subgraphs.ComposeFusionGraphAsync();
        var executor = await subgraphs.GetExecutor(fusionGraph);

        var request = Parse(
            """
            query {
              viewer {
                userId
                name
                obj {
                  aField
                  bField
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
        CollectErrorSnapshotData(snapshot, request, result);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Resolve_Parallel_Nested_Object_Field_Of_Both_Services_Returns_Error()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync("a", builder =>
        {
            builder
                .AddResolverMocking()
                .AddTestDirectives()
                .AddDocumentFromString(
                    """
                    type Query {
                      viewer: Viewer
                    }

                    type Viewer {
                      name: String
                      obj: SomeObject @error
                    }

                    type SomeObject {
                      aField: String
                    }
                    """);
        });

        var subgraphB = await TestSubgraph.CreateAsync("b", builder =>
        {
            builder
                .AddResolverMocking()
                .AddTestDirectives()
                .AddDocumentFromString(
                    """
                    type Query {
                      viewer: Viewer
                    }

                    type Viewer {
                      userId: ID
                      obj: SomeObject @error
                    }

                    type SomeObject {
                      bField: String
                    }
                    """);
        });

        using var subgraphs = new TestSubgraphCollection(output) { Subgraphs = [subgraphA, subgraphB], };

        // act
        var fusionGraph = await subgraphs.ComposeFusionGraphAsync();
        var executor = await subgraphs.GetExecutor(fusionGraph);

        var request = Parse(
            """
            query {
              viewer {
                userId
                name
                obj {
                  aField
                  bField
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
        CollectErrorSnapshotData(snapshot, request, result);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Resolve_Sequence_First_Service_Entry_Resolver_Error()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync("a", builder =>
        {
            builder
                .AddResolverMocking()
                .AddTestDirectives()
                .AddDocumentFromString(
                    """
                    type Query {
                      productById(id: ID!): Product @error
                    }

                    type Product {
                      id: ID!
                      name: String!
                      price: Float!
                    }
                    """);
        });

        var subgraphB = await TestSubgraph.CreateAsync("b", builder =>
        {
            builder
                .AddResolverMocking()
                .AddTestDirectives()
                .AddDocumentFromString(
                    """
                    type Query {
                      productById(id: ID!): Product
                    }

                    type Product {
                      id: ID!
                      score: Int!
                    }
                    """);
        });

        using var subgraphs = new TestSubgraphCollection(output) { Subgraphs = [subgraphA, subgraphB], };

        // act
        var fusionGraph = await subgraphs.ComposeFusionGraphAsync();
        var executor = await subgraphs.GetExecutor(fusionGraph);

        var request = Parse(
            """
            query {
              productById(id: "1") {
                id?
                name?
                price?
                score?
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
        CollectErrorSnapshotData(snapshot, request, result);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Resolve_Sequence_Both_Services_Entry_Resolver_Error()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync("a", builder =>
        {
            builder
                .AddResolverMocking()
                .AddTestDirectives()
                .AddDocumentFromString(
                    """
                    type Query {
                      productById(id: ID!): Product @error
                    }

                    type Product {
                      id: ID!
                      name: String!
                      price: Float!
                    }
                    """);
        });

        var subgraphB = await TestSubgraph.CreateAsync("b", builder =>
        {
            builder
                .AddResolverMocking()
                .AddTestDirectives()
                .AddDocumentFromString(
                    """
                    type Query {
                      productById(id: ID!): Product @error
                    }

                    type Product {
                      id: ID!
                      score: Int!
                    }
                    """);
        });

        using var subgraphs = new TestSubgraphCollection(output) { Subgraphs = [subgraphA, subgraphB], };

        // act
        var fusionGraph = await subgraphs.ComposeFusionGraphAsync();
        var executor = await subgraphs.GetExecutor(fusionGraph);

        var request = Parse(
            """
            query {
              productById(id: "1") {
                id?
                name?
                price?
                score?
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
        CollectErrorSnapshotData(snapshot, request, result);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Resolve_Sequence_Second_Service_Entry_Resolver_Error_Field_Nullable()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync("a", builder =>
        {
            builder
                .AddResolverMocking()
                .AddTestDirectives()
                .AddDocumentFromString(
                    """
                    type Query {
                      productById(id: ID!): Product
                    }

                    type Product {
                      id: ID!
                      name: String!
                      price: Float!
                    }
                    """);
        });

        var subgraphB = await TestSubgraph.CreateAsync("b", builder =>
        {
            builder
                .AddResolverMocking()
                .AddTestDirectives()
                .AddDocumentFromString(
                    """
                    type Query {
                      productById(id: ID!): Product @error
                    }

                    type Product {
                      id: ID!
                      score: Int!
                    }
                    """);
        });

        using var subgraphs = new TestSubgraphCollection(output) { Subgraphs = [subgraphA, subgraphB], };

        // act
        var fusionGraph = await subgraphs.ComposeFusionGraphAsync();
        var executor = await subgraphs.GetExecutor(fusionGraph);

        var request = Parse(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score?
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
        CollectErrorSnapshotData(snapshot, request, result);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Resolve_Sequence_Second_Service_Entry_Resolver_Error_Field_NonNull()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync("a", builder =>
        {
            builder
                .AddResolverMocking()
                .AddTestDirectives()
                .AddDocumentFromString(
                    """
                    type Query {
                      productById(id: ID!): Product
                    }

                    type Product {
                      id: ID!
                      name: String!
                      price: Float!
                    }
                    """);
        });

        var subgraphB = await TestSubgraph.CreateAsync("b", builder =>
        {
            builder
                .AddResolverMocking()
                .AddTestDirectives()
                .AddDocumentFromString(
                    """
                    type Query {
                      productById(id: ID!): Product @error
                    }

                    type Product {
                      id: ID!
                      score: Int!
                    }
                    """);
        });

        using var subgraphs = new TestSubgraphCollection(output) { Subgraphs = [subgraphA, subgraphB], };

        // act
        var fusionGraph = await subgraphs.ComposeFusionGraphAsync();
        var executor = await subgraphs.GetExecutor(fusionGraph);

        var request = Parse(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score!
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
        CollectErrorSnapshotData(snapshot, request, result);
        snapshot.MatchMarkdownSnapshot();
    }

    private readonly Func<ICompositionLog> _logFactory = () => new TestCompositionLog(output);

    [Fact]
    public async Task Subgraph_Error_Top_Level_Field()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync("a", builder =>
        {
            builder
                .AddResolverMocking()
                .AddTestDirectives()
                .AddDocumentFromString(
                    """
                    type Query {
                      field: String @error
                    }
                    """);
        });

        using var subgraphs = new TestSubgraphCollection(output) { Subgraphs = [subgraphA], };

        // act
        var fusionGraph = await subgraphs.ComposeFusionGraphAsync();
        var executor = await subgraphs.GetExecutor(fusionGraph);

        var request = Parse(
            """
            query {
              field
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
        CollectErrorSnapshotData(snapshot, request, result);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Subgraph_Error_For_Field_Resolved_In_Sequence()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph =
            await new FusionGraphComposer(logFactory: _logFactory)
                .ComposeAsync(
                    new[]
                    {
                        demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                        demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                    },
                    new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            {
              reviewById(id: "UmV2aWV3Cmkx") {
                body
                author {
                  username
                  errorField
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
        CollectErrorSnapshotData(snapshot, request, result);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Subgraph_Error_For_Field_Within_A_List()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph =
            await new FusionGraphComposer(logFactory: _logFactory)
                .ComposeAsync(
                    new[]
                    {
                        demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                        demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                    },
                    new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            {
              userById(id: "VXNlcgppMQ==") {
                account1: birthdate
                account2: birthdate
                username
                reviews {
                  body
                  errorField
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
        CollectErrorSnapshotData(snapshot, request, result);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Subgraph_Error_For_Field_Resolved_Via_Key_Batch()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph =
            await new FusionGraphComposer(logFactory: _logFactory)
                .ComposeAsync(
                    new[]
                    {
                        demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                        demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                    },
                    new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            {
              reviews {
                body
                author {
                  id
                  errorField
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
        CollectErrorSnapshotData(snapshot, request, result);
        snapshot.MatchMarkdownSnapshot();
    }
}
