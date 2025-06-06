using HotChocolate.Execution;

namespace HotChocolate.Data.Sorting;

public class QueryableSortVisitorBooleanTests : IClassFixture<SchemaCache>
{
    private static readonly Foo[] s_fooEntities =
    [
        new() { Bar = true },
        new() { Bar = false }
    ];

    private static readonly FooNullable[] s_fooNullableEntities =
    [
        new() { Bar = true },
        new() { Bar = null },
        new() { Bar = false }
    ];

    private readonly SchemaCache _cache;

    public QueryableSortVisitorBooleanTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Create_Boolean_OrderBy()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooSortType>(s_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(order: { bar: ASC}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(order: { bar: DESC}){ bar}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "ASC")
            .Add(res2, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_Boolean_OrderBy_List()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooSortType>(s_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(order: [{ bar: ASC}]){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(order: [{ bar: DESC}]){ bar}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "ASC")
            .Add(res2, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_Boolean_OrderBy_Nullable()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableSortType>(
            s_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(order: { bar: ASC}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(order: { bar: DESC}){ bar}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "ASC")
            .Add(res2, "DESC")
            .MatchAsync();
    }

    public class Foo
    {
        public int Id { get; set; }

        public bool Bar { get; set; }
    }

    public class FooNullable
    {
        public int Id { get; set; }

        public bool? Bar { get; set; }
    }

    public class FooSortType : SortInputType<Foo>;

    public class FooNullableSortType : SortInputType<FooNullable>;
}
