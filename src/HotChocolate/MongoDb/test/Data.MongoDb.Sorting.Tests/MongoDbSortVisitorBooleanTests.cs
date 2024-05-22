using CookieCrumble;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;

namespace HotChocolate.Data.MongoDb.Sorting;

public class MongoDbSortVisitorBooleanTests
    : SchemaCache,
      IClassFixture<MongoResource>
{
    private static readonly Foo[] _fooEntities =
    [
        new() { Bar = true, },
        new() { Bar = false, },
    ];

    private static readonly FooNullable[] _fooNullableEntities =
    [
        new() { Bar = true, },
        new() { Bar = null, },
        new() { Bar = false, },
    ];

    public MongoDbSortVisitorBooleanTests(MongoResource resource)
    {
        Init(resource);
    }

    [Fact]
    public async Task Create_Boolean_OrderBy()
    {
        // arrange
        var tester = CreateSchema<Foo, FooSortType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(order: { bar: ASC}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(order: { bar: DESC}){ bar}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "ASC"), res2, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_Boolean_OrderBy_Nullable()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableSortType>(
            _fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(order: { bar: ASC}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(order: { bar: DESC}){ bar}}")
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

        public bool Bar { get; set; }
    }

    public class FooNullable
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public bool? Bar { get; set; }
    }

    public class FooSortType : SortInputType<Foo>
    {
    }

    public class FooNullableSortType : SortInputType<FooNullable>
    {
    }
}
