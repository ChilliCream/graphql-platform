using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Fusion.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using static HotChocolate.Fusion.TestHelper;
using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Fusion;

// TODO: ResolveByKey tests
// TODO: Nested Object on sequential resolve

public class NullMergingTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Resolve_Parallel_Entry_Resolver_Returns_Null_For_One_Service()
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
                      viewer: Viewer @null
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
    public async Task Resolve_Parallel_Entry_Resolver_Returns_Null_For_Both_Services()
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
                      viewer: Viewer @null
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
                      viewer: Viewer @null
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
    public async Task Resolve_Parallel_Nested_Object_Field_Of_One_Service_Returns_Null()
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
                      obj: SomeObject @null
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
    public async Task Resolve_Parallel_Nested_Object_Field_Of_Both_Services_Returns_Null()
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
                      obj: SomeObject @null
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
                      obj: SomeObject @null
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
    public async Task Resolve_Sequence_First_Service_Entry_Resolver_Returns_Null()
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
                      productById(id: ID!): Product @null
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
    public async Task Resolve_Sequence_Both_Services_Entry_Resolver_Returns_Null()
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
                      productById(id: ID!): Product @null
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
                      productById(id: ID!): Product @null
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
    public async Task Resolve_Sequence_Second_Service_Entry_Resolver_Returns_Null_Field_Nullable()
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
                      productById(id: ID!): Product @null
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
    public async Task Resolve_Sequence_Second_Service_Entry_Resolver_Returns_Null_Field_NonNull()
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
                      productById(id: ID!): Product @null
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
}
