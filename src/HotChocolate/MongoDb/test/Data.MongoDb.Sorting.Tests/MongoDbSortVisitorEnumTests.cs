using CookieCrumble;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;

namespace HotChocolate.Data.MongoDb.Sorting;

public class MongoDbSortVisitorEnumTests
    : SchemaCache,
      IClassFixture<MongoResource>
{
    private static readonly Foo[] _fooEntities =
    [
        new() { BarEnum = FooEnum.BAR, },
        new() { BarEnum = FooEnum.BAZ, },
        new() { BarEnum = FooEnum.FOO, },
        new() { BarEnum = FooEnum.QUX, },
    ];

    private static readonly FooNullable[] _fooNullableEntities =
    [
        new() { BarEnum = FooEnum.BAR, },
        new() { BarEnum = FooEnum.BAZ, },
        new() { BarEnum = FooEnum.FOO, },
        new() { BarEnum = null, },
        new() { BarEnum = FooEnum.QUX, },
    ];

    public MongoDbSortVisitorEnumTests(MongoResource resource)
    {
        Init(resource);
    }

    [Fact]
    public async Task Create_Enum_OrderBy()
    {
        // arrange
        var tester = CreateSchema<Foo, FooSortType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(order: { barEnum: ASC}){ barEnum}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(order: { barEnum: DESC}){ barEnum}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "ASC"), res2, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_Enum_OrderBy_Nullable()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableSortType>(
            _fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(order: { barEnum: ASC}){ barEnum}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(order: { barEnum: DESC}){ barEnum}}")
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

        public FooEnum BarEnum { get; set; }
    }

    public class FooNullable
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public FooEnum? BarEnum { get; set; }
    }

    public enum FooEnum
    {
        FOO,
        BAR,
        BAZ,
        QUX,
    }

    public class FooSortType : SortInputType<Foo>
    {
    }

    public class FooNullableSortType : SortInputType<FooNullable>
    {
    }
}
