using HotChocolate.Execution;
using HotChocolate.Fusion.Shared;
using HotChocolate.Types.Relay;
using Xunit.Abstractions;
using static HotChocolate.Language.Utf8GraphQLParser;
using static HotChocolate.Fusion.TestHelper;

namespace HotChocolate.Fusion;

public class InterfaceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Selections_On_Interface_List_Field()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              votables: [Votable]
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable {
              id: ID!
              viewerCanVote: Boolean!
              viewerRating: Float!
            }

            type Comment implements Votable {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = Parse("""
                            query testQuery {
                              votables {
                                viewerCanVote
                              }
                            }
                            """);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();
    }

    [Fact(Skip = "Fix with new planner")]
    public async Task Selections_On_Interface_List_Field_Interface_Selection_Has_Dependency()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              authorables: [Authorable]
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author {
              id: ID!
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              authorsById(ids: [ID!]!): [Author]!
            }

            type Author {
              id: ID!
              displayName: String!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = Parse("""
                            query testQuery {
                              authorables {
                                author {
                                  id
                                  displayName
                                }
                              }
                            }
                            """);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Selections_On_Interface_List_Field_And_Concrete_Type()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              votables: [Votable]
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable {
              id: ID!
              viewerCanVote: Boolean!
              title: String!
            }

            type Comment implements Votable {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = Parse("""
                            query testQuery {
                              votables {
                                viewerCanVote
                                ... on Discussion {
                                  title
                                }
                              }
                            }
                            """);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();
    }

    [Fact(Skip = "Fix with new planner")]
    public async Task Selections_On_Interface_List_Field_And_Concrete_Type_Interface_Selection_Has_Dependency()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              authorables: [Authorable]
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              title: String!
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author {
              id: ID!
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              authorsById(ids: [ID!]!): [Author]
            }

            type Author {
              id: ID!
              displayName: String!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = Parse("""
                            query testQuery {
                              authorables {
                                author {
                                  id
                                  displayName
                                }
                                ... on Discussion {
                                  title
                                }
                              }
                            }
                            """);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Selections_On_Interface_Field()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              votable: Votable
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable {
              id: ID!
              viewerCanVote: Boolean!
              viewerRating: Float!
            }

            type Comment implements Votable {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = Parse("""
                            query testQuery {
                              votable {
                                viewerCanVote
                              }
                            }
                            """);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();
    }

    [Fact(Skip = "Fix with new planner")]
    public async Task Selections_On_Interface_Field_Interface_Selection_Has_Dependency()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              authorable: Authorable
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author {
              id: ID!
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              authorById(id: ID!): Author
            }

            type Author {
              id: ID!
              displayName: String!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = Parse("""
                            query testQuery {
                              authorable {
                                author {
                                  id
                                  displayName
                                }
                              }
                            }
                            """);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Selections_On_Interface_Field_And_Concrete_Type()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              votable: Votable
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Votable {
              id: ID!
              viewerCanVote: Boolean!
              title: String!
            }

            type Comment implements Votable {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = Parse("""
                            query testQuery {
                              votable {
                                viewerCanVote
                                ... on Discussion {
                                  title
                                }
                              }
                            }
                            """);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();
    }

    [Fact(Skip = "Fix with new planner")]
    public async Task Selections_On_Interface_Field_And_Concrete_Type_Interface_Selection_Has_Dependency()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              authorable: Authorable
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Authorable {
              title: String!
              author: Author
            }

            type Comment implements Authorable {
              author: Author
            }

            type Author {
              id: ID!
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              authorById(id: ID!): Author
            }

            type Author {
              id: ID!
              displayName: String!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = Parse("""
                            query testQuery {
                              authorable {
                                author {
                                  id
                                  displayName
                                }
                                ... on Discussion {
                                  title
                                }
                              }
                            }
                            """);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Selections_On_Interface_On_Node_Field()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              node(id: ID!): Node
            }

            interface Node {
              id: ID!
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Node & Votable {
              id: ID!
              viewerCanVote: Boolean!
              viewerRating: Float!
            }

            type Comment implements Node & Votable {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = Parse("""
                            query testQuery($id: ID!) {
                              node(id: $id) {
                                ... on Votable {
                                  viewerCanVote
                                }
                              }
                            }
                            """);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?>
                {
                    ["id"] = new DefaultNodeIdSerializer().Format("Discussion", 1)
                })
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Selections_On_Interface_And_Concrete_Type_On_Node_Field()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              node(id: ID!): Node
            }

            interface Node {
              id: ID!
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Node & Votable {
              id: ID!
              viewerCanVote: Boolean!
              viewerRating: Float!
            }

            type Comment implements Node & Votable {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = Parse("""
                      query testQuery($id: ID!) {
                        node(id: $id) {
                          ... on Votable {
                            viewerCanVote
                          }
                          ... on Discussion {
                            viewerRating
                          }
                        }
                      }
                      """);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?>
                {
                    ["id"] = new DefaultNodeIdSerializer().Format("Discussion", 1)
                })
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();
    }

    [Fact(Skip = "Fix with new planner")]
    public async Task Selections_On_Interface_On_Node_Field_Interface_Selection_Has_Dependency()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              node(id: ID!): Node
            }

            interface Node {
              id: ID!
            }

            interface ProductList {
              products: [Product]
            }

            type Item1 implements Node & ProductList {
              id: ID!
              products: [Product]
            }

            type Item2 implements Node & ProductList {
              id: ID!
              products: [Product]
            }

            type Product implements Node {
              id: ID!
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              node(id: ID!): Node
              nodes(ids: [ID!]!): [Node]!
            }

            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              name: String
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = Parse("""
                      query testQuery($id: ID!) {
                        node(id: $id) {
                          __typename
                          id
                          ... on ProductList {
                            products {
                              id
                              name
                            }
                          }
                        }
                      }
                      """);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?>
                {
                    ["id"] = new DefaultNodeIdSerializer().Format("Item2", 1)
                })
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();
    }

    [Fact(Skip = "Fix with new planner")]
    public async Task Selections_On_Interface_And_Concrete_Type_On_Node_Field_Interface_Selection_Has_Dependency()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              node(id: ID!): Node
            }

            interface Node {
              id: ID!
            }

            interface ProductList {
              products: [Product]
            }

            type Item1 implements Node & ProductList {
              id: ID!
              products: [Product]
            }

            type Item2 implements Node & ProductList {
              id: ID!
              products: [Product]
              singularProduct: Product
            }

            type Product implements Node {
              id: ID!
            }
            """);

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              node(id: ID!): Node
              nodes(ids: [ID!]!): [Node]!
            }

            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              name: String
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = Parse("""
                      query testQuery($id: ID!) {
                        node(id: $id) {
                          __typename
                          id
                          ... on ProductList {
                            products {
                              id
                              name
                            }
                          }
                          ... on Item2 {
                            singularProduct {
                              name
                            }
                          }
                        }
                      }
                      """);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?>
                {
                    ["id"] = new DefaultNodeIdSerializer().Format("Item2", 1)
                })
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();
    }
}
