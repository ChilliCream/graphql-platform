using System.Linq.Expressions;
using System.Text.Json;
using GreenDonut.Data;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class IntegrationTests
{
    [Fact]
    public async Task Projection_Should_NotBreakProjections_When_ExtensionsFieldRequested()
    {
        // arrange
        // act
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<FooExtensions>()
            .AddProjections()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var result = await executor.ExecuteAsync(
            @"
            {
                foos {
                    bar
                    baz
                }
            }
            ",
            TestContext.Current.CancellationToken);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Projection_Should_NotThrow_When_ParentRequiresObjectField()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(q => q
                .Field("foo")
                .Resolve(_ => new[] { new RequiresFoo { Bar = new RequiresBar { Baz = "baz" } } }.AsQueryable())
                .UseProjection())
            .AddObjectType<RequiresFoo>(c =>
            {
                c.Field(f => f.Bar);
                c.Field("quux")
                    .Resolve(ctx => ctx.Parent<RequiresFoo>().Bar)
                    .ParentRequires<RequiresFoo>(f => f.Bar!);
            })
            .AddProjections()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            query {
                foo {
                    quux {
                        baz
                    }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ParentRequires_Should_ProjectRequiredProperty_When_ResolverFieldNameCollides()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<ParentRequiresCollisionQuery>()
            .AddTypeExtension<ParentRequiresCollisionItemExtensions>()
            .AddProjections()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              items {
                code
                label
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "items": [
                  {
                    "code": "computed",
                    "label": "ABC"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Projection_Should_NotProjectBackingProperty_When_PagedExtensionResolverShadowsProperty()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<PagingCollisionQuery>()
            .AddTypeExtension<PagingCollisionItemExtensions>()
            .AddProjections()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              items(first: 1) {
                nodes {
                  details {
                    code
                  }
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "items": {
                  "nodes": [
                    {
                      "details": {
                        "code": "computed"
                      }
                    }
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task ParentRequires_Should_ProjectNestedRequiredProperty_When_ResolverFieldNameCollides()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<NestedParentRequiresQuery>()
            .AddTypeExtension<NestedParentRequiresItemExtensions>()
            .AddTypeExtension<NestedParentRequiresDetailsExtensions>()
            .AddProjections()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              items {
                label
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "items": [
                  {
                    "label": "stored"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ParentRequires_Should_ProjectRequiredProperty_When_FieldIsPaged()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<PagedParentRequiresQuery>()
            .AddTypeExtension<PagedParentRequiresItemExtensions>()
            .AddProjections()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              items {
                nodes {
                  nameLength
                }
                edges {
                  node {
                    nameLength
                  }
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "items": {
                  "nodes": [
                    {
                      "nameLength": 3
                    }
                  ],
                  "edges": [
                    {
                      "node": {
                        "nameLength": 3
                      }
                    }
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task ParentRequires_Should_MergeWithClientSelection_When_ClientSelectsRequiredNavigation()
    {
        // arrange
        var executor = await CreateMergedParentRequiresExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              books {
                author {
                  name
                }
                authorProfileUrl
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "books": [
                  {
                    "author": {
                      "name": "Jane Doe"
                    },
                    "authorProfileUrl": "/authors/42"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ParentRequires_Should_MergeWithClientSelection_When_FieldIsPaged()
    {
        // arrange
        var executor = await CreateMergedParentRequiresExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              booksCursor {
                nodes {
                  author {
                    name
                  }
                  authorProfileUrl
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "booksCursor": {
                  "nodes": [
                    {
                      "author": {
                        "name": "Jane Doe"
                      },
                      "authorProfileUrl": "/authors/42"
                    }
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task ParentRequires_Should_MergeWithClientSelection_When_ClientSelectsRequiredLeaf()
    {
        // arrange
        var executor = await CreateMergedParentRequiresExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              booksCursor {
                nodes {
                  author {
                    name
                    id
                  }
                  authorProfileUrl
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "booksCursor": {
                  "nodes": [
                    {
                      "author": {
                        "name": "Jane Doe",
                        "id": 42
                      },
                      "authorProfileUrl": "/authors/42"
                    }
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task ParentRequires_Should_MergeWithClientSelection_When_RequirementSelectsWholeNavigation()
    {
        // arrange
        var executor = await CreateMergedParentRequiresExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              books {
                authorLabel
                author {
                  name
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "books": [
                  {
                    "authorLabel": "Jane Doe (42)",
                    "author": {
                      "name": "Jane Doe"
                    }
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Projection_Should_MergeSelections_When_NavigationIsSelectedUnderTwoAliases()
    {
        // arrange
        var executor = await CreateMergedParentRequiresExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              books {
                a: author {
                  name
                }
                b: author {
                  id
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "books": [
                  {
                    "a": {
                      "name": "Jane Doe"
                    },
                    "b": {
                      "id": 42
                    }
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Projection_Should_MergeSelections_When_ListIsSelectedUnderTwoAliases()
    {
        // arrange
        var executor = await CreateMergedParentRequiresExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              books {
                a: reviews {
                  text
                }
                b: reviews {
                  rating
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "books": [
                  {
                    "a": [
                      {
                        "text": "Great"
                      },
                      {
                        "text": "Fine"
                      }
                    ],
                    "b": [
                      {
                        "rating": 4
                      },
                      {
                        "rating": 2
                      }
                    ]
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [InlineData("where: { rating: { eq: 4 } }")]
    [InlineData("where: { text: { contains: \"r\" } }")]
    [InlineData("where: { rating: { gt: 2 }, text: { startsWith: \"G\" } }")]
    public async Task Projection_Should_PreserveFiltering_When_ListIsSelectedUnderTwoAliases(string arguments)
    {
        // arrange
        var executor = await CreateMergedParentRequiresExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            $$"""
            {
              books {
                a: reviews({{arguments}}) { text }
                b: reviews({{arguments}}) { rating }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "books": [
                  {
                    "a": [
                      {
                        "text": "Great"
                      }
                    ],
                    "b": [
                      {
                        "rating": 4
                      }
                    ]
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [InlineData("order: { rating: ASC }")]
    [InlineData("order: [{ rating: ASC }, { text: DESC }]")]
    public async Task Projection_Should_PreserveSorting_When_ListIsSelectedUnderTwoAliases(string arguments)
    {
        // arrange
        var executor = await CreateMergedParentRequiresExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            $$"""
            {
              books {
                a: reviews({{arguments}}) { text }
                b: reviews({{arguments}}) { rating }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "books": [
                  {
                    "a": [
                      {
                        "text": "Fine"
                      },
                      {
                        "text": "Great"
                      }
                    ],
                    "b": [
                      {
                        "rating": 2
                      },
                      {
                        "rating": 4
                      }
                    ]
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [InlineData("book")]
    [InlineData("optionalBook")]
    public async Task Projection_Should_PreserveNestedFiltering_When_FirstAliasSelectsTypename(string navigation)
    {
        // arrange
        var executor = await CreateMergedParentRequiresExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            $$"""
            {
              bookContainers {
                a: {{navigation}} { __typename }
                b: {{navigation}} {
                  reviews(where: { rating: { eq: 4 } }) { text }
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "bookContainers": [
                  {
                    "a": {
                      "__typename": "MergedParentRequiresBook"
                    },
                    "b": {
                      "reviews": [
                        {
                          "text": "Great"
                        }
                      ]
                    }
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [InlineData("book")]
    [InlineData("optionalBook")]
    public async Task Projection_Should_PreserveNestedSorting_When_FirstAliasSelectsTypename(string navigation)
    {
        // arrange
        var executor = await CreateMergedParentRequiresExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            $$"""
            {
              bookContainers {
                a: {{navigation}} { __typename }
                b: {{navigation}} {
                  reviews(order: { rating: ASC }) { text }
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "bookContainers": [
                  {
                    "a": {
                      "__typename": "MergedParentRequiresBook"
                    },
                    "b": {
                      "reviews": [
                        {
                          "text": "Fine"
                        },
                        {
                          "text": "Great"
                        }
                      ]
                    }
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ParentRequires_Should_ProjectListElementMembers_When_RequirementSelectsListElements()
    {
        // arrange
        var executor = await CreateListParentRequiresExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              books {
                averageRating
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "books": [
                  {
                    "averageRating": 3
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ParentRequires_Should_MergeWithClientSelection_When_ClientSelectsRequiredList()
    {
        // arrange
        var executor = await CreateListParentRequiresExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              books {
                reviews {
                  text
                }
                averageRating
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "books": [
                  {
                    "reviews": [
                      {
                        "text": "Great"
                      },
                      {
                        "text": "Fine"
                      }
                    ],
                    "averageRating": 3
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ParentRequires_Should_ProjectListElementMembers_When_SelectorIsBuiltFromSelection()
    {
        // arrange
        var executor = await CreateListParentRequiresExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              booksWithSelector {
                reviews {
                  text
                }
                averageRating
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "booksWithSelector": [
                  {
                    "reviews": [
                      {
                        "text": "Great"
                      },
                      {
                        "text": "Fine"
                      }
                    ],
                    "averageRating": 3
                  }
                ]
              }
            }
            """);
    }

    private static ValueTask<IRequestExecutor> CreateListParentRequiresExecutorAsync()
        => new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<MergedParentRequiresQuery>()
            .AddTypeExtension(typeof(ListParentRequiresBookExtensions))
            .AddProjections()
            .AddFiltering()
            .AddSorting()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

    private static ValueTask<IRequestExecutor> CreateMergedParentRequiresExecutorAsync()
        => new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<MergedParentRequiresQuery>()
            .AddTypeExtension(typeof(MergedParentRequiresBookExtensions))
            .AddProjections()
            .AddFiltering()
            .AddSorting()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

    [Fact]
    public async Task Projection_Should_NotBreakProjections_When_ExtensionsListRequested()
    {
        // arrange
        // act
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<FooExtensions>()
            .AddProjections()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var result = await executor.ExecuteAsync(
            @"
            {
                foos {
                    bar
                    qux
                }
            }
            ",
            TestContext.Current.CancellationToken);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Projection_Should_NotBreakProjections_When_ExtensionsObjectListRequested()
    {
        // arrange
        // act
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<FooExtensions>()
            .AddProjections()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var result = await executor.ExecuteAsync(
            @"
            {
                foos {
                    bar
                    nestedList {
                        bar
                    }
                }
            }
            ",
            TestContext.Current.CancellationToken);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Projection_Should_NotBreakProjections_When_ExtensionsObjectRequested()
    {
        // arrange
        // act
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<FooExtensions>()
            .AddProjections()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var result = await executor.ExecuteAsync(
            @"
            {
                foos {
                    bar
                    nested {
                        bar
                    }
                }
            }
            ",
            TestContext.Current.CancellationToken);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Projection_Should_NotThrow_When_OnlyNonProjectableExtensionFieldInNestedList()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithNestedListExtension>()
            .AddTypeExtension<ListItemExtensions>()
            .AddProjections()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        // 'computed' is the only selection inside the list.
        var result = await executor.ExecuteAsync(
            """
            {
                listParents {
                    items {
                        computed
                    }
                    id
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        // 'computed' has no backing column, so it contributes nothing to the projection.
        // As the only selection on the element, it leaves the list sub-projection empty, so
        // the list is not materialized and 'items' is empty. The trailing 'id' must still
        // bind against the parent. Materializing the list so element resolvers run when no
        // element column is selected is a separate change, not covered here.
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "listParents": [
                  {
                    "items": [],
                    "id": 1
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Projection_Should_Project_ArrayLength_Expression_Fields()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithExpressionProjection>()
            .AddType<CardReaderType>()
            .AddProjections()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                cardReaders {
                    cardReaderUidLength
                }
            }
            """,
            TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(result.ToJson());
        var readers = document.RootElement
            .GetProperty("data")
            .GetProperty("cardReaders");

        // assert
        Assert.Equal(2, readers.GetArrayLength());

        var enumerator = readers.EnumerateArray();
        Assert.True(enumerator.MoveNext());
        Assert.Equal(3, enumerator.Current.GetProperty("cardReaderUidLength").GetInt32());
        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, enumerator.Current.GetProperty("cardReaderUidLength").GetInt32());
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public async Task Projection_Should_Project_ComputedExpression_Field_Dependencies()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithComputedExpressionProjection>()
            .AddType<ExpressionPersonType>()
            .AddProjections()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                people {
                    firstName
                    fullName
                }
            }
            """,
            TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(result.ToJson());
        var people = document.RootElement
            .GetProperty("data")
            .GetProperty("people");

        // assert
        Assert.Equal(2, people.GetArrayLength());

        var enumerator = people.EnumerateArray();
        Assert.True(enumerator.MoveNext());
        Assert.Equal("Jane", enumerator.Current.GetProperty("firstName").GetString());
        Assert.Equal("Jane Doe", enumerator.Current.GetProperty("fullName").GetString());
        Assert.True(enumerator.MoveNext());
        Assert.Equal("John", enumerator.Current.GetProperty("firstName").GetString());
        Assert.Equal("John Smith", enumerator.Current.GetProperty("fullName").GetString());
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public async Task Node_Resolver_With_SingleOrDefault_Schema()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithNodeResolvers>()
            .AddObjectType<Foo>(d => d.ImplementsNode().IdField(t => t.Bar))
            .AddObjectType<Bar>(d => d.ImplementsNode().IdField(t => t.IdOfBar))
            .AddObjectType<Baz>(d => d.ImplementsNode().IdField(t => t.Bar2))
            .AddGlobalObjectIdentification()
            .AddProjections()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Node_Resolver_With_SingleOrDefault()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithNodeResolvers>()
            .AddObjectType<Foo>(d => d.ImplementsNode().IdField(t => t.Bar))
            .AddObjectType<Bar>(d => d.ImplementsNode().IdField(t => t.IdOfBar))
            .AddObjectType<Baz>(d => d.ImplementsNode().IdField(t => t.Bar2))
            .AddGlobalObjectIdentification()
            .AddProjections()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var result = await executor.ExecuteAsync(
            """
            {
              node(id: "Rm9vOkE=") {
                id
                __typename
                ... on Baz { fieldOfBaz }
                ... on Foo { fieldOfFoo }
                ... on Bar { fieldOfBar }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Node_Resolver_With_SingleOrDefault_Fragments()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithNodeResolvers>()
            .AddObjectType<Foo>(d => d.ImplementsNode().IdField(t => t.Bar))
            .AddObjectType<Bar>(d => d.ImplementsNode().IdField(t => t.IdOfBar))
            .AddObjectType<Baz>(d => d.ImplementsNode().IdField(t => t.Bar2))
            .AddGlobalObjectIdentification()
            .AddProjections()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var result = await executor
            .ExecuteAsync(
                """
                {
                    node(id: "Rm9vOkE=") {
                        id
                        __typename
                        ... on Baz { fieldOfBaz }
                        ... on Foo { fieldOfFoo }
                    }
                }
                """,
                TestContext.Current.CancellationToken);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Node_Resolver_Without_SingleOrDefault()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithNodeResolvers>()
            .AddObjectType<Foo>(d => d.ImplementsNode().IdField(t => t.Bar))
            .AddObjectType<Bar>(d => d.ImplementsNode().IdField(t => t.IdOfBar))
            .AddObjectType<Baz>(d => d.ImplementsNode().IdField(t => t.Bar2))
            .AddGlobalObjectIdentification()
            .AddProjections()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var result = await executor.ExecuteAsync(
            """
            {
              node(id: "QmFyOkE=") {
                id
                __typename
                ... on Baz { fieldOfBaz }
                ... on Foo { fieldOfFoo }
                ... on Bar { fieldOfBar }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Nodes_Resolver_With_SingleOrDefault()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithNodeResolvers>()
            .AddObjectType<Foo>(d => d.ImplementsNode().IdField(t => t.Bar))
            .AddObjectType<Bar>(d => d.ImplementsNode().IdField(t => t.IdOfBar))
            .AddObjectType<Baz>(d => d.ImplementsNode().IdField(t => t.Bar2))
            .AddGlobalObjectIdentification()
            .AddProjections()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var result = await executor.ExecuteAsync(
            @"{ nodes(ids: ""Rm9vOkE="") { id __typename } }",
            TestContext.Current.CancellationToken);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Nodes_Resolver_With_SingleOrDefault_Fragments()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithNodeResolvers>()
            .AddObjectType<Foo>(d => d.ImplementsNode().IdField(t => t.Bar))
            .AddObjectType<Bar>(d => d.ImplementsNode().IdField(t => t.IdOfBar))
            .AddObjectType<Baz>(d => d.ImplementsNode().IdField(t => t.Bar2))
            .AddGlobalObjectIdentification()
            .AddProjections()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var result = await executor
            .ExecuteAsync(
                """
                {
                    nodes(ids: "Rm9vOkE=") {
                        id
                        __typename
                        ... on Baz { fieldOfBaz }
                        ... on Foo { fieldOfFoo }
                    }
                }
                """,
                TestContext.Current.CancellationToken);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Nodes_Resolver_Without_SingleOrDefault()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithNodeResolvers>()
            .AddObjectType<Foo>(d => d.ImplementsNode().IdField(t => t.Bar))
            .AddObjectType<Bar>(d => d.ImplementsNode().IdField(t => t.IdOfBar))
            .AddObjectType<Baz>(d => d.ImplementsNode().IdField(t => t.Bar2))
            .AddGlobalObjectIdentification()
            .AddProjections()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var result = await executor
            .ExecuteAsync(
                """
                {
                    nodes(ids: "QmFyOkE=") {
                        id
                        __typename
                        ... on Baz { fieldOfBaz }
                        ... on Foo { fieldOfFoo }
                        ... on Bar { fieldOfBar }
                    }
                }
                """,
                TestContext.Current.CancellationToken);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Mutation_Convention_Select()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>() //error thrown without query, it's not needed for the test though
            .AddMutationType<Mutation>()
            .AddProjections()
            .AddMutationConventions()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var result = await executor.ExecuteAsync(
            """
              mutation {
                  modify {
                      foo {
                          bar
                      }
                  }
              }
              """,
            TestContext.Current.CancellationToken);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Mutation_Convention_HasError()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>() //error thrown without query, it's not needed for the test though
            .AddMutationType<Mutation>()
            .AddProjections()
            .AddMutationConventions()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var result = await executor.ExecuteAsync(
            """
            mutation {
                createRecord(input: {throwError: false}) {
                    foo {
                        bar
                    }
                    errors {
                        ... on Error {
                            message
                        }
                    }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Mutation_Convention_ThrowsError()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>() //error thrown without query, it's not needed for the test though
            .AddMutationType<Mutation>()
            .AddProjections()
            .AddMutationConventions()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var result = await executor.ExecuteAsync(
            """
            mutation {
                createRecord(input: {throwError: true}) {
                    foo {
                        bar
                    }
                    errors {
                        ... on Error {
                            message
                        }
                    }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Mutation_Convention_Select_With_SingleOrDefault()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>() //error thrown without query, it's not needed for the test though
            .AddMutationType<Mutation>()
            .AddProjections()
            .AddMutationConventions()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var result = await executor.ExecuteAsync(
            """
              mutation {
                  modifySingleOrDefault {
                      foo {
                          bar
                      }
                  }
              }
              """,
            TestContext.Current.CancellationToken);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Mutation_Convention_With_Relay_Projection_Schema()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithNodeResolvers>()
            .AddObjectType<Foo>(d => d.ImplementsNode().IdField(t => t.Bar))
            .AddObjectType<Bar>(d => d.ImplementsNode().IdField(t => t.IdOfBar))
            .AddObjectType<Baz>(d => d.ImplementsNode().IdField(t => t.Bar2))
            .AddGlobalObjectIdentification()
            .AddMutationType<Mutation>()
            .AddQueryFieldToMutationPayloads()
            .AddProjections()
            .AddMutationConventions()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Mutation_Convention_With_Relay_Projection()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithNodeResolvers>()
            .AddObjectType<Foo>(d => d.ImplementsNode().IdField(t => t.Bar))
            .AddObjectType<Bar>(d => d.ImplementsNode().IdField(t => t.IdOfBar))
            .AddObjectType<Baz>(d => d.ImplementsNode().IdField(t => t.Bar2))
            .AddGlobalObjectIdentification()
            .AddMutationType<Mutation>()
            .AddQueryFieldToMutationPayloads()
            .AddProjections()
            .AddMutationConventions()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var result = await executor.ExecuteAsync(
            """
            mutation {
                createRecord(input: {throwError: false}) {
                    foo {
                        id
                        fieldOfFoo
                    }
                    errors {
                        ... on Error {
                            message
                        }
                    }
                    query {
                        node(id: "QmFyOkE=") {
                            id
                            __typename
                            ... on Baz { fieldOfBaz }
                            ... on Foo { fieldOfFoo }
                            ... on Bar { fieldOfBar }
                        }
                    }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Mutation_Convention_QueryContext_Selector()
    {
        // arrange
        var capture = new QueryContextSelectorCapture();
        var executor = await new ServiceCollection()
            .AddSingleton(capture)
            .AddGraphQL()
            .AddQueryType(c => c.Name("Query").Field("abc").Resolve("def"))
            .AddMutationType<MutationWithQueryContext>()
            .AddQueryContext()
            .AddMutationConventions()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            mutation {
                modifyUser(input: { userName: "abc" }) {
                    userProfile {
                        userName
                    }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "modifyUser": {
                  "userProfile": {
                    "userName": "abc"
                  }
                }
              }
            }
            """);

        // the selector must project the selected userName field only,
        // not fall back to an identity selector that fetches the entire entity.
        var selector = capture.Selector;
        Assert.NotNull(selector);
        var projected = selector.Compile().Invoke(new UserProfile { UserName = "abc", DisplayName = "def" });
        Assert.Equal("abc", projected.UserName);
        Assert.Null(projected.DisplayName);
    }

    [Fact]
    public async Task Mutation_Convention_QueryContext_Selector_Merges_Aliased_Data_Fields()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddSingleton(new QueryContextSelectorCapture())
            .AddGraphQL()
            .AddQueryType(c => c.Name("Query").Field("abc").Resolve("def"))
            .AddMutationType<MutationWithQueryContext>()
            .AddQueryContext()
            .AddMutationConventions()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            mutation {
                modifyUser(input: { userName: "abc" }) {
                    a: userProfile { userName }
                    b: userProfile { displayName }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "modifyUser": {
                  "a": {
                    "userName": "abc"
                  },
                  "b": {
                    "displayName": "Display-abc"
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Mutation_Convention_QueryContext_Without_Data_Field_Selection()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddSingleton(new QueryContextSelectorCapture())
            .AddGraphQL()
            .AddQueryType(c => c.Name("Query").Field("abc").Resolve("def"))
            .AddMutationType<MutationWithQueryContext>()
            .AddQueryContext()
            .AddMutationConventions()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            mutation {
                modifyUser(input: { userName: "abc" }) {
                    __typename
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "modifyUser": {
                  "__typename": "ModifyUserPayload"
                }
              }
            }
            """);
    }
}

public class Query
{
    [UseProjection]
    public IQueryable<Foo> Foos
        => new Foo[] { new() { Bar = "A" }, new() { Bar = "B" } }.AsQueryable();
}

public class QueryWithExpressionProjection
{
    [UseProjection]
    public IQueryable<CardReader> CardReaders
        => new[]
        {
            new CardReader { CardReaderUid = [1, 2, 3] },
            new CardReader { CardReaderUid = [1] }
        }.AsQueryable();
}

public class QueryWithComputedExpressionProjection
{
    [UseProjection]
    public IQueryable<ExpressionPerson> People
        => new[]
        {
            new ExpressionPerson { FirstName = "Jane", LastName = "Doe" },
            new ExpressionPerson { FirstName = "John", LastName = "Smith" }
        }.AsQueryable();
}

public class CardReaderType : ObjectType<CardReader>
{
    protected override void Configure(IObjectTypeDescriptor<CardReader> descriptor)
    {
        descriptor.Field(x => x.CardReaderUid.Length).Name("cardReaderUidLength");
    }
}

public class ExpressionPersonType : ObjectType<ExpressionPerson>
{
    protected override void Configure(IObjectTypeDescriptor<ExpressionPerson> descriptor)
    {
        descriptor.Field(x => x.FirstName + " " + x.LastName).Name("fullName");
    }
}

public class CardReader
{
    public byte[] CardReaderUid { get; set; } = [];
}

public class ExpressionPerson
{
    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;
}

public class Mutation
{
    [UseMutationConvention]
    [UseProjection]
    public IQueryable<Foo> Modify()
    {
        return new Foo[] { new() { Bar = "A" }, new() { Bar = "B" } }.AsQueryable();
    }

    [UseMutationConvention]
    [UseSingleOrDefault]
    [UseProjection]
    public IQueryable<Foo> ModifySingleOrDefault()
        => new Foo[] { new() { Bar = "A" } }.AsQueryable();

    [Error<AnError>]
    [UseMutationConvention]
    [UseProjection]
    public IQueryable<Foo> CreateRecord(bool throwError)
    {
        if (throwError)
        {
            throw new AnError("this is only a test");
        }

        return new Foo[] { new() { Bar = "A" }, new() { Bar = "B" } }.AsQueryable();
    }

    public class AnError : Exception
    {
        public AnError(string message) : base(message)
        {
        }
    }
}

public class MutationWithQueryContext
{
    [UseMutationConvention]
    public UserProfile? ModifyUser(
        string userName,
        QueryContext<UserProfile> query,
        [Service] QueryContextSelectorCapture capture)
    {
        capture.Selector = query.Selector;

        var data = new UserProfile[]
        {
            new() { UserName = userName, DisplayName = "Display-" + userName }
        }.AsQueryable();

        return data.With(query).FirstOrDefault();
    }
}

public sealed class QueryContextSelectorCapture
{
    public Expression<Func<UserProfile, UserProfile>>? Selector { get; set; }
}

public class UserProfile
{
    public string? UserName { get; set; }

    public string? DisplayName { get; set; }
}

[ExtendObjectType(typeof(Foo))]
public class FooExtensions
{
    public string Baz => "baz";

    public IEnumerable<string> Qux => ["baz"];

    public IEnumerable<Foo> NestedList => [new Foo { Bar = "C" }];

    public Foo Nested => new() { Bar = "C" };
}

public class Foo
{
    public string? Bar { get; set; }
    public string FieldOfFoo => "fieldOfFoo";
}

public class RequiresFoo
{
    public RequiresBar? Bar { get; set; }
}

public class RequiresBar
{
    public string? Baz { get; set; }
}

public sealed class ParentRequiresCollisionItem
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;
}

[ExtendObjectType<ParentRequiresCollisionItem>]
public sealed class ParentRequiresCollisionItemExtensions
{
    public string GetCode([Parent] ParentRequiresCollisionItem item)
        => "computed";

    public string GetLabel(
        [Parent(requires: nameof(ParentRequiresCollisionItem.Code))]
        ParentRequiresCollisionItem item)
        => item.Code;
}

public sealed class ParentRequiresCollisionQuery
{
    [UseProjection]
    public IQueryable<ParentRequiresCollisionItem> GetItems()
        => new[]
        {
            new ParentRequiresCollisionItem
            {
                Id = 1,
                Code = "ABC"
            }
        }.AsQueryable();
}

public sealed class PagingCollisionItem
{
    public int Id { get; set; }

    public PagingCollisionDetails Details
    {
        get => throw new InvalidOperationException("The backing property must not be projected.");
        set { }
    }
}

public sealed class PagingCollisionDetails
{
    public string Code { get; set; } = string.Empty;
}

[ExtendObjectType<PagingCollisionItem>]
public sealed class PagingCollisionItemExtensions
{
    public PagingCollisionDetails GetDetails([Parent] PagingCollisionItem item)
        => new() { Code = "computed" };
}

public sealed class PagingCollisionQuery
{
    [UsePaging]
    [UseProjection]
    public IQueryable<PagingCollisionItem> GetItems()
        => new[]
        {
            new PagingCollisionItem { Id = 1 }
        }.AsQueryable();
}

public sealed class NestedParentRequiresItem
{
    public int Id { get; set; }

    public NestedParentRequiresDetails Details { get; set; } = new();
}

public sealed class NestedParentRequiresDetails
{
    public string Code { get; set; } = string.Empty;
}

[ExtendObjectType<NestedParentRequiresItem>]
public sealed class NestedParentRequiresItemExtensions
{
    public string GetLabel(
        [Parent(requires: "Details { Code }")]
        NestedParentRequiresItem item)
        => item.Details.Code;
}

[ExtendObjectType<NestedParentRequiresDetails>]
public sealed class NestedParentRequiresDetailsExtensions
{
    public string GetCode([Parent] NestedParentRequiresDetails details)
        => "computed";
}

public sealed class NestedParentRequiresQuery
{
    [UseProjection]
    public IQueryable<NestedParentRequiresItem> GetItems()
        => new[]
        {
            new NestedParentRequiresItem
            {
                Id = 1,
                Details = new NestedParentRequiresDetails { Code = "stored" }
            }
        }.AsQueryable();
}

public sealed class PagedParentRequiresItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

[ExtendObjectType<PagedParentRequiresItem>]
public sealed class PagedParentRequiresItemExtensions
{
    public int GetNameLength(
        [Parent(requires: nameof(PagedParentRequiresItem.Name))]
        PagedParentRequiresItem item)
        => item.Name.Length;
}

public sealed class PagedParentRequiresQuery
{
    [UsePaging]
    [UseProjection]
    public IQueryable<PagedParentRequiresItem> GetItems()
        => new[]
        {
            new PagedParentRequiresItem
            {
                Id = 1,
                Name = "abc"
            }
        }.AsQueryable();
}

public sealed class MergedParentRequiresBookContainer
{
    public required MergedParentRequiresBook Book { get; set; }

    public MergedParentRequiresBook? OptionalBook { get; set; }
}

public sealed class MergedParentRequiresBook
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public required MergedParentRequiresAuthor Author { get; set; }

    [UseFiltering]
    [UseSorting]
    public List<MergedParentRequiresReview> Reviews { get; set; } = [];
}

public sealed class MergedParentRequiresAuthor
{
    public int Id { get; set; }

    public required string Name { get; set; }
}

public sealed class MergedParentRequiresReview
{
    public int Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public int Rating { get; set; }
}

[ExtendObjectType<MergedParentRequiresBook>]
public static class MergedParentRequiresBookExtensions
{
    public static string GetAuthorProfileUrl(
        [Parent(requires: "{ Author { Id } }")]
        MergedParentRequiresBook book)
        => $"/authors/{book.Author.Id}";

    public static string GetAuthorLabel(
        [Parent(requires: "{ Author }")]
        MergedParentRequiresBook book)
        => $"{book.Author.Name} ({book.Author.Id})";
}

[ExtendObjectType<MergedParentRequiresBook>]
public static class ListParentRequiresBookExtensions
{
    public static double GetAverageRating(
        [Parent(requires: "{ Reviews { Rating } }")]
        MergedParentRequiresBook book)
        => book.Reviews.Average(t => t.Rating);
}

public sealed class MergedParentRequiresQuery
{
    private static readonly MergedParentRequiresBook[] s_books =
    [
        new()
        {
            Id = 1,
            Title = "Some Book",
            Author = new MergedParentRequiresAuthor { Id = 42, Name = "Jane Doe" },
            Reviews =
            [
                new MergedParentRequiresReview { Id = 1, Text = "Great", Rating = 4 },
                new MergedParentRequiresReview { Id = 2, Text = "Fine", Rating = 2 }
            ]
        }
    ];

    [UseProjection]
    public IQueryable<MergedParentRequiresBook> GetBooks() => s_books.AsQueryable();

    [UseProjection]
    public IQueryable<MergedParentRequiresBookContainer> GetBookContainers()
        => s_books.Select(book => new MergedParentRequiresBookContainer
        {
            Book = book,
            OptionalBook = book
        }).AsQueryable();

    [UsePaging]
    [UseProjection]
    public IQueryable<MergedParentRequiresBook> GetBooksCursor() => s_books.AsQueryable();

    public IQueryable<MergedParentRequiresBook> GetBooksWithSelector(ISelection selection)
        => s_books.AsQueryable().Select(selection.AsSelector<MergedParentRequiresBook>());
}

public class Baz
{
    public string? Bar2 { get; set; }

    public string FieldOfBaz => "fieldOfBaz";
}

public class Bar
{
    public string? IdOfBar { get; set; }

    public string FieldOfBar => "fieldOfBar";
}

public class QueryWithNodeResolvers
{
    [UseProjection]
    public IQueryable<Foo> All()
        => new Foo[] { new() { Bar = "A" } }.AsQueryable();

    [NodeResolver]
    [UseSingleOrDefault]
    [UseProjection]
    public IQueryable<Foo> GetById(string id)
        => new Foo[] { new() { Bar = "A" } }.AsQueryable();

    [NodeResolver]
    [UseSingleOrDefault]
    [UseProjection]
    public IQueryable<Baz> GetBazById(string id)
        => new Baz[] { new() { Bar2 = "A" } }.AsQueryable();

    [NodeResolver]
    public Bar GetBarById(string id) => new() { IdOfBar = "A" };
}

public class QueryWithNestedListExtension
{
    [UseProjection]
    public IQueryable<ListParent> ListParents
        => new[]
        {
            new ListParent
            {
                Id = 1,
                Items = [new ListItem { Id = 10, Value = "a" }, new ListItem { Id = 11, Value = "b" }]
            }
        }.AsQueryable();
}

public class ListParent
{
    public int Id { get; set; }

    public List<ListItem> Items { get; set; } = [];
}

public class ListItem
{
    public int Id { get; set; }

    public string Value { get; set; } = "";
}

[ExtendObjectType(typeof(ListItem))]
public class ListItemExtensions
{
    // Resolver whose value does not come from a ListItem column, so it is not projected.
    public string Computed() => "computed";
}
