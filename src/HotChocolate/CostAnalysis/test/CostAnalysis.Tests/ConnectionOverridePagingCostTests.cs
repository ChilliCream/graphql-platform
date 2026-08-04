using HotChocolate.CostAnalysis.Types;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

public class ConnectionOverridePagingCostTests
{
    // A connection field whose paging options come from [UseConnection] (not [UsePaging]) must
    // still honor the global MaxPageSize for its @listSize.assumedSize when the attribute does
    // not set one. Without the global merge the field falls back to the framework default (50)
    // and ignores the configured global maximum, which skews the cost analysis. The assumedSize
    // is the value the cost analyzer multiplies a connection's sized fields by, so it is the
    // schema artifact that drives the reported cost.
    [Theory]
    [InlineData(typeof(NoOverrideQueryType), 100)]
    [InlineData(typeof(Override50QueryType), 50)]
    [InlineData(typeof(Override100QueryType), 100)]
    public async Task ListSize_AssumedSize_Should_Honor_Global_Max_Page_Size_When_Override_Is_Absent(
        Type queryType,
        int expectedAssumedSize)
    {
        // arrange & act
        // the global MaxPageSize is 100; the connection field gets its paging options from
        // [UseConnection], which does not set a max page size unless one is provided.
        var assumedSize = await GetBooksAssumedSizeAsync(b => b.AddQueryType(queryType));

        // assert
        Assert.Equal(expectedAssumedSize, assumedSize);
    }

    // [UsePaging] and [UseConnection] are two ways to declare the same connection field, so they
    // must derive the same @listSize.assumedSize from the global MaxPageSize when no per-field
    // override is set. NoOverrideQueryType (connection flag + [UseConnection]) is the same shape a
    // source-generated connection resolver produces.
    [Fact]
    public async Task ListSize_AssumedSize_Should_Be_Equal_For_UsePaging_And_UseConnection_Without_Override()
    {
        // arrange & act
        var usePaging = await GetBooksAssumedSizeAsync(b => b.AddQueryType<UsePagingQuery>());
        var useConnection = await GetBooksAssumedSizeAsync(b => b.AddQueryType<NoOverrideQueryType>());

        // assert
        Assert.Equal(100, usePaging);
        Assert.Equal(usePaging, useConnection);
    }

    private static async Task<int?> GetBooksAssumedSizeAsync(
        Action<IRequestExecutorBuilder> configureQueryType)
    {
        var builder =
            new ServiceCollection()
                .AddGraphQLServer()
                .ModifyPagingOptions(o =>
                {
                    o.MaxPageSize = 100;
                    o.RequirePagingBoundaries = false;
                });

        configureQueryType(builder);

        var schema = await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        var listSize =
            schema.QueryType.Fields["books"].Directives
                .Where(d => d.Name == "listSize")
                .Select(d => d.ToValue<ListSizeDirective>())
                .SingleOrDefault();

        Assert.NotNull(listSize);
        return listSize.AssumedSize;
    }

    public class Book
    {
        public required string Title { get; set; }
    }

    public class UsePagingQuery
    {
        [UsePaging]
        public IQueryable<Book> GetBooks() => new List<Book>().AsQueryable();
    }

    public class NoOverrideQuery
    {
        [UseConnection]
        public Connection<Book> GetBooks()
            => new([], new ConnectionPageInfo(false, false, null, null));
    }

    public class Override50Query
    {
        [UseConnection(MaxPageSize = 50)]
        public Connection<Book> GetBooks()
            => new([], new ConnectionPageInfo(false, false, null, null));
    }

    public class Override100Query
    {
        [UseConnection(MaxPageSize = 100)]
        public Connection<Book> GetBooks()
            => new([], new ConnectionPageInfo(false, false, null, null));
    }

    // [UseConnection] sets the paging options feature but not the connection flag, so the field
    // is flagged as a connection here to mirror a source-generated connection resolver.
    public class NoOverrideQueryType : ObjectType<NoOverrideQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<NoOverrideQuery> descriptor)
            => descriptor.Field(t => t.GetBooks())
                .Extend().OnBeforeCreate((_, cfg) => cfg.SetConnectionFlags());
    }

    public class Override50QueryType : ObjectType<Override50Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Override50Query> descriptor)
            => descriptor.Field(t => t.GetBooks())
                .Extend().OnBeforeCreate((_, cfg) => cfg.SetConnectionFlags());
    }

    public class Override100QueryType : ObjectType<Override100Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Override100Query> descriptor)
            => descriptor.Field(t => t.GetBooks())
                .Extend().OnBeforeCreate((_, cfg) => cfg.SetConnectionFlags());
    }
}
