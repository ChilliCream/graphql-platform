using HotChocolate.Execution;
using HotChocolate.Fusion.Shared;
using Xunit.Abstractions;
using static HotChocolate.Fusion.TestHelper;

namespace HotChocolate.Fusion;

public class TransportErrorTests(ITestOutputHelper output)
{
    #region Resolve (node)
    [Fact]
    public async Task Resolve_Node_Service_Offline_EntryField_Nullable()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              node(id: ID!): Node
            }

            type Brand implements Node {
              id: ID!
              name: String
            }

            interface Node {
              id: ID!
            }
            """,
            isOffline: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        node(id: "QnJhbmQ6MQ==") {
                          id
                          ... on Brand {
                            name
                          }
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }
    #endregion

    #region Parallel, Shared Entry Field

    [Fact]
    public async Task Resolve_Parallel_One_Service_Offline_SubField_Nullable_SharedEntryField_Nullable()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String
            }
            """,
            isOffline: true);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              userId: ID
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        viewer {
                          userId
                          name
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Resolve_Parallel_One_Service_Offline_SubField_NonNull_SharedEntryField_Nullable()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String!
            }
            """,
            isOffline: true);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              userId: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        viewer {
                          userId
                          name
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Resolve_Parallel_One_Service_Offline_SubField_NonNull_SharedEntryField_NonNull()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer!
            }

            type Viewer {
              name: String!
            }
            """,
            isOffline: true);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer!
            }

            type Viewer {
              userId: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        viewer {
                          userId
                          name
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Resolve_Parallel_Both_Services_Offline_SharedEntryField_Nullable()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String
            }
            """,
            isOffline: true);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              userId: ID
            }
            """,
            isOffline: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        viewer {
                          userId
                          name
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Resolve_Parallel_Both_Services_Offline_SharedEntryField_NonNull()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer!
            }

            type Viewer {
              name: String
            }
            """,
            isOffline: true);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer!
            }

            type Viewer {
              userId: ID
            }
            """,
            isOffline: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        viewer {
                          userId
                          name
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    #endregion

    #region Parallel, No Shared Entry Field

    [Fact]
    public async Task Resolve_Parallel_Single_Service_Offline_EntryField_Nullable()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String!
            }
            """,
            isOffline: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        viewer {
                          name
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Resolve_Parallel_Single_Service_Offline_EntryField_NonNull()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer!
            }

            type Viewer {
              name: String!
            }
            """,
            isOffline: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        viewer {
                          name
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Resolve_Parallel_One_Service_Offline_EntryFields_Nullable()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String!
            }
            """,
            isOffline: true);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              other: Other
            }

            type Other {
              userId: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        viewer {
                          name
                        }
                        other {
                          userId
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Resolve_Parallel_One_Service_Offline_EntryFields_NonNull()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer!
            }

            type Viewer {
              name: String!
            }
            """,
            isOffline: true);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              other: Other!
            }

            type Other {
              userId: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        viewer {
                          name
                        }
                        other {
                          userId
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    #endregion

    #region Entity Resolver

    [Fact]
    public async Task Entity_Resolver_Single_Service_Offline_EntryField_Nullable()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product
            }

            type Product implements Node {
              id: ID!
              name: String
              price: Float
            }

            interface Node {
              id: ID!
            }
            """,
            isOffline: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        productById(id: "1") {
                          id
                          name
                          price
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Entity_Resolver_Single_Service_Offline_EntryField_NonNull()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product!
            }

            type Product implements Node {
              id: ID!
              name: String
              price: Float
            }

            interface Node {
              id: ID!
            }
            """,
            isOffline: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        productById(id: "1") {
                          id
                          name
                          price
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Entity_Resolver_First_Service_Offline_SubFields_Nullable_EntryField_Nullable()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product
            }

            type Product implements Node {
              id: ID!
              name: String
              price: Float
            }

            interface Node {
              id: ID!
            }
            """,
            isOffline: true);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product
            }

            type Product implements Node {
              id: ID!
              score: Int
            }

            interface Node {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        productById(id: "1") {
                          id
                          name
                          price
                          score
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Entity_Resolver_First_Service_Offline_SubFields_NonNull_EntryField_Nullable()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product
            }

            type Product implements Node {
              id: ID!
              name: String!
              price: Float!
            }

            interface Node {
              id: ID!
            }
            """,
            isOffline: true);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product
            }

            type Product implements Node {
              id: ID!
              score: Int!
            }

            interface Node {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        productById(id: "1") {
                          id
                          name
                          price
                          score
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Entity_Resolver_First_Service_Offline_SubFields_NonNull_EntryField_NonNull()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product!
            }

            type Product implements Node {
              id: ID!
              name: String!
              price: Float!
            }

            interface Node {
              id: ID!
            }
            """,
            isOffline: true);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product!
            }

            type Product implements Node {
              id: ID!
              score: Int!
            }

            interface Node {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        productById(id: "1") {
                          id
                          name
                          price
                          score
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Entity_Resolver_Second_Service_Offline_SubFields_Nullable_EntryField_Nullable()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product
            }

            type Product implements Node {
              id: ID!
              name: String
              price: Float
            }

            interface Node {
              id: ID!
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product
            }

            type Product implements Node {
              id: ID!
              score: Int
            }

            interface Node {
              id: ID!
            }
            """,
            isOffline: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        productById(id: "1") {
                          id
                          name
                          price
                          score
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Entity_Resolver_Second_Service_Offline_SubFields_NonNull_EntryField_Nullable()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product
            }

            type Product implements Node {
              id: ID!
              name: String!
              price: Float!
            }

            interface Node {
              id: ID!
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product
            }

            type Product implements Node {
              id: ID!
              score: Int!
            }

            interface Node {
              id: ID!
            }
            """,
            isOffline: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        productById(id: "1") {
                          id
                          name
                          price
                          score
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Entity_Resolver_Second_Service_Offline_SubFields_NonNull_EntryField_NonNull()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product!
            }

            type Product implements Node {
              id: ID!
              name: String!
              price: Float!
            }

            interface Node {
              id: ID!
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product!
            }

            type Product implements Node {
              id: ID!
              score: Int!
            }

            interface Node {
              id: ID!
            }
            """,
            isOffline: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        productById(id: "1") {
                          id
                          name
                          price
                          score
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Entity_Resolver_Both_Services_Offline_EntryField_Nullable()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product
            }

            type Product implements Node {
              id: ID!
              name: String!
              price: Float!
            }

            interface Node {
              id: ID!
            }
            """,
            isOffline: true);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product
            }

            type Product implements Node {
              id: ID!
              score: Int!
            }

            interface Node {
              id: ID!
            }
            """,
            isOffline: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        productById(id: "1") {
                          id
                          name
                          price
                          score
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Entity_Resolver_Both_Services_Offline_EntryField_NonNull()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product!
            }

            type Product implements Node {
              id: ID!
              name: String!
              price: Float!
            }

            interface Node {
              id: ID!
            }
            """,
            isOffline: true);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product!
            }

            type Product implements Node {
              id: ID!
              score: Int!
            }

            interface Node {
              id: ID!
            }
            """,
            isOffline: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        productById(id: "1") {
                          id
                          name
                          price
                          score
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    #endregion

    #region Resolve Sequence

    [Fact]
    public async Task Resolve_Sequence_First_Service_Offline_EntryField_Nullable()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              brand: Brand!
            }

            type Brand {
              id: ID!
            }
            """,
            isOffline: true);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              brandById(id: ID!): Brand
            }

            type Brand {
              id: ID!
              name: String!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        product {
                          id
                          brand {
                            id
                            name
                          }
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Resolve_Sequence_First_Service_Offline_EntryField_NonNull()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              product: Product!
            }

            type Product {
              id: ID!
              brand: Brand!
            }

            type Brand {
              id: ID!
            }
            """,
            isOffline: true);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              brandById(id: ID!): Brand
            }

            type Brand {
              id: ID!
              name: String!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        product {
                          id
                          brand {
                            id
                            name
                          }
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Resolve_Sequence_Second_Service_Offline_SubField_Nullable_Parent_Nullable()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              brand: Brand
            }

            type Brand {
              id: ID!
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              brandById(id: ID!): Brand
            }

            type Brand {
              id: ID!
              name: String
            }
            """,
            isOffline: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        product {
                          id
                          brand {
                            id
                            name
                          }
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Resolve_Sequence_Second_Service_Offline_SubField_NonNull_Parent_Nullable()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              brand: Brand
            }

            type Brand {
              id: ID!
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              brandById(id: ID!): Brand
            }

            type Brand {
              id: ID!
              name: String!
            }
            """,
            isOffline: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        product {
                          id
                          brand {
                            id
                            name
                          }
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Resolve_Sequence_Second_Service_Offline_SubField_NonNull_Parent_NonNull()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              brand: Brand!
            }

            type Brand {
              id: ID!
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              brandById(id: ID!): Brand
            }

            type Brand {
              id: ID!
              name: String!
            }
            """,
            isOffline: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        product {
                          id
                          brand {
                            id
                            name
                          }
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    #endregion

    #region Resolve Sequence (node)
    [Fact]
    public async Task Resolve_Sequence_Node_Second_Service_Offline_SubField_Nullable_Parent_Nullable()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              brand: Brand
            }

            type Brand {
              id: ID!
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              node(id: ID!): Node
            }

            type Brand implements Node {
              id: ID!
              name: String
            }

            interface Node {
              id: ID!
            }
            """,
            isOffline: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        product {
                          id
                          brand {
                            id
                            name
                          }
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }
    #endregion

    #region ResolveByKey

    [Fact]
    public async Task ResolveByKey_Second_Service_Offline_SubField_Nullable()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              products: [Product!]!
            }

            type Product {
              id: ID!
              name: String!
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              productsById(ids: [ID!]!): [Product]
            }

            type Product {
              id: ID!
              price: Int
            }
            """,
            isOffline: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        products {
                          id
                          name
                          price
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task ResolveByKey_Second_Service_Offline_SubField_NonNull_ListItem_NonNull()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              products: [Product!]!
            }

            type Product {
              id: ID!
              name: String!
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              productsById(ids: [ID!]!): [Product]
            }

            type Product {
              id: ID!
              price: Int!
            }
            """,
            isOffline: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        products {
                          id
                          name
                          price
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task ResolveByKey_Second_Service_Offline_SubField_NonNull_ListItem_Nullable()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              products: [Product]!
            }

            type Product {
              id: ID!
              name: String!
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              productsById(ids: [ID!]!): [Product]
            }

            type Product {
              id: ID!
              price: Int!
            }
            """,
            isOffline: true);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = """
                      query {
                        products {
                          id
                          name
                          price
                        }
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    #endregion
}
