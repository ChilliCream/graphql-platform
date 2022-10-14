using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Sorting;

public class QueryableSortVisitorExpressionTests : IClassFixture<SchemaCache>
{
    private static readonly Foo[] _fooEntities =
    {
        new Foo { Name = "Sam", LastName = "Sampleman", Bars = new List<Bar>() },
        new Foo
        {
            Name = "Foo", LastName = "Galoo", Bars = new List<Bar>() { new() { Value = "A" } }
        }
    };

    private readonly SchemaCache _cache;

    public QueryableSortVisitorExpressionTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Expression_WithMoreThanOneParameter_ThrowsException()
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("Foo")
                .Resolve(Array.Empty<Foo>())
                .UseSorting())
            .AddType(new SortInputType<Foo>(x => x
                .Field(x => x.LastName)
                .Extend()
                .OnBeforeCreate(x => x.Expression = (Foo x, string bar) => x.LastName == bar)))
            .AddSorting();

        // act
        async Task<IRequestExecutor> Call() => await builder.BuildRequestExecutorAsync();

        // assert
        var ex = await Assert.ThrowsAsync<SchemaException>(Call);
        ex.Errors.MatchSnapshot();
    }

    [Fact]
    public async Task Create_CollectionLengthExpression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooSortInputType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(order: { barLength: ASC}){ name lastName}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(order: { barLength: DESC}){ name lastName}}")
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(),
                    res1,
                    "ASC"),
                res2,
                "DESC")
            .MatchAsync();
        ;
    }

    public class Foo
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? LastName { get; set; }

        public List<Bar>? Bars { get; set; }
    }

    public class Bar
    {
        public int Id { get; set; }

        public string? Value { get; set; }
    }

    public class FooSortInputType : SortInputType<Foo>
    {
        protected override void Configure(ISortInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(x => x.Bars!.Count).Name("barLength");
        }
    }
}
