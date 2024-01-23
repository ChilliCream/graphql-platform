using CookieCrumble;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;

namespace HotChocolate.Data;

[Collection(SchemaCacheCollectionFixture.DefinitionName)]
public class QueryableSortVisitorComparableTests
{
    private static readonly Foo[] _fooEntities =
    [
        new() { BarShort = 12, }, new() { BarShort = 14, }, new() { BarShort = 13, },
    ];

    private static readonly FooNullable[] _fooNullableEntities =
    [
        new() { BarShort = 12, },
        new() { BarShort = null, },
        new() { BarShort = 14, },
        new() { BarShort = 13, },
    ];

    private readonly SchemaCache _cache;

    public QueryableSortVisitorComparableTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Create_Short_OrderBy()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooSortType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(order: { barShort: ASC}){ barShort}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(order: { barShort: DESC}){ barShort}}")
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
    }

    [Fact]
    public async Task Create_Short_OrderBy_Nullable()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableSortType>(
            _fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(order: { barShort: ASC}){ barShort}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(order: { barShort: DESC}){ barShort}}")
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
    }

    public class Foo
    {
        public int Id { get; set; }

        public short BarShort { get; set; }

        public int BarInt { get; set; }

        public long BarLong { get; set; }

        public float BarFloat { get; set; }

        public double BarDouble { get; set; }

        public decimal BarDecimal { get; set; }
    }

    public class FooNullable
    {
        public int Id { get; set; }
        public short? BarShort { get; set; }
    }

    public class FooSortType : SortInputType<Foo>
    {
    }

    public class FooNullableSortType : SortInputType<FooNullable>
    {
    }
}
