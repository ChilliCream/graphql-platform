using HotChocolate.Fusion.Shared;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using static HotChocolate.Fusion.TestHelper;

namespace HotChocolate.Fusion;

public class SubgraphErrorTests(ITestOutputHelper output)
{
    #region Parallel, Shared Entry Field

    [Fact]
    public async Task Resolve_Parallel_SharedEntryField_Nullable_Both_Services_Error_SharedEntryField()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer @error
            }

            type Viewer {
              name: String
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer @error
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
    public async Task Resolve_Parallel_SharedEntryField_NonNull_Both_Services_Error_SharedEntryField()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer! @error
            }

            type Viewer {
              name: String
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer! @error
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
    public async Task Resolve_Parallel_SubField_Nullable_SharedEntryField_Nullable_One_Service_Errors_SharedEntryField()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer @error
            }

            type Viewer {
              name: String
            }
            """);

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
    public async Task Resolve_Parallel_SubField_NonNull_SharedEntryField_Nullable_One_Service_Errors_SharedEntryField()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer @error
            }

            type Viewer {
              name: String!
            }
            """);

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
    public async Task Resolve_Parallel_SubField_NonNull_SharedEntryField_NonNull_One_Service_Errors_SharedEntryField()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer! @error
            }

            type Viewer {
              name: String!
            }
            """);

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
    public async Task Resolve_Parallel_SubField_Nullable_SharedEntryField_Nullable_One_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String @error
            }
            """);

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
    public async Task Resolve_Parallel_SubField_NonNull_SharedEntryField_Nullable_One_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String! @error
            }
            """);

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
    public async Task Resolve_Parallel_SubField_NonNull_SharedEntryField_NonNull_One_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              viewer: Viewer!
            }

            type Viewer {
              name: String! @error
            }
            """);

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
    public async Task
        Resolve_Parallel_SubField_Nullable_SharedEntryField_Nullable_One_Service_Returns_TopLevel_Error_Without_Data()
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
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            builder => builder
                .AddDocumentFromString(
                    """
                    type Query {
                      viewer: Viewer
                    }

                    type Viewer {
                      userId: ID
                    }
                    """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result =
                        OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
                    return default;
                })
        );

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
    public async Task
        Resolve_Parallel_SubField_NonNull_SharedEntryField_Nullable_One_Service_Returns_TopLevel_Error_Without_Data()
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
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            builder => builder
                .AddDocumentFromString(
                    """
                    type Query {
                      viewer: Viewer
                    }

                    type Viewer {
                      userId: ID!
                    }
                    """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result =
                        OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
                    return default;
                })
        );

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
    public async Task
        Resolve_Parallel_SubField_NonNull_SharedEntryField_NonNull_One_Service_Returns_TopLevel_Error_Without_Data()
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
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            builder => builder
                .AddDocumentFromString(
                    """
                    type Query {
                      viewer: Viewer!
                    }

                    type Viewer {
                      userId: ID!
                    }
                    """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result =
                        OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
                    return default;
                })
        );

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
    public async Task Resolve_Parallel_SubField_Nullable_EntryField_Nullable_One_Service_Errors_SubField()
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
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              other: Other
            }

            type Other {
              userId: ID @error
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
    public async Task Resolve_Parallel_SubField_NonNull_EntryField_Nullable_One_Service_Errors_SubField()
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
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              other: Other
            }

            type Other {
              userId: ID! @error
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
    public async Task Resolve_Parallel_SubField_NonNull_EntryField_NonNull_One_Service_Errors_SubField()
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
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              other: Other!
            }

            type Other {
              userId: ID! @error
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
    public async Task Resolve_Parallel_EntryField_Nullable_One_Service_Errors_EntryField()
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
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              other: Other @error
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
    public async Task Resolve_Parallel_EntryField_NonNull_One_Service_Errors_EntryField()
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
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              other: Other! @error
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
    public async Task Resolve_Parallel_EntryField_Nullable_One_Service_Returns_TopLevel_Error_Without_Data()
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
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            builder => builder
                .AddDocumentFromString(
                    """
                    type Query {
                      other: Other
                    }

                    type Other {
                      userId: ID!
                    }
                    """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result =
                        OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
                    return default;
                })
        );

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
    public async Task Resolve_Parallel_EntryField_NonNull_One_Service_Returns_TopLevel_Error_Without_Data()
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
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            builder => builder
                .AddDocumentFromString(
                    """
                    type Query {
                      other: Other!
                    }

                    type Other {
                      userId: ID!
                    }
                    """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result =
                        OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
                    return default;
                })
        );

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
    public async Task Entity_Resolver_SubField_Nullable_EntryField_Nullable_First_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product
            }

            type Product implements Node {
              id: ID!
              name: String @error
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
    public async Task Entity_Resolver_SubField_NonNull_EntryField_Nullable_First_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product
            }

            type Product implements Node {
              id: ID!
              name: String! @error
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
    public async Task Entity_Resolver_SubField_NonNull_EntryField_NonNull_First_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product!
            }

            type Product implements Node {
              id: ID!
              name: String! @error
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
    public async Task Entity_Resolver_SubField_Nullable_EntryField_Nullable_Second_Service_Errors_SubField()
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
              score: Int @error
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
    public async Task Entity_Resolver_SubField_NonNull_EntryField_Nullable_Second_Service_Errors_SubField()
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
              score: Int! @error
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
    public async Task Entity_Resolver_SubField_NonNull_EntryField_NonNull_Second_Service_Errors_SubField()
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
              score: Int! @error
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
    public async Task Entity_Resolver_SubField_Nullable_EntryField_Nullable_First_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product @error
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
    public async Task Entity_Resolver_SubField_NonNull_EntryField_Nullable_First_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product @error
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
    public async Task Entity_Resolver_SubField_NonNull_EntryField_NonNull_First_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product! @error
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
    public async Task Entity_Resolver_SubField_Nullable_EntryField_Nullable_Second_Service_Errors_EntryField()
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
              productById(id: ID!): Product @error
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
    public async Task Entity_Resolver_SubField_NonNull_EntryField_Nullable_Second_Service_Errors_EntryField()
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
              productById(id: ID!): Product @error
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
    public async Task Entity_Resolver_SubField_NonNull_EntryField_NonNull_Second_Service_Errors_EntryField()
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
              productById(id: ID!): Product! @error
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
    public async Task Entity_Resolver_EntryField_Nullable_Both_Services_Error_EntryField()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product @error
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
              productById(id: ID!): Product @error
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
    public async Task Entity_Resolver_EntryField_NonNull_Both_Services_Error_EntryField()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product! @error
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
              productById(id: ID!): Product! @error
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
    public async Task
        Entity_Resolver_SubField_Nullable_EntryField_Nullable_Second_Service_Returns_TopLevel_Error_Without_Data()
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
            builder => builder
                .AddDocumentFromString(
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
                    """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result =
                        OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
                    return default;
                })
        );

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
    public async Task
        Entity_Resolver_SubField_NonNull_EntryField_Nullable_Second_Service_Returns_TopLevel_Error_Without_Data()
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
            builder => builder
                .AddDocumentFromString(
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
                    """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result =
                        OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
                    return default;
                })
        );

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
    public async Task
        Entity_Resolver_SubField_NonNull_EntryField_NonNull_Second_Service_Returns_TopLevel_Error_Without_Data()
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
            builder => builder
                .AddDocumentFromString(
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
                    """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result =
                        OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
                    return default;
                })
        );

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
    public async Task
        Entity_Resolver_SubField_Nullable_EntryField_Nullable_First_Service_Returns_TopLevel_Error_Without_Data()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            builder => builder
                .AddDocumentFromString(
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
                    """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result =
                        OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
                    return default;
                })
        );

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
            """
        );

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
    public async Task
        Entity_Resolver_SubField_NonNull_EntryField_Nullable_First_Service_Returns_TopLevel_Error_Without_Data()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            builder => builder
                .AddDocumentFromString(
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
                    """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result =
                        OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
                    return default;
                })
        );

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
            """
        );

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
    public async Task
        Entity_Resolver_SubField_NonNull_EntryField_NonNull_First_Service_Returns_TopLevel_Error_Without_Data()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            builder => builder
                .AddDocumentFromString(
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
                    """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result =
                        OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
                    return default;
                })
        );

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
            """
        );

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
    public async Task Resolve_Sequence_SubField_Nullable_Parent_Nullable_One_Service_Errors_SubField()
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
              name: String @error
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
    public async Task Resolve_Sequence_SubField_NonNull_Parent_Nullable_One_Service_Errors_SubField()
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
              name: String! @error
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
    public async Task Resolve_Sequence_SubField_NonNull_Parent_NonNull_One_Service_Errors_SubField()
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
              name: String! @error
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
    public async Task Resolve_Sequence_SubField_Nullable_Parent_Nullable_One_Service_Errors_EntryField()
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
              brandById(id: ID!): Brand @error
            }

            type Brand {
              id: ID!
              name: String
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
    public async Task Resolve_Sequence_SubField_NonNull_Parent_Nullable_One_Service_Errors_EntryField()
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
              brandById(id: ID!): Brand @error
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
    public async Task Resolve_Sequence_SubField_NonNull_Parent_NonNull_One_Service_Errors_EntryField()
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
              brandById(id: ID!): Brand @error
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
    public async Task
        Resolve_Sequence_SubField_Nullable_Parent_Nullable_Second_Service_Returns_TopLevel_Error_Without_Data()
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
            builder => builder
                .AddDocumentFromString(
                    """
                    type Query {
                      brandById(id: ID!): Brand
                    }

                    type Brand {
                      id: ID!
                      name: String
                    }
                    """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result =
                        OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
                    return default;
                })
        );

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
    public async Task
        Resolve_Sequence_SubField_NonNull_Parent_Nullable_Second_Service_Returns_TopLevel_Error_Without_Data()
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
            builder => builder
                .AddDocumentFromString(
                    """
                    type Query {
                      brandById(id: ID!): Brand
                    }

                    type Brand {
                      id: ID!
                      name: String!
                    }
                    """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result =
                        OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
                    return default;
                })
        );

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
    public async Task
        Resolve_Sequence_SubField_NonNull_Parent_NonNull_Second_Service_Returns_TopLevel_Error_Without_Data()
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
            builder => builder
                .AddDocumentFromString(
                    """
                    type Query {
                      brandById(id: ID!): Brand
                    }

                    type Brand {
                      id: ID!
                      name: String!
                    }
                    """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result =
                        OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
                    return default;
                })
        );

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
    public async Task ResolveByKey_SubField_Nullable_ListItem_Nullable_Second_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              products: [Product]
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
              price: Int @error(atIndex: 1)
            }
            """);

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
    public async Task ResolveByKey_SubField_NonNull_ListItem_Nullable_Second_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              products: [Product]
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
              price: Int! @error(atIndex: 1)
            }
            """);

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
    public async Task ResolveByKey_SubField_NonNull_ListItem_NonNull_Second_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              products: [Product!]
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
              price: Int! @error(atIndex: 1)
            }
            """);

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
    public async Task ResolveByKey_SubField_Nullable_ListItem_Nullable_Second_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              products: [Product]
            }

            type Product {
              id: ID!
              name: String!
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              productsById(ids: [ID!]!): [Product] @error
            }

            type Product {
              id: ID!
              price: Int
            }
            """);

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
    public async Task ResolveByKey_SubField_NonNull_ListItem_Nullable_Second_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              products: [Product]
            }

            type Product {
              id: ID!
              name: String!
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              productsById(ids: [ID!]!): [Product] @error
            }

            type Product {
              id: ID!
              price: Int!
            }
            """);

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
    public async Task ResolveByKey_SubField_NonNull_ListItem_NonNull_Second_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              products: [Product!]
            }

            type Product {
              id: ID!
              name: String!
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              productsById(ids: [ID!]!): [Product] @error
            }

            type Product {
              id: ID!
              price: Int!
            }
            """);

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
    public async Task
        ResolveByKey_SubField_Nullable_ListItem_Nullable_Second_Service_Returns_TopLevel_Error_Without_Data()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              products: [Product]
            }

            type Product {
              id: ID!
              name: String!
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            builder => builder
                .AddDocumentFromString(
                    """
                    type Query {
                      productsById(ids: [ID!]!): [Product]
                    }

                    type Product {
                      id: ID!
                      price: Int
                    }
                    """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result =
                        OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
                    return default;
                })
        );

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
    public async Task
        ResolveByKey_SubField_NonNull_ListItem_Nullable_Second_Service_Returns_TopLevel_Error_Without_Data()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              products: [Product]
            }

            type Product {
              id: ID!
              name: String!
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            builder => builder
                .AddDocumentFromString(
                    """
                    type Query {
                      productsById(ids: [ID!]!): [Product]
                    }

                    type Product {
                      id: ID!
                      price: Int!
                    }
                    """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result =
                        OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
                    return default;
                })
        );

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
    public async Task
        ResolveByKey_SubField_NonNull_ListItem_NonNull_Second_Service_Returns_TopLevel_Error_Without_Data()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              products: [Product!]
            }

            type Product {
              id: ID!
              name: String!
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            builder => builder
                .AddDocumentFromString(
                    """
                    type Query {
                      productsById(ids: [ID!]!): [Product]
                    }

                    type Product {
                      id: ID!
                      price: Int!
                    }
                    """)
                .AddResolverMocking()
                .UseDefaultPipeline()
                .UseRequest(_ => context =>
                {
                    context.Result =
                        OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
                    return default;
                })
        );

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

    [Fact]
    public async Task ErrorFilter_Is_Applied()
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: String @error
            }
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync(
            configure: builder =>
                builder.AddErrorFilter(error => error.WithMessage("REPLACED MESSAGE").WithCode("CUSTOM_CODE")));
        var request = """
                      query {
                        field
                      }
                      """;

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchInlineSnapshot("""
                                   {
                                     "errors": [
                                       {
                                         "message": "REPLACED MESSAGE",
                                         "locations": [
                                           {
                                             "line": 2,
                                             "column": 3
                                           }
                                         ],
                                         "path": [
                                           "field"
                                         ],
                                         "extensions": {
                                           "code": "CUSTOM_CODE"
                                         }
                                       }
                                     ],
                                     "data": {
                                       "field": null
                                     }
                                   }
                                   """);
    }
}
