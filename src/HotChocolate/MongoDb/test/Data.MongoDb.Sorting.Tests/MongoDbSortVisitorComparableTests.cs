using CookieCrumble;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;

namespace HotChocolate.Data.MongoDb.Sorting;

public class MongoDbSortVisitorComparableTests
    : SchemaCache,
      IClassFixture<MongoResource>
{
    private static readonly Foo[] _fooEntities =
    [
        new() { BarShort = 12, },
        new() { BarShort = 14, },
        new() { BarShort = 13, },
    ];

    private static readonly FooNullable[] _fooNullableEntities =
    [
        new() { BarShort = 12, },
        new() { BarShort = null, },
        new() { BarShort = 14, },
        new() { BarShort = 13, },
    ];

    public MongoDbSortVisitorComparableTests(MongoResource resource)
    {
        Init(resource);
    }

    [Fact]
    public async Task Create_Short_OrderBy()
    {
        // arrange
        var tester = CreateSchema<Foo, FooSortType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(order: { barShort: ASC}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(order: { barShort: DESC}){ barShort}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "ASC"), res2, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_Short_OrderBy_Nullable()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableSortType>(
            _fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(order: { barShort: ASC}){ barShort}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(order: { barShort: DESC}){ barShort}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "ASC"), res2, "DESC")
            .MatchAsync();
    }

    public class Foo
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public short BarShort { get; set; }

        public int BarInt { get; set; }

        public long BarLong { get; set; }

        public float BarFloat { get; set; }

        public double BarDouble { get; set; }

        public decimal BarDecimal { get; set; }
    }

    public class FooNullable
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();
        public short? BarShort { get; set; }
    }

    public class FooSortType : SortInputType<Foo>
    {
    }

    public class FooNullableSortType : SortInputType<FooNullable>
    {
    }
}
