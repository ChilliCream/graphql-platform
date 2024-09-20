using HotChocolate.Execution;
using HotChocolate.Fusion.Shared;
using Xunit.Abstractions;
using static HotChocolate.Fusion.TestHelper;

namespace HotChocolate.Fusion;

public class SkipTests(ITestOutputHelper output)
{
    #region EntityResolver

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EntityResolver_Skip_On_EntityResolver(bool skip)
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product
            }

            type Product implements Node {
              id: ID!
              name: String!
            }

            interface Node {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           productById(id: "1") @skip(if: $skip) {
                             id
                             name
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
        if (skip)
        {
            Assert.False(subgraph.HasReceivedRequest);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EntityResolver_Skip_On_EntityResolver_Other_RootField_Selected(bool skip)
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product
              other: String
            }

            type Product implements Node {
              id: ID!
              name: String!
            }

            interface Node {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           productById(id: "1") @skip(if: $skip) {
                             id
                             name
                           }
                           other
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EntityResolver_Skip_On_EntityResolver_Fragment(bool skip)
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product
            }

            type Product implements Node {
              id: ID!
              name: String!
            }

            interface Node {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           ...Test @skip(if: $skip)
                         }

                         fragment Test on Query {
                           productById(id: "1") {
                             id
                             name
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
        if (skip)
        {
            Assert.False(subgraph.HasReceivedRequest);
        }
    }

    [Theory]
    [InlineData(true, Skip = "@skip isn't forwarded correctly")]
    [InlineData(false)]
    public async Task EntityResolver_Skip_On_EntityResolver_Fragment_Other_RootField_Selected(bool skip)
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product
              other: String
            }

            type Product implements Node {
              id: ID!
              name: String!
            }

            interface Node {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           ...Test @skip(if: $skip)
                           other
                         }

                         fragment Test on Query {
                           productById(id: "1") {
                             id
                             name
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EntityResolver_Skip_On_EntityResolver_Fragment_EntityResolver_Selected_Separately(bool skip)
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
            """
            type Query {
              productById(id: ID!): Product
            }

            type Product implements Node {
              id: ID!
              name: String!
            }

            interface Node {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           ...Test @skip(if: $skip)
                           productById(id: "1") {
                             id
                             name
                           }
                         }

                         fragment Test on Query {
                           productById(id: "1") {
                             id
                             name
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EntityResolver_Skip_On_SubField(bool skip)
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
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

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           productById(id: "1") {
                             name @skip(if: $skip)
                             price
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
    }

    [Theory]
    [InlineData(true, Skip = "@skip isn't forwarded correctly")]
    [InlineData(false)]
    public async Task EntityResolver_Skip_On_SubField_Fragment(bool skip)
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
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

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           productById(id: "1") {
                             ...Test @skip(if: $skip)
                             price
                           }
                         }

                         fragment Test on Product {
                           name
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EntityResolver_Skip_On_SubField_Fragment_SubField_Selected_Separately(bool skip)
    {
        // arrange
        var subgraph = await TestSubgraph.CreateAsync(
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

        using var subgraphs = new TestSubgraphCollection(output, [subgraph]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           productById(id: "1") {
                             ...Test @skip(if: $skip)
                             name
                             price
                           }
                         }

                         fragment Test on Product {
                           name
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    #endregion

    #region Parallel Resolve

    [Theory]
    [InlineData(true, Skip = "Subgraph unnecessarily called")]
    [InlineData(false)]
    public async Task Parallel_Resolve_Skip_On_EntryField(bool skip)
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
              userId: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           viewer {
                             name
                           }
                           other @skip(if: $skip) {
                             userId
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
        if (skip)
        {
            Assert.False(subgraphB.HasReceivedRequest);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Parallel_Resolve_Skip_On_EntryField_Other_RootField_Selected(bool skip)
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
              another: String
            }

            type Other {
              userId: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           viewer {
                             name
                           }
                           other @skip(if: $skip) {
                             userId
                           }
                           another
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
    }

    [Theory]
    [InlineData(true, Skip = "Subgraph unnecessarily called")]
    [InlineData(false)]
    public async Task Parallel_Resolve_Skip_On_EntryField_Fragment(bool skip)
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
              userId: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           viewer {
                             name
                           }
                           ...Test @skip(if: $skip)
                         }

                         fragment Test on Query {
                           other {
                             userId
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
        if (skip)
        {
            Assert.False(subgraphB.HasReceivedRequest);
        }
    }

    [Theory]
    [InlineData(true, Skip = "@skip isn't forwarded correctly")]
    [InlineData(false)]
    public async Task Parallel_Resolve_Skip_On_EntryField_Fragment_Other_RootField_Selected(bool skip)
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
              another: String
            }

            type Other {
              userId: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           viewer {
                             name
                           }
                           ...Test @skip(if: $skip)
                           another
                         }

                         fragment Test on Query {
                           other {
                             userId
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Parallel_Resolve_Skip_On_EntryField_Fragment_EntryField_Selected_Separately(bool skip)
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
              userId: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           viewer {
                             name
                           }
                           other {
                             userId
                           }
                           ...Test @skip(if: $skip)
                         }

                         fragment Test on Query {
                           other {
                             userId
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Parallel_Resolve_Skip_On_SubField(bool skip)
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
              userId: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           viewer {
                             name
                           }
                           other {
                             userId @skip(if: $skip)
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
    }

    [Theory]
    [InlineData(true, Skip = "@skip isn't forwarded correctly")]
    [InlineData(false)]
    public async Task Parallel_Resolve_Skip_On_SubField_Fragment(bool skip)
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
              userId: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           viewer {
                             name
                           }
                           other {
                             ...Test @skip(if: $skip)
                           }
                         }

                         fragment Test on Other {
                           userId
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Parallel_Resolve_Skip_On_SubField_Fragment_SubField_Selected_Separately(bool skip)
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
              userId: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           viewer {
                             name
                           }
                           other {
                             userId
                             ...Test @skip(if: $skip)
                           }
                         }

                         fragment Test on Other {
                           userId
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    #endregion

    #region Parallel Resolve - Shared Entry Field

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Parallel_Resolve_SharedEntryField_Skip_On_EntryField(bool skip)
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
              viewer: Viewer!
            }

            type Viewer {
              userId: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           viewer @skip(if: $skip) {
                             userId
                             name
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
        if (skip)
        {
            Assert.False(subgraphA.HasReceivedRequest);
            Assert.False(subgraphB.HasReceivedRequest);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Parallel_Resolve_SharedEntryField_Skip_On_EntryField_Other_RootField_Selected(bool skip)
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
              viewer: Viewer!
              other: String
            }

            type Viewer {
              userId: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           viewer @skip(if: $skip) {
                             userId
                             name
                           }
                           other
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
        if (skip)
        {
            Assert.False(subgraphA.HasReceivedRequest);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Parallel_Resolve_SharedEntryField_Skip_On_EntryField_Fragment(bool skip)
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
              viewer: Viewer!
            }

            type Viewer {
              userId: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           ...Test @skip(if: $skip)
                         }

                         fragment Test on Query {
                           viewer {
                             userId
                             name
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
        if (skip)
        {
            Assert.False(subgraphA.HasReceivedRequest);
            Assert.False(subgraphB.HasReceivedRequest);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Parallel_Resolve_SharedEntryField_Skip_On_EntryField_Fragment_Other_RootField_Selected(bool skip)
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
              viewer: Viewer!
              other: String
            }

            type Viewer {
              userId: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           ...Test @skip(if: $skip)
                           other
                         }

                         fragment Test on Query {
                           viewer {
                             userId
                             name
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
        if (skip)
        {
            Assert.False(subgraphA.HasReceivedRequest);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task
        Parallel_Resolve_SharedEntryField_Skip_On_EntryField_Fragment_EntryField_Selected_Separately(bool skip)
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
              viewer: Viewer!
            }

            type Viewer {
              userId: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           ...Test @skip(if: $skip)
                           viewer {
                             userId
                             name
                           }
                         }

                         fragment Test on Query {
                           viewer {
                             userId
                             name
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Theory]
    [InlineData(true, Skip = "Not sure what correct behavior would be")]
    [InlineData(false)]
    public async Task
        Parallel_Resolve_SharedEntryField_Skip_On_EntryField_Fragment_EntryField_Partially_Selected_Separately(
            bool skip)
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
              viewer: Viewer!
            }

            type Viewer {
              userId: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           ...Test @skip(if: $skip)
                           viewer {
                             name
                           }
                         }

                         fragment Test on Query {
                           viewer {
                             userId
                             name
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
        if (skip)
        {
            Assert.False(subgraphB.HasReceivedRequest);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Parallel_Resolve_SharedEntryField_Skip_On_SubField(bool skip)
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
              viewer: Viewer!
            }

            type Viewer {
              userId: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           viewer {
                             userId @skip(if: $skip)
                             name
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
    }

    [Theory]
    [InlineData(true, Skip = "Not sure what correct behavior would be")]
    [InlineData(false)]
    public async Task Parallel_Resolve_SharedEntryField_Skip_On_SubField_Fragment(bool skip)
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
              viewer: Viewer!
            }

            type Viewer {
              userId: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           viewer {
                             ...Test @skip(if: $skip)
                             name
                           }
                         }

                         fragment Test on Viewer {
                           userId
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
        if (skip)
        {
            Assert.False(subgraphB.HasReceivedRequest);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Parallel_Resolve_SharedEntryField_Skip_On_SubField_Fragment_SubField_Selected_Separately(
        bool skip)
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
              viewer: Viewer!
            }

            type Viewer {
              userId: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           viewer {
                             ...Test @skip(if: $skip)
                             userId
                             name
                           }
                         }

                         fragment Test on Viewer {
                           userId
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    #endregion

    #region Resolve Sequence

    [Theory]
    [InlineData(true, Skip = "Subgraph unnecessarily called")]
    [InlineData(false)]
    public async Task Resolve_Sequence_Skip_On_RootField(bool skip)
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

            type Brand implements Node {
              id: ID!
              name: String!
            }

            interface Node {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           product @skip(if: $skip) {
                             id
                             brand {
                               id
                               name
                             }
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
        if (skip)
        {
            Assert.False(subgraphA.HasReceivedRequest);
            Assert.False(subgraphB.HasReceivedRequest);
        }
    }

    [Theory]
    [InlineData(true, Skip = "Subgraph unnecessarily called")]
    [InlineData(false)]
    public async Task Resolve_Sequence_Skip_On_RootField_Fragment(bool skip)
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

            type Brand implements Node {
              id: ID!
              name: String!
            }

            interface Node {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           ...Test @skip(if: $skip)
                         }

                         fragment Test on Query {
                           product {
                             id
                             brand {
                               id
                               name
                             }
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
        if (skip)
        {
            Assert.False(subgraphA.HasReceivedRequest);
            Assert.False(subgraphB.HasReceivedRequest);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Resolve_Sequence_Skip_On_RootField_Other_RootField_Selected(bool skip)
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              product: Product
              other: String
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

            type Brand implements Node {
              id: ID!
              name: String!
            }

            interface Node {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           product @skip(if: $skip) {
                             id
                             brand {
                               id
                               name
                             }
                           }
                           other
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
        if (skip)
        {
            Assert.False(subgraphB.HasReceivedRequest);
        }
    }

    [Theory]
    [InlineData(true, Skip = "@skip isn't forwarded correctly")]
    [InlineData(false)]
    public async Task Resolve_Sequence_Skip_On_RootField_Fragment_Other_RootField_Selected(bool skip)
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              product: Product
              other: String
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

            type Brand implements Node {
              id: ID!
              name: String!
            }

            interface Node {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           ...Test @skip(if: $skip)
                           other
                         }

                         fragment Test on Query {
                           product {
                             id
                             brand {
                               id
                               name
                             }
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
        if (skip)
        {
            Assert.False(subgraphB.HasReceivedRequest);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Resolve_Sequence_Skip_On_EntryField(bool skip)
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

            type Brand implements Node {
              id: ID!
              name: String!
            }

            interface Node {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           product {
                             id
                             brand @skip(if: $skip) {
                               id
                               name
                             }
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
        if (skip)
        {
            Assert.False(subgraphB.HasReceivedRequest);
        }
    }

    [Theory]
    [InlineData(true, Skip = "@skip isn't forwarded correctly")]
    [InlineData(false)]
    public async Task Resolve_Sequence_Skip_On_EntryField_Fragment(bool skip)
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

            type Brand implements Node {
              id: ID!
              name: String!
            }

            interface Node {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           product {
                             id
                             ...Test @skip(if: $skip)
                           }
                         }

                         fragment Test on Product {
                           brand {
                             id
                             name
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
        if (skip)
        {
            Assert.False(subgraphB.HasReceivedRequest);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Resolve_Sequence_Skip_On_EntryField_Other_Field_Selected(bool skip)
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
              productById(id: ID!): Product
            }

            type Product implements Node {
              id: ID!
              other: String!
            }

            type Brand implements Node {
              id: ID!
              name: String!
            }

            interface Node {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           product {
                             id
                             brand @skip(if: $skip) {
                               id
                               name
                             }
                             other
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
    }

    [Theory]
    [InlineData(true, Skip = "@skip isn't forwarded correctly")]
    [InlineData(false)]
    public async Task Resolve_Sequence_Skip_On_EntryField_Fragment_Other_Field_Selected(bool skip)
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
              productById(id: ID!): Product
            }

            type Product implements Node {
              id: ID!
              other: String!
            }

            type Brand implements Node {
              id: ID!
              name: String!
            }

            interface Node {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           product {
                             id
                             ...Test @skip(if: $skip)
                             other
                           }
                         }

                         fragment Test on Product {
                           brand {
                             id
                             name
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Resolve_Sequence_Skip_On_EntryField_Fragment_EntryField_Selected_Separately(bool skip)
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

            type Brand implements Node {
              id: ID!
              name: String!
            }

            interface Node {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           product {
                             id
                             ...Test @skip(if: $skip)
                             brand {
                               id
                               name
                             }
                           }
                         }

                         fragment Test on Product {
                           brand {
                             id
                             name
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Theory]
    [InlineData(true, Skip = "Subgraph unnecessarily called")]
    [InlineData(false)]
    public async Task Resolve_Sequence_Skip_On_SubField(bool skip)
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

            type Brand implements Node {
              id: ID!
              name: String!
            }

            interface Node {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           product {
                             id
                             brand {
                               id
                               name @skip(if: $skip)
                             }
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
        if (skip)
        {
            Assert.False(subgraphB.HasReceivedRequest);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Resolve_Sequence_Skip_On_SubField_Other_Field_Selected(bool skip)
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

            type Brand implements Node {
              id: ID!
              name: String!
              other: String!
            }

            interface Node {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           product {
                             id
                             brand {
                               id
                               name @skip(if: $skip)
                               other
                             }
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
    }

    [Theory]
    [InlineData(true, Skip = "Subgraph unnecessarily called")]
    [InlineData(false)]
    public async Task Resolve_Sequence_Skip_On_SubField_Fragment(bool skip)
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

            type Brand implements Node {
              id: ID!
              name: String!
            }

            interface Node {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           product {
                             id
                             brand {
                               id
                               ...Test @skip(if: $skip)
                             }
                           }
                         }

                         fragment Test on Brand {
                           name
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
        if (skip)
        {
            Assert.False(subgraphB.HasReceivedRequest);
        }
    }

    [Theory]
    [InlineData(true, Skip = "@skip isn't forwarded correctly")]
    [InlineData(false)]
    public async Task Resolve_Sequence_Skip_On_SubField_Fragment_Other_Field_Selected(bool skip)
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

            type Brand implements Node {
              id: ID!
              name: String!
              other: String!
            }

            interface Node {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           product {
                             id
                             brand {
                               id
                               ...Test @skip(if: $skip)
                               other
                             }
                           }
                         }

                         fragment Test on Brand {
                           name
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Resolve_Sequence_Skip_On_SubField_Fragment_SubField_Selected_Separately(bool skip)
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

            type Brand implements Node {
              id: ID!
              name: String!
            }

            interface Node {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           product {
                             id
                             brand {
                               id
                               ...Test @skip(if: $skip)
                               name
                             }
                           }
                         }

                         fragment Test on Brand {
                           name
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    #endregion

    #region ResolveByKey

    [Theory]
    [InlineData(true, Skip = "Subgraph unnecessarily called")]
    [InlineData(false)]
    public async Task ResolveByKey_Skip_On_SubField(bool skip)
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
              price: Int!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           products {
                             id
                             name
                             price @skip(if: $skip)
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
        if (skip)
        {
            Assert.False(subgraphB.HasReceivedRequest);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResolveByKey_Skip_On_SubField_Other_Field_Selected(bool skip)
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
              price: Int!
              other: String!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           products {
                             id
                             name
                             price @skip(if: $skip)
                             other
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
    }

    [Theory]
    [InlineData(true, Skip = "Subgraph unnecessarily called")]
    [InlineData(false)]
    public async Task ResolveByKey_Skip_On_SubField_Fragment(bool skip)
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
              price: Int!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           products {
                             id
                             name
                             ...Test @skip(if: $skip)
                           }
                         }

                         fragment Test on Product {
                           price
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
        if (skip)
        {
            Assert.False(subgraphB.HasReceivedRequest);
        }
    }

    [Theory]
    [InlineData(true, Skip = "@skip isn't forwarded correctly")]
    [InlineData(false)]
    public async Task ResolveByKey_Skip_On_SubField_Fragment_Other_Field_Selected(bool skip)
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
              price: Int!
              other: String!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           products {
                             id
                             name
                             ...Test @skip(if: $skip)
                             other
                           }
                         }

                         fragment Test on Product {
                           price
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result, postFix: skip);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResolveByKey_Skip_On_SubField_Fragment_SubField_Selected_Separately(bool skip)
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
              price: Int!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           products {
                             id
                             name
                             ...Test @skip(if: $skip)
                             price
                           }
                         }

                         fragment Test on Product {
                           price
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    #endregion

    #region Special cases
    [Fact]
    public async Task Error_On_SubField_Skip_On_Preceding_Field_In_Parent()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              brandById(id: ID!): Brand
              productById(id: ID!): Product
            }

            type Product implements Node {
              id: ID!
              skippedField: String
            }

            type Brand implements Node {
              id: ID!
              errorField: String @error
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
              brand: Brand!
            }

            type Brand implements Node {
              id: ID!
            }

            interface Node {
              id: ID!
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument("""
                         query Test($skip: Boolean!) {
                           productById(id: "1") {
                             skippedField @skip(if: $skip)
                             brand {
                               errorField
                             }
                           }
                         }
                         """)
            .SetVariableValues(new Dictionary<string, object?> { ["skip"] = true })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }
    #endregion
}
