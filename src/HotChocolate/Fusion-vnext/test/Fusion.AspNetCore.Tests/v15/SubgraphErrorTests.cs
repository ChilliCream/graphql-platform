using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

public class SubgraphErrorTests : FusionTestBase
{
    #region Parallel, Shared Entry Field

    [Fact(Skip = "There should only ever be one error associated with a field")]
    public async Task Resolve_Parallel_SharedEntryField_Nullable_Both_Services_Error_SharedEntryField()
    {
        // arrange
        var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer @error
            }

            type Viewer {
              name: String
            }
            """);

        var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer @error
            }

            type Viewer {
              userId: ID
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                userId
                name
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Flaky in snapshot")]
    public async Task Resolve_Parallel_SharedEntryField_NonNull_Both_Services_Error_SharedEntryField()
    {
        // arrange
        var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer! @error
            }

            type Viewer {
              name: String
            }
            """);

        var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer! @error
            }

            type Viewer {
              userId: ID
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                userId
                name
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Error is incorrectly placed")]
    public async Task Resolve_Parallel_SubField_Nullable_SharedEntryField_Nullable_One_Service_Errors_SharedEntryField()
    {
        // arrange
        var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer @error
            }

            type Viewer {
              name: String
            }
            """);

        var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              userId: ID
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                userId
                name
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Error is not correctly shown")]
    public async Task Resolve_Parallel_SubField_NonNull_SharedEntryField_Nullable_One_Service_Errors_SharedEntryField()
    {
        // arrange
        var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer @error
            }

            type Viewer {
              name: String!
            }
            """);

        var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              userId: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                userId
                name
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Error is not correctly shown")]
    public async Task Resolve_Parallel_SubField_NonNull_SharedEntryField_NonNull_One_Service_Errors_SharedEntryField()
    {
        // arrange
        var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer! @error
            }

            type Viewer {
              name: String!
            }
            """);

        var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer!
            }

            type Viewer {
              userId: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                userId
                name
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Error is not correctly shown")]
    public async Task Resolve_Parallel_SubField_Nullable_SharedEntryField_Nullable_One_Service_Errors_SubField()
    {
        // arrange
        var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String @error
            }
            """);

        var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              userId: ID
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                userId
                name
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Error is not correctly shown")]
    public async Task Resolve_Parallel_SubField_NonNull_SharedEntryField_Nullable_One_Service_Errors_SubField()
    {
        // arrange
        var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String! @error
            }
            """);

        var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              userId: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                userId
                name
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Error is not correctly shown")]
    public async Task Resolve_Parallel_SubField_NonNull_SharedEntryField_NonNull_One_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer!
            }

            type Viewer {
              name: String! @error
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer!
            }

            type Viewer {
              userId: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                userId
                name
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    //     [Fact]
    //     public async Task
    //         Resolve_Parallel_SubField_Nullable_SharedEntryField_Nullable_One_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               viewer: Viewer
    //             }
    //
    //             type Viewer {
    //               name: String
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   viewer: Viewer
    //                 }
    //
    //                 type Viewer {
    //                   userId: ID
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         viewer {
    //                           userId
    //                           name
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }
    //
    //     [Fact]
    //     public async Task
    //         Resolve_Parallel_SubField_NonNull_SharedEntryField_Nullable_One_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               viewer: Viewer
    //             }
    //
    //             type Viewer {
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   viewer: Viewer
    //                 }
    //
    //                 type Viewer {
    //                   userId: ID!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         viewer {
    //                           userId
    //                           name
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }
    //
    //     [Fact]
    //     public async Task
    //         Resolve_Parallel_SubField_NonNull_SharedEntryField_NonNull_One_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               viewer: Viewer!
    //             }
    //
    //             type Viewer {
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   viewer: Viewer!
    //                 }
    //
    //                 type Viewer {
    //                   userId: ID!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         viewer {
    //                           userId
    //                           name
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }

    #endregion

    #region Parallel, No Shared Entry Field

    [Fact]
    public async Task Resolve_Parallel_SubField_Nullable_EntryField_Nullable_One_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              other: Other
            }

            type Other {
              userId: ID @error
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                name
              }
              other {
                userId
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Resolve_Parallel_SubField_NonNull_EntryField_Nullable_One_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              other: Other
            }

            type Other {
              userId: ID! @error
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                name
              }
              other {
                userId
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Resolve_Parallel_SubField_NonNull_EntryField_NonNull_One_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer!
            }

            type Viewer {
              name: String!
            }
            """,
            isTimingOut: true);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              other: Other!
            }

            type Other {
              userId: ID! @error
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                name
              }
              other {
                userId
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Resolve_Parallel_EntryField_Nullable_One_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              other: Other @error
            }

            type Other {
              userId: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                name
              }
              other {
                userId
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Resolve_Parallel_EntryField_NonNull_One_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer!
            }

            type Viewer {
              name: String
            }
            """,
            isTimingOut: true);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              other: Other! @error
            }

            type Other {
              userId: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                name
              }
              other {
                userId
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    //     [Fact]
    //     public async Task Resolve_Parallel_EntryField_Nullable_One_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               viewer: Viewer!
    //             }
    //
    //             type Viewer {
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   other: Other
    //                 }
    //
    //                 type Other {
    //                   userId: ID!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         viewer {
    //                           name
    //                         }
    //                         other {
    //                           userId
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }

    //     [Fact]
    //     public async Task Resolve_Parallel_EntryField_NonNull_One_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               viewer: Viewer!
    //             }
    //
    //             type Viewer {
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   other: Other!
    //                 }
    //
    //                 type Other {
    //                   userId: ID!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         viewer {
    //                           name
    //                         }
    //                         other {
    //                           userId
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }

    #endregion

    #region Entity Resolver

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_Nullable_EntryField_Nullable_First_Service_Errors_SubField()
    {
        // arrange
        var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
              productById(id: ID!): Product @lookup
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

        var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product implements Node {
              id: ID!
              score: Int
            }

            interface Node {
              id: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_NonNull_EntryField_Nullable_First_Service_Errors_SubField()
    {
        // arrange
        var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
              productById(id: ID!): Product @lookup
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

        var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product implements Node {
              id: ID!
              score: Int!
            }

            interface Node {
              id: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_NonNull_EntryField_NonNull_First_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
              productById(id: ID!): Product! @lookup
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

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product! @lookup
            }

            type Product implements Node {
              id: ID!
              score: Int!
            }

            interface Node {
              id: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_Nullable_EntryField_Nullable_Second_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
              productById(id: ID!): Product @lookup
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

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product implements Node {
              id: ID!
              score: Int @error
            }

            interface Node {
              id: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_NonNull_EntryField_Nullable_Second_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
              productById(id: ID!): Product @lookup
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

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product implements Node {
              id: ID!
              score: Int! @error
            }

            interface Node {
              id: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
              }
            }
            """);
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_NonNull_EntryField_NonNull_Second_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
              productById(id: ID!): Product! @lookup
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

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product! @lookup
            }

            type Product implements Node {
              id: ID!
              score: Int! @error
            }

            interface Node {
              id: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_Nullable_EntryField_Nullable_First_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
              productById(id: ID!): Product @lookup @error
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

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product implements Node {
              id: ID!
              score: Int
            }

            interface Node {
              id: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_NonNull_EntryField_Nullable_First_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup @error
            }

            type Product {
              id: ID!
              name: String!
              price: Float!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              score: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_NonNull_EntryField_NonNull_First_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product! @lookup @error
            }

            type Product {
              id: ID!
              name: String!
              price: Float!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product! @lookup
            }

            type Product {
              id: ID!
              score: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_Nullable_EntryField_Nullable_Second_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              name: String
              price: Float
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup @error
            }

            type Product {
              id: ID!
              score: Int
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_NonNull_EntryField_Nullable_Second_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              name: String!
              price: Float!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup @error
            }

            type Product {
              id: ID!
              score: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_SubField_NonNull_EntryField_NonNull_Second_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product! @lookup
            }

            type Product {
              id: ID!
              name: String!
              price: Float!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product! @lookup @error
            }

            type Product {
              id: ID!
              score: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_EntryField_Nullable_Both_Services_Error_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup @error
            }

            type Product {
              id: ID!
              name: String!
              price: Float!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup @error
            }

            type Product {
              id: ID!
              score: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_EntryField_NonNull_Both_Services_Error_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product! @lookup @error
            }

            type Product {
              id: ID!
              name: String!
              price: Float!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product! @lookup @error
            }

            type Product {
              id: ID!
              score: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    //     [Fact]
    //     public async Task
    //         Entity_Resolver_SubField_Nullable_EntryField_Nullable_Second_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               productById(id: ID!): Product
    //             }
    //
    //             type Product implements Node {
    //               id: ID!
    //               name: String
    //               price: Float
    //             }
    //
    //             interface Node {
    //               id: ID!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   productById(id: ID!): Product
    //                 }
    //
    //                 type Product implements Node {
    //                   id: ID!
    //                   score: Int
    //                 }
    //
    //                 interface Node {
    //                   id: ID!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         productById(id: "1") {
    //                           id
    //                           name
    //                           price
    //                           score
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }
    //
    //     [Fact]
    //     public async Task
    //         Entity_Resolver_SubField_NonNull_EntryField_Nullable_Second_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               productById(id: ID!): Product
    //             }
    //
    //             type Product implements Node {
    //               id: ID!
    //               name: String!
    //               price: Float!
    //             }
    //
    //             interface Node {
    //               id: ID!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   productById(id: ID!): Product
    //                 }
    //
    //                 type Product implements Node {
    //                   id: ID!
    //                   score: Int!
    //                 }
    //
    //                 interface Node {
    //                   id: ID!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         productById(id: "1") {
    //                           id
    //                           name
    //                           price
    //                           score
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }
    //
    //     [Fact]
    //     public async Task
    //         Entity_Resolver_SubField_NonNull_EntryField_NonNull_Second_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               productById(id: ID!): Product!
    //             }
    //
    //             type Product implements Node {
    //               id: ID!
    //               name: String!
    //               price: Float!
    //             }
    //
    //             interface Node {
    //               id: ID!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   productById(id: ID!): Product!
    //                 }
    //
    //                 type Product implements Node {
    //                   id: ID!
    //                   score: Int!
    //                 }
    //
    //                 interface Node {
    //                   id: ID!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         productById(id: "1") {
    //                           id
    //                           name
    //                           price
    //                           score
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }
    //
    //     [Fact]
    //     public async Task
    //         Entity_Resolver_SubField_Nullable_EntryField_Nullable_First_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   productById(id: ID!): Product
    //                 }
    //
    //                 type Product implements Node {
    //                   id: ID!
    //                   name: String
    //                   price: Float
    //                 }
    //
    //                 interface Node {
    //                   id: ID!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               productById(id: ID!): Product
    //             }
    //
    //             type Product implements Node {
    //               id: ID!
    //               score: Int
    //             }
    //
    //             interface Node {
    //               id: ID!
    //             }
    //             """
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         productById(id: "1") {
    //                           id
    //                           name
    //                           price
    //                           score
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }
    //
    //     [Fact]
    //     public async Task
    //         Entity_Resolver_SubField_NonNull_EntryField_Nullable_First_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   productById(id: ID!): Product
    //                 }
    //
    //                 type Product implements Node {
    //                   id: ID!
    //                   name: String!
    //                   price: Float!
    //                 }
    //
    //                 interface Node {
    //                   id: ID!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               productById(id: ID!): Product
    //             }
    //
    //             type Product implements Node {
    //               id: ID!
    //               score: Int!
    //             }
    //
    //             interface Node {
    //               id: ID!
    //             }
    //             """
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         productById(id: "1") {
    //                           id
    //                           name
    //                           price
    //                           score
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }
    //
    //     [Fact]
    //     public async Task
    //         Entity_Resolver_SubField_NonNull_EntryField_NonNull_First_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   productById(id: ID!): Product!
    //                 }
    //
    //                 type Product implements Node {
    //                   id: ID!
    //                   name: String!
    //                   price: Float!
    //                 }
    //
    //                 interface Node {
    //                   id: ID!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               productById(id: ID!): Product!
    //             }
    //
    //             type Product implements Node {
    //               id: ID!
    //               score: Int!
    //             }
    //
    //             interface Node {
    //               id: ID!
    //             }
    //             """
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         productById(id: "1") {
    //                           id
    //                           name
    //                           price
    //                           score
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }

    #endregion

    #region Resolve Sequence

    [Fact]
    public async Task Resolve_Sequence_SubField_Nullable_Parent_Nullable_One_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              brand: Brand
            }

            type Brand @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              brandById(id: ID!): Brand @lookup
            }

            type Brand {
              id: ID!
              name: String @error
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              product {
                id
                brand {
                  id
                  name
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Resolve_Sequence_SubField_NonNull_Parent_Nullable_One_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              brand: Brand
            }

            type Brand @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              brandById(id: ID!): Brand @lookup
            }

            type Brand {
              id: ID!
              name: String! @error
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              product {
                id
                brand {
                  id
                  name
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Resolve_Sequence_SubField_NonNull_Parent_NonNull_One_Service_Errors_SubField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              brand: Brand!
            }

            type Brand @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              brandById(id: ID!): Brand @lookup
            }

            type Brand {
              id: ID!
              name: String! @error
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              product {
                id
                brand {
                  id
                  name
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Resolve_Sequence_SubField_Nullable_Parent_Nullable_One_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              brand: Brand
            }

            type Brand @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              brandById(id: ID!): Brand @lookup @error
            }

            type Brand {
              id: ID!
              name: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              product {
                id
                brand {
                  id
                  name
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Resolve_Sequence_SubField_NonNull_Parent_Nullable_One_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              brand: Brand
            }

            type Brand @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              brandById(id: ID!): Brand @lookup @error
            }

            type Brand {
              id: ID!
              name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              product {
                id
                brand {
                  id
                  name
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Resolve_Sequence_SubField_NonNull_Parent_NonNull_One_Service_Errors_EntryField()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              brand: Brand!
            }

            type Brand @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              brandById(id: ID!): Brand @lookup @error
            }

            type Brand {
              id: ID!
              name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              product {
                id
                brand {
                  id
                  name
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    //     [Fact]
    //     public async Task
    //         Resolve_Sequence_SubField_Nullable_Parent_Nullable_Second_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               product: Product
    //             }
    //
    //             type Product {
    //               id: ID!
    //               brand: Brand
    //             }
    //
    //             type Brand {
    //               id: ID!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   brandById(id: ID!): Brand
    //                 }
    //
    //                 type Brand {
    //                   id: ID!
    //                   name: String
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         product {
    //                           id
    //                           brand {
    //                             id
    //                             name
    //                           }
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }
    //
    //     [Fact]
    //     public async Task
    //         Resolve_Sequence_SubField_NonNull_Parent_Nullable_Second_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               product: Product
    //             }
    //
    //             type Product {
    //               id: ID!
    //               brand: Brand
    //             }
    //
    //             type Brand {
    //               id: ID!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   brandById(id: ID!): Brand
    //                 }
    //
    //                 type Brand {
    //                   id: ID!
    //                   name: String!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         product {
    //                           id
    //                           brand {
    //                             id
    //                             name
    //                           }
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }
    //
    //     [Fact]
    //     public async Task
    //         Resolve_Sequence_SubField_NonNull_Parent_NonNull_Second_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               product: Product
    //             }
    //
    //             type Product {
    //               id: ID!
    //               brand: Brand!
    //             }
    //
    //             type Brand {
    //               id: ID!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   brandById(id: ID!): Brand
    //                 }
    //
    //                 type Brand {
    //                   id: ID!
    //                   name: String!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         product {
    //                           id
    //                           brand {
    //                             id
    //                             name
    //                           }
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }

    #endregion

    #region ResolveByKey

    //     [Fact]
    //     public async Task ResolveByKey_SubField_Nullable_ListItem_Nullable_Second_Service_Errors_SubField()
    //     {
    //         // arrange
    //         var subgraphA = CreateSourceSchema(
    //             "A",
    //             """
    //             type Query {
    //               products: [Product]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = CreateSourceSchema(
    //             "B",
    //             """
    //             type Query {
    //               productsById(ids: [ID!]!): [Product]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               price: Int @error(atIndex: 1)
    //             }
    //             """);
    //
    //         using var gateway = await CreateCompositeSchemaAsync(
    //         [
    //             ("A", subgraphA),
    //             ("B", subgraphB)
    //         ]);
    //
    //         // act
    //         using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //         using var result = await client.PostAsync(
    //             """
    //             query {
    //               products {
    //                 id
    //                 name
    //                 price
    //               }
    //             }
    //             """,
    //             new Uri("http://localhost:5000/graphql"));
    //
    //         // assert
    //         using var response = await result.ReadAsResultAsync();
    //         MatchSnapshot(gateway, request, response);
    //     }
    //
    //     [Fact]
    //     public async Task ResolveByKey_SubField_NonNull_ListItem_Nullable_Second_Service_Errors_SubField()
    //     {
    //         // arrange
    //         var subgraphA = CreateSourceSchema(
    //             "A",
    //             """
    //             type Query {
    //               products: [Product]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = CreateSourceSchema(
    //             "B",
    //             """
    //             type Query {
    //               productsById(ids: [ID!]!): [Product]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               price: Int! @error(atIndex: 1)
    //             }
    //             """);
    //
    //         using var gateway = await CreateCompositeSchemaAsync(
    //         [
    //             ("A", subgraphA),
    //             ("B", subgraphB)
    //         ]);
    //
    //         // act
    //         using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //         using var result = await client.PostAsync(
    //             """
    //             query {
    //               products {
    //                 id
    //                 name
    //                 price
    //               }
    //             }
    //             """,
    //             new Uri("http://localhost:5000/graphql"));
    //
    //         // assert
    //         using var response = await result.ReadAsResultAsync();
    //         MatchSnapshot(gateway, request, response);
    //     }
    //
    //     [Fact]
    //     public async Task ResolveByKey_SubField_NonNull_ListItem_NonNull_Second_Service_Errors_SubField()
    //     {
    //         // arrange
    //         var subgraphA = CreateSourceSchema(
    //             "A",
    //             """
    //             type Query {
    //               products: [Product!]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = CreateSourceSchema(
    //             "B",
    //             """
    //             type Query {
    //               productsById(ids: [ID!]!): [Product]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               price: Int! @error(atIndex: 1)
    //             }
    //             """);
    //
    //         using var gateway = await CreateCompositeSchemaAsync(
    //         [
    //             ("A", subgraphA),
    //             ("B", subgraphB)
    //         ]);
    //
    //         // act
    //         using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //         using var result = await client.PostAsync(
    //             """
    //             query {
    //               products {
    //                 id
    //                 name
    //                 price
    //               }
    //             }
    //             """,
    //             new Uri("http://localhost:5000/graphql"));
    //
    //         // assert
    //         using var response = await result.ReadAsResultAsync();
    //         MatchSnapshot(gateway, request, response);
    //     }
    //
    //     [Fact]
    //     public async Task ResolveByKey_SubField_Nullable_ListItem_Nullable_Second_Service_Errors_EntryField()
    //     {
    //         // arrange
    //         var subgraphA = CreateSourceSchema(
    //             "A",
    //             """
    //             type Query {
    //               products: [Product]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = CreateSourceSchema(
    //             "B",
    //             """
    //             type Query {
    //               productsById(ids: [ID!]!): [Product] @error
    //             }
    //
    //             type Product {
    //               id: ID!
    //               price: Int
    //             }
    //             """);
    //
    //         using var gateway = await CreateCompositeSchemaAsync(
    //         [
    //             ("A", subgraphA),
    //             ("B", subgraphB)
    //         ]);
    //
    //         // act
    //         using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //         using var result = await client.PostAsync(
    //             """
    //             query {
    //               products {
    //                 id
    //                 name
    //                 price
    //               }
    //             }
    //             """,
    //             new Uri("http://localhost:5000/graphql"));
    //
    //         // assert
    //         using var response = await result.ReadAsResultAsync();
    //         MatchSnapshot(gateway, request, response);
    //     }
    //
    //     [Fact]
    //     public async Task ResolveByKey_SubField_NonNull_ListItem_Nullable_Second_Service_Errors_EntryField()
    //     {
    //         // arrange
    //         var subgraphA = CreateSourceSchema(
    //             "A",
    //             """
    //             type Query {
    //               products: [Product]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = CreateSourceSchema(
    //             "B",
    //             """
    //             type Query {
    //               productsById(ids: [ID!]!): [Product] @error
    //             }
    //
    //             type Product {
    //               id: ID!
    //               price: Int!
    //             }
    //             """);
    //
    //         using var gateway = await CreateCompositeSchemaAsync(
    //         [
    //             ("A", subgraphA),
    //             ("B", subgraphB)
    //         ]);
    //
    //         // act
    //         using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //         using var result = await client.PostAsync(
    //             """
    //             query {
    //               products {
    //                 id
    //                 name
    //                 price
    //               }
    //             }
    //             """,
    //             new Uri("http://localhost:5000/graphql"));
    //
    //         // assert
    //         using var response = await result.ReadAsResultAsync();
    //         MatchSnapshot(gateway, request, response);
    //     }
    //
    //     [Fact]
    //     public async Task ResolveByKey_SubField_NonNull_ListItem_NonNull_Second_Service_Errors_EntryField()
    //     {
    //         // arrange
    //         var subgraphA = CreateSourceSchema(
    //             "A",
    //             """
    //             type Query {
    //               products: [Product!]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = CreateSourceSchema(
    //             "B",
    //             """
    //             type Query {
    //               productsById(ids: [ID!]!): [Product] @error
    //             }
    //
    //             type Product {
    //               id: ID!
    //               price: Int!
    //             }
    //             """);
    //
    //         using var gateway = await CreateCompositeSchemaAsync(
    //         [
    //             ("A", subgraphA),
    //             ("B", subgraphB)
    //         ]);
    //
    //         // act
    //         using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //         using var result = await client.PostAsync(
    //             """
    //             query {
    //               products {
    //                 id
    //                 name
    //                 price
    //               }
    //             }
    //             """,
    //             new Uri("http://localhost:5000/graphql"));
    //
    //         // assert
    //         using var response = await result.ReadAsResultAsync();
    //         MatchSnapshot(gateway, request, response);
    //     }

    //     [Fact]
    //     public async Task
    //         ResolveByKey_SubField_Nullable_ListItem_Nullable_Second_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               products: [Product]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   productsById(ids: [ID!]!): [Product]
    //                 }
    //
    //                 type Product {
    //                   id: ID!
    //                   price: Int
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         products {
    //                           id
    //                           name
    //                           price
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }
    //
    //     [Fact]
    //     public async Task
    //         ResolveByKey_SubField_NonNull_ListItem_Nullable_Second_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               products: [Product]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   productsById(ids: [ID!]!): [Product]
    //                 }
    //
    //                 type Product {
    //                   id: ID!
    //                   price: Int!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         products {
    //                           id
    //                           name
    //                           price
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }
    //
    //     [Fact]
    //     public async Task
    //         ResolveByKey_SubField_NonNull_ListItem_NonNull_Second_Service_Returns_TopLevel_Error_Without_Data()
    //     {
    //         // arrange
    //         var subgraphA = await TestSubgraph.CreateAsync(
    //             """
    //             type Query {
    //               products: [Product!]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = await TestSubgraph.CreateAsync(builder => builder
    //             .AddDocumentFromString(
    //                 """
    //                 type Query {
    //                   productsById(ids: [ID!]!): [Product]
    //                 }
    //
    //                 type Product {
    //                   id: ID!
    //                   price: Int!
    //                 }
    //                 """)
    //             .AddResolverMocking()
    //             .UseDefaultPipeline()
    //             .UseRequest(_ => context =>
    //             {
    //                 context.Result =
    //                     OperationResultBuilder.CreateError(ErrorBuilder.New().SetMessage("Top Level Error").Build());
    //                 return default;
    //             })
    //         );
    //
    //         using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
    //         var executor = await subgraphs.GetExecutorAsync();
    //         var request = """
    //                       query {
    //                         products {
    //                           id
    //                           name
    //                           price
    //                         }
    //                       }
    //                       """;
    //
    //         // act
    //         var result = await executor.ExecuteAsync(request);
    //
    //         // assert
    //         MatchMarkdownSnapshot(request, result);
    //     }

    #endregion

    // [Fact]
    // public async Task ErrorFilter_Is_Applied()
    // {
    //     // arrange
    //     var server = CreateSourceSchema(
    //         "A",
    //         """
    //         type Query {
    //           field: String @error
    //         }
    //         """);
    //
    //     using var gateway = await CreateCompositeSchemaAsync(
    //     [
    //         ("A", server)
    //     ],
    //     configure: builder =>
    //         builder.AddErrorFilter(error => error.WithMessage("REPLACED MESSAGE").WithCode("CUSTOM_CODE")));
    //
    //     // act
    //     using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //     using var result = await client.PostAsync(
    //         """
    //         query {
    //           field
    //         }
    //         """,
    //         new Uri("http://localhost:5000/graphql"));
    //
    //     // assert
    //     using var response = await result.ReadAsResultAsync();
    //     response.MatchInlineSnapshot("""
    //                                {
    //                                  "errors": [
    //                                    {
    //                                      "message": "REPLACED MESSAGE",
    //                                      "locations": [
    //                                        {
    //                                          "line": 2,
    //                                          "column": 3
    //                                        }
    //                                      ],
    //                                      "path": [
    //                                        "field"
    //                                      ],
    //                                      "extensions": {
    //                                        "code": "CUSTOM_CODE"
    //                                      }
    //                                    }
    //                                  ],
    //                                  "data": {
    //                                    "field": null
    //                                  }
    //                                }
    //                                """);
    // }
}
