using GreenDonut.Data;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Pagination;

public class PageInfoSchemaTests
{
    [Fact]
    public async Task Schema_Should_KeepPageInfoFieldNames_When_StreamPageConnectionIsExposed()
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQLServer()
            .ModifyPagingOptions(o => o.EnableRelativeCursors = true)
            .AddQueryType<Query>();

        // act
        var schema = await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.Types.GetType<ObjectType>("PageInfo").ToString().MatchInlineSnapshot(
            """
            "Information about pagination in a connection."
            type PageInfo {
              "Indicates whether more edges exist following the set defined by the clients arguments."
              hasNextPage: Boolean!
              "Indicates whether more edges exist prior the set defined by the clients arguments."
              hasPreviousPage: Boolean!
              "When paginating backwards, the cursor to continue."
              startCursor: String
              "When paginating forwards, the cursor to continue."
              endCursor: String
              "A list of cursors to continue paginating forwards."
              forwardCursors: [PageCursor!]!
              "A list of cursors to continue paginating backwards."
              backwardCursors: [PageCursor!]!
            }
            """);
    }

    [Fact]
    public async Task Schema_Should_ApplyShareableOnce_When_ClassicPageInfoIsComposite()
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQLServer()
            .ModifyOptions(o => o.ApplyShareableToPageInfo = true)
            .ModifyPagingOptions(o => o.EnableRelativeCursors = true)
            .AddQueryType<ClassicQuery>();

        // act
        var schema = await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.Types.GetType<ObjectType>("PageInfo").ToString().MatchInlineSnapshot(
            """
            "Information about pagination in a connection."
            type PageInfo @shareable {
              "Indicates whether more edges exist following the set defined by the clients arguments."
              hasNextPage: Boolean!
              "Indicates whether more edges exist prior the set defined by the clients arguments."
              hasPreviousPage: Boolean!
              "When paginating backwards, the cursor to continue."
              startCursor: String
              "When paginating forwards, the cursor to continue."
              endCursor: String
              "A list of cursors to continue paginating forwards."
              forwardCursors: [PageCursor!]!
              "A list of cursors to continue paginating backwards."
              backwardCursors: [PageCursor!]!
            }
            """);
        schema.Types.GetType<ObjectType>("PageCursor").ToString().MatchInlineSnapshot(
            """
            "A cursor that points to a specific page."
            type PageCursor @shareable {
              "The page number."
              page: Int!
              "The cursor."
              cursor: String!
            }
            """);
    }

    [Fact]
    public void Schema_Should_ApplyShareableOnce_When_SchemaBuilderIsUsed()
    {
        // arrange
        var builder = SchemaBuilder.New()
            .ModifyOptions(o => o.ApplyShareableToPageInfo = true)
            .ModifyPagingOptions(o => o.EnableRelativeCursors = true)
            .AddQueryType<ClassicQuery>();

        // act
        var schema = builder.Create();

        // assert
        schema.Types.GetType<ObjectType>("PageInfo").ToString().MatchInlineSnapshot(
            """
            "Information about pagination in a connection."
            type PageInfo @shareable {
              "Indicates whether more edges exist following the set defined by the clients arguments."
              hasNextPage: Boolean!
              "Indicates whether more edges exist prior the set defined by the clients arguments."
              hasPreviousPage: Boolean!
              "When paginating backwards, the cursor to continue."
              startCursor: String
              "When paginating forwards, the cursor to continue."
              endCursor: String
              "A list of cursors to continue paginating forwards."
              forwardCursors: [PageCursor!]!
              "A list of cursors to continue paginating backwards."
              backwardCursors: [PageCursor!]!
            }
            """);
        schema.Types.GetType<ObjectType>("PageCursor").ToString().MatchInlineSnapshot(
            """
            "A cursor that points to a specific page."
            type PageCursor @shareable {
              "The page number."
              page: Int!
              "The cursor."
              cursor: String!
            }
            """);
    }

    [Fact]
    public async Task Schema_Should_ApplyShareableOnce_When_StreamPageInfoIsComposite()
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQLServer()
            .ModifyOptions(o => o.ApplyShareableToPageInfo = true)
            .ModifyPagingOptions(o => o.EnableRelativeCursors = true)
            .AddQueryType<Query>();

        // act
        var schema = await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.Types.GetType<ObjectType>("PageInfo").ToString().MatchInlineSnapshot(
            """
            "Information about pagination in a connection."
            type PageInfo @shareable {
              "Indicates whether more edges exist following the set defined by the clients arguments."
              hasNextPage: Boolean!
              "Indicates whether more edges exist prior the set defined by the clients arguments."
              hasPreviousPage: Boolean!
              "When paginating backwards, the cursor to continue."
              startCursor: String
              "When paginating forwards, the cursor to continue."
              endCursor: String
              "A list of cursors to continue paginating forwards."
              forwardCursors: [PageCursor!]!
              "A list of cursors to continue paginating backwards."
              backwardCursors: [PageCursor!]!
            }
            """);
    }

    [Fact]
    public async Task PageInfo_Should_ResolveAllFields_When_QueriedOverStreamPageConnection()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .ModifyPagingOptions(o => o.EnableRelativeCursors = true)
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              streamed {
                pageInfo {
                  hasNextPage
                  hasPreviousPage
                  startCursor
                  endCursor
                  forwardCursors { page }
                  backwardCursors { page }
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
                "streamed": {
                  "pageInfo": {
                    "hasNextPage": false,
                    "hasPreviousPage": false,
                    "startCursor": null,
                    "endCursor": null,
                    "forwardCursors": [],
                    "backwardCursors": []
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Schema_Should_ExposeOnePageInfoType_When_ClassicPageConnectionAndStreamPageConnectionAreMixed()
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQLServer()
            .ModifyPagingOptions(o => o.EnableRelativeCursors = true)
            .AddQueryType<MixedQueryType>()
            .AddType<OrderPageConnectionType>();

        // act
        var schema = await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var snapshot = Snapshot.Create();

        var types = schema.Types
            .OfType<ObjectType>()
            .Where(t => t.Name.Contains("Connection") || t.Name == "PageInfo")
            .OrderBy(t => t.Name, StringComparer.Ordinal);

        foreach (var type in types)
        {
            snapshot.Add(type.ToString(), type.Name, MarkdownLanguages.GraphQL);
        }

        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task PageInfo_Should_CarryNoCostDirective_When_CostAnalysisIsEnabled()
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQLServer()
            .ModifyPagingOptions(o => o.EnableRelativeCursors = true)
            .AddQueryType<MixedQueryType>()
            .AddType<OrderPageConnectionType>();

        // act
        var schema = await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.Types.GetType<ObjectType>("PageInfo").ToString().MatchInlineSnapshot(
            """
            "Information about pagination in a connection."
            type PageInfo {
              "Indicates whether more edges exist following the set defined by the clients arguments."
              hasNextPage: Boolean!
              "Indicates whether more edges exist prior the set defined by the clients arguments."
              hasPreviousPage: Boolean!
              "When paginating backwards, the cursor to continue."
              startCursor: String
              "When paginating forwards, the cursor to continue."
              endCursor: String
              "A list of cursors to continue paginating forwards."
              forwardCursors: [PageCursor!]!
              "A list of cursors to continue paginating backwards."
              backwardCursors: [PageCursor!]!
            }
            """);
    }

    [Fact]
    public async Task Schema_Should_ExposeSeparateConnectionPageInfo_When_CustomConnectionMixesWithClassicPaging()
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<CustomMixedQuery>()
            .AddType<CustomConnectionType>();

        // act
        var schema = await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var snapshot = Snapshot.Create();

        var types = schema.Types
            .OfType<ObjectType>()
            .Where(t => t.Name.Contains("Connection") || t.Name.EndsWith("PageInfo"))
            .OrderBy(t => t.Name, StringComparer.Ordinal);

        foreach (var type in types)
        {
            snapshot.Add(type.ToString(), type.Name, MarkdownLanguages.GraphQL);
        }

        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task PageInfo_Should_ResolveEmptyCursorLists_When_QueriedOverClassicConnection()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .ModifyPagingOptions(o => o.EnableRelativeCursors = true)
            .AddQueryType<ClassicQuery>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              products(first: 2) {
                pageInfo {
                  hasNextPage
                  hasPreviousPage
                  startCursor
                  endCursor
                  forwardCursors { page }
                  backwardCursors { page }
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
                "products": {
                  "pageInfo": {
                    "hasNextPage": true,
                    "hasPreviousPage": false,
                    "startCursor": "MA==",
                    "endCursor": "MQ==",
                    "forwardCursors": [],
                    "backwardCursors": []
                  }
                }
              }
            }
            """);
    }

    [Theory]
    [InlineData(PagingFlavor.Classic)]
    [InlineData(PagingFlavor.PageConnection)]
    [InlineData(PagingFlavor.StreamPageConnection)]
    public async Task Schema_Should_ExposeFourFieldsAndNoPageCursor_When_NothingIsEnabled(PagingFlavor flavor)
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(GetQueryType(flavor, enabled: false));

        // act
        var schema = await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.Types.GetType<ObjectType>("PageInfo").ToString().MatchInlineSnapshot(
            """
            "Information about pagination in a connection."
            type PageInfo {
              "Indicates whether more edges exist following the set defined by the clients arguments."
              hasNextPage: Boolean!
              "Indicates whether more edges exist prior the set defined by the clients arguments."
              hasPreviousPage: Boolean!
              "When paginating backwards, the cursor to continue."
              startCursor: String
              "When paginating forwards, the cursor to continue."
              endCursor: String
            }
            """);
        Assert.False(schema.Types.TryGetType<ObjectType>("PageCursor", out _));
    }

    [Theory]
    [InlineData(PagingFlavor.Classic)]
    [InlineData(PagingFlavor.PageConnection)]
    [InlineData(PagingFlavor.StreamPageConnection)]
    public async Task Schema_Should_ExposeSixFields_When_RelativeCursorsAreEnabledGlobally(PagingFlavor flavor)
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQLServer()
            .ModifyPagingOptions(o => o.EnableRelativeCursors = true)
            .AddQueryType(GetQueryType(flavor, enabled: false));

        // act
        var schema = await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.Types.GetType<ObjectType>("PageInfo").ToString().MatchInlineSnapshot(
            """
            "Information about pagination in a connection."
            type PageInfo {
              "Indicates whether more edges exist following the set defined by the clients arguments."
              hasNextPage: Boolean!
              "Indicates whether more edges exist prior the set defined by the clients arguments."
              hasPreviousPage: Boolean!
              "When paginating backwards, the cursor to continue."
              startCursor: String
              "When paginating forwards, the cursor to continue."
              endCursor: String
              "A list of cursors to continue paginating forwards."
              forwardCursors: [PageCursor!]!
              "A list of cursors to continue paginating backwards."
              backwardCursors: [PageCursor!]!
            }
            """);
        Assert.True(schema.Types.TryGetType<ObjectType>("PageCursor", out _));
    }

    [Theory]
    [InlineData(PagingFlavor.Classic)]
    [InlineData(PagingFlavor.PageConnection)]
    [InlineData(PagingFlavor.StreamPageConnection)]
    public async Task Schema_Should_ExposeSixFieldsOnSharedType_When_OneFieldEnablesRelativeCursors(
        PagingFlavor flavor)
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(GetQueryType(flavor, enabled: true));

        // act
        var schema = await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.Types.GetType<ObjectType>("PageInfo").ToString().MatchInlineSnapshot(
            """
            "Information about pagination in a connection."
            type PageInfo {
              "Indicates whether more edges exist following the set defined by the clients arguments."
              hasNextPage: Boolean!
              "Indicates whether more edges exist prior the set defined by the clients arguments."
              hasPreviousPage: Boolean!
              "When paginating backwards, the cursor to continue."
              startCursor: String
              "When paginating forwards, the cursor to continue."
              endCursor: String
              "A list of cursors to continue paginating forwards."
              forwardCursors: [PageCursor!]!
              "A list of cursors to continue paginating backwards."
              backwardCursors: [PageCursor!]!
            }
            """);
        Assert.True(schema.Types.TryGetType<ObjectType>("PageCursor", out _));
    }

    [Fact]
    public async Task Schema_Should_ExposeSixFieldsOnOnePageInfo_When_OnlyOneOfTheMixedFieldsEnablesRelativeCursors()
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<MixedEnabledQuery>();

        // act
        var schema = await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.Types
            .OfType<ObjectType>()
            .Where(t => t.Name.EndsWith("PageInfo"))
            .Select(t => $"{t.Name}: {string.Join(", ", GetFieldNames(t))}")
            .ToArray()
            .MatchInlineSnapshot(
                """
                [
                  "PageInfo: hasNextPage, hasPreviousPage, startCursor, endCursor, forwardCursors, backwardCursors"
                ]
                """);
    }

    [Theory]
    [InlineData(PagingFlavor.Classic)]
    [InlineData(PagingFlavor.PageConnection)]
    [InlineData(PagingFlavor.StreamPageConnection)]
    public async Task PageInfo_Should_FailValidation_When_SelectingForwardCursorsAndNothingIsEnabled(
        PagingFlavor flavor)
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(GetQueryType(flavor, enabled: false))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            $"{{ {GetFieldName(flavor)} {{ pageInfo {{ forwardCursors {{ page }} }} }} }}",
            TestContext.Current.CancellationToken);

        // assert
        result.ExpectOperationResult().Errors![0].Message.MatchInlineSnapshot(
            "The field `forwardCursors` does not exist on the type `PageInfo`.");
    }

    private static IEnumerable<string> GetFieldNames(ObjectType type)
        => type.Fields.Where(t => !t.IsIntrospectionField).Select(t => t.Name);

    private static Type GetQueryType(PagingFlavor flavor, bool enabled)
        => (flavor, enabled) switch
        {
            (PagingFlavor.Classic, false) => typeof(ClassicQuery),
            (PagingFlavor.Classic, true) => typeof(ClassicEnabledQueryType),
            (PagingFlavor.PageConnection, false) => typeof(PagedQueryType),
            (PagingFlavor.PageConnection, true) => typeof(PagedEnabledQueryType),
            (PagingFlavor.StreamPageConnection, false) => typeof(Query),
            (PagingFlavor.StreamPageConnection, true) => typeof(StreamedEnabledQuery),
            _ => throw new ArgumentOutOfRangeException(nameof(flavor))
        };

    private static string GetFieldName(PagingFlavor flavor)
        => flavor switch
        {
            PagingFlavor.Classic => "products(first: 1)",
            PagingFlavor.PageConnection => "paged",
            PagingFlavor.StreamPageConnection => "streamed",
            _ => throw new ArgumentOutOfRangeException(nameof(flavor))
        };

    public enum PagingFlavor
    {
        Classic,
        PageConnection,
        StreamPageConnection
    }

    public sealed class Product
    {
        public int Id { get; set; }
    }

    public sealed class ClassicQuery
    {
        [UsePaging]
        public IEnumerable<Product> GetProducts()
            => [new Product { Id = 1 }, new Product { Id = 2 }, new Product { Id = 3 }];
    }

    public sealed class Order
    {
        public int Id { get; set; }
    }

    public sealed class MixedQuery
    {
        [UsePaging]
        public IEnumerable<Product> GetProducts() => [];

        public PageConnection<Order> GetPaged()
            => new(
                Page<Order>.Create(
                    [new Order { Id = 1 }, new Order { Id = 2 }],
                    hasNextPage: true,
                    hasPreviousPage: false,
                    order => order.Id.ToString(),
                    totalCount: 5));

        public StreamPageConnection<Product> GetStreamed()
            => new(StreamPage<Product>.Empty);
    }

    public sealed class MixedQueryType : ObjectType<MixedQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<MixedQuery> descriptor)
            => descriptor.Field(t => t.GetPaged()).Type<NonNullType<OrderPageConnectionType>>();
    }

    public sealed class OrderPageConnectionType : ObjectType<PageConnection<Order>>
    {
        protected override void Configure(IObjectTypeDescriptor<PageConnection<Order>> descriptor)
            => descriptor.Name("OrderConnection");
    }

    public sealed class CustomMixedQuery
    {
        [UsePaging]
        public IEnumerable<Product> GetProducts() => [];

        public CustomConnection GetCustom()
            => new(new ConnectionPageInfo(hasNextPage: true, hasPreviousPage: false, "a", "b"));
    }

    public sealed class CustomConnection(ConnectionPageInfo pageInfo)
        : ConnectionBase<Product, CustomEdge, ConnectionPageInfo>
    {
        public override IReadOnlyList<CustomEdge>? Edges { get; } = [new CustomEdge(new Product { Id = 1 })];

        public override ConnectionPageInfo PageInfo { get; } = pageInfo;
    }

    public sealed class CustomEdge(Product node) : IEdge<Product>
    {
        public string Cursor => node.Id.ToString();

        public Product Node => node;

        object? IEdge.Node => Node;
    }

    public sealed class CustomConnectionType : ObjectType<CustomConnection>;

    public sealed class ClassicUnattributedQuery
    {
        public IEnumerable<Product> GetProducts()
            => [new Product { Id = 1 }, new Product { Id = 2 }, new Product { Id = 3 }];
    }

    public sealed class ClassicEnabledQueryType : ObjectType<ClassicUnattributedQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<ClassicUnattributedQuery> descriptor)
            => descriptor
                .Field(t => t.GetProducts())
                .UsePaging(options: new PagingOptions { EnableRelativeCursors = true });
    }

    public sealed class PagedQuery
    {
        public PageConnection<Order> GetPaged()
            => new(Page<Order>.Create(
                [new Order { Id = 1 }],
                hasNextPage: false,
                hasPreviousPage: false,
                order => order.Id.ToString()));
    }

    public sealed class PagedQueryType : ObjectType<PagedQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<PagedQuery> descriptor)
            => descriptor.Field(t => t.GetPaged()).Type<NonNullType<OrderPageConnectionType>>();
    }

    public sealed class PagedEnabledQueryType : ObjectType<PagedQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<PagedQuery> descriptor)
            => descriptor
                .Field(t => t.GetPaged())
                .Type<NonNullType<OrderPageConnectionType>>()
                .Extend()
                .OnBeforeCreate((_, d) => d.Features.Set(new PagingOptions { EnableRelativeCursors = true }));
    }

    public sealed class StreamedEnabledQuery
    {
        [UseConnection(EnableRelativeCursors = true)]
        public StreamPageConnection<Product> GetStreamed()
            => new(StreamPage<Product>.Empty);
    }

    public sealed class MixedEnabledQuery
    {
        [UseConnection(EnableRelativeCursors = true)]
        public StreamPageConnection<Product> GetProducts()
            => new(StreamPage<Product>.Empty);

        [UsePaging]
        public IEnumerable<Order> GetOrders() => [];
    }

    public sealed class Query
    {
        public StreamPageConnection<Product> GetStreamed()
            => new(StreamPage<Product>.Empty);
    }
}
