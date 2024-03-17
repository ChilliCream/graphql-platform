using CookieCrumble;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;

namespace HotChocolate.Data.MongoDb.Filters;

public class MongoDbFilterVisitorEnumTests
    : SchemaCache
    , IClassFixture<MongoResource>
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

    public MongoDbFilterVisitorEnumTests(MongoResource resource)
    {
        Init(resource);
    }

    [Fact]
    public async Task Create_EnumEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { eq: BAR } }) { barEnum } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { eq: FOO } }) { barEnum } }")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { eq: null } }) { barEnum } }")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "BAR"), res2, "FOO"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_EnumNotEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { neq: BAR } }) { barEnum } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { neq: FOO } }) { barEnum } }")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { neq: null } }){ barEnum } }")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "BAR"), res2, "FOO"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_EnumIn_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { in: [ BAR FOO ]}}){ barEnum}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { in: [ FOO ]}}){ barEnum}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { in: [ null FOO ]}}){ barEnum}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "BarAndFoo"), res2, "FOO"), res3, "nullAndFoo")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_EnumNotIn_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { nin: [ BAR FOO ] } }) { barEnum } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { nin: [ FOO ] } }) { barEnum } }")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { nin: [ null FOO ] } }) { barEnum } }")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "BarAndFoo"), res2, "FOO"), res3, "nullAndFoo")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableEnumEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { eq: BAR } }) { barEnum } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { eq: FOO } }) { barEnum } }")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { eq: null } }){ barEnum } }")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "BAR"), res2, "FOO"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableEnumNotEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { neq: BAR } }) { barEnum } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { neq: FOO } }) { barEnum } }")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { neq: null } }) { barEnum } }")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "BAR"), res2, "FOO"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableEnumIn_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { in: [ BAR FOO ] } }) { barEnum } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { in: [ FOO ] } }) { barEnum } }")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { in: [ null FOO ] } }) { barEnum } }")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "BarAndFoo"), res2, "FOO"), res3, "nullAndFoo")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableEnumNotIn_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { nin: [ BAR FOO ] } }){ barEnum } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { nin: [ FOO ] } }) { barEnum } }")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { nin: [ null FOO ] } }) { barEnum } }")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "BarAndFoo"), res2, "FOO"), res3, "nullAndFoo")
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

    public class FooFilterType : FilterInputType<Foo>
    {
    }

    public class FooNullableFilterType : FilterInputType<FooNullable>
    {
    }
}
