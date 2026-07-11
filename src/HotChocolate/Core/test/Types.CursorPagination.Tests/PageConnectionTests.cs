using GreenDonut.Data;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Pagination;

public class PageConnectionTests
{
    [Fact]
    public void ImplicitConversion_Should_WrapPage_When_ConvertingPageToConnection()
    {
        // arrange
        var page = Page<string>.Create(
            ["a", "b", "c"],
            hasNextPage: true,
            hasPreviousPage: false,
            item => item,
            totalCount: 10);

        // act
        PageConnection<string> connection = page;

        // assert
        Assert.Same(page, connection.Nodes);
        Assert.Equal(10, connection.TotalCount);
        Assert.Equal(3, connection.Edges!.Count);
        Assert.True(connection.PageInfo.HasNextPage);
    }

    [Fact]
    public void ImplicitConversion_Should_ThrowArgumentNullException_When_PageIsNull()
    {
        // arrange
        Page<string>? page = null;

        // act
        PageConnection<string> Convert() => page!;

        // assert
        Assert.Throws<ArgumentNullException>(Convert);
    }

    [Fact]
    public async Task TotalCount_Should_PropagateNonNullViolation_When_CountIsUnknown()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .AddType<StringPageConnectionType>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              unknown { totalCount }
              knownEmpty { totalCount }
              known { totalCount }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        var connectionType = Assert.IsAssignableFrom<ObjectType>(
            executor.Schema.Types.GetType<ObjectType>("Query").Fields["unknown"].Type.NamedType());
        Assert.Equal(
            "Int!",
            connectionType.Fields["totalCount"].Type.Print());
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Cannot return null for non-nullable field.",
                  "path": [
                    "unknown",
                    "totalCount"
                  ],
                  "extensions": {
                    "code": "HC0018"
                  }
                }
              ],
              "data": {
                "unknown": null,
                "knownEmpty": {
                  "totalCount": 0
                },
                "known": {
                  "totalCount": 2
                }
              }
            }
            """);
    }

    [Fact]
    public async Task TotalCount_Should_PreserveCustomResolverResult_When_ResolverIsExplicit()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<CustomQueryType>()
            .AddType<CustomStringPageConnectionType>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ custom { totalCount } }",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "custom": {
                  "totalCount": 42
                }
              }
            }
            """);
    }

    public sealed class Query
    {
        public PageConnection<string>? GetUnknown()
            => new(Page<string>.Empty);

        public PageConnection<string> GetKnownEmpty()
            => new(Page<string>.Create([], false, false, _ => string.Empty, totalCount: 0));

        public PageConnection<string> GetKnown()
            => new(Page<string>.Create(["a"], false, false, t => t, totalCount: 2));
    }

    public sealed class QueryType : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor.Field(t => t.GetUnknown()).Type<StringPageConnectionType>();
            descriptor.Field(t => t.GetKnownEmpty()).Type<NonNullType<StringPageConnectionType>>();
            descriptor.Field(t => t.GetKnown()).Type<NonNullType<StringPageConnectionType>>();
        }
    }

    public sealed class StringPageConnectionType : ObjectType<PageConnection<string>>
    {
        protected override void Configure(IObjectTypeDescriptor<PageConnection<string>> descriptor)
            => descriptor.Name("StringConnection");
    }

    public sealed class CustomQuery
    {
        public PageConnection<string> GetCustom()
            => new(Page<string>.Empty);
    }

    public sealed class CustomQueryType : ObjectType<CustomQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<CustomQuery> descriptor)
            => descriptor
                .Field(t => t.GetCustom())
                .Type<NonNullType<CustomStringPageConnectionType>>();
    }

    public sealed class CustomStringPageConnectionType : ObjectType<PageConnection<string>>
    {
        protected override void Configure(IObjectTypeDescriptor<PageConnection<string>> descriptor)
        {
            descriptor.Name("CustomStringConnection");
            descriptor.Field(t => t.TotalCount).Resolve(42);
        }
    }
}
