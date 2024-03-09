using CookieCrumble;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;

namespace HotChocolate.Data.MongoDb.Filters;

public class MongoDbFilterVisitorObjectTests
    : SchemaCache
    , IClassFixture<MongoResource>
{
    private static readonly Bar[] _barEntities =
    [
        new()
        {
            Foo = new Foo
            {
                BarShort = 12,
                BarBool = true,
                BarEnum = BarEnum.BAR,
                BarString = "testatest",
                ObjectArray = new List<Bar>
                {
                    new() { Foo = new Foo { BarShort = 12, BarString = "a", }, },
                },
            },
        },
        new()
        {
            Foo = new Foo
            {
                BarShort = 14,
                BarBool = true,
                BarEnum = BarEnum.BAZ,
                BarString = "testbtest",
                ObjectArray = new List<Bar>
                {
                    new() { Foo = new Foo { BarShort = 14, BarString = "d", }, },
                },
            },
        },
        new()
        {
            Foo = new Foo
            {
                BarShort = 13,
                BarBool = false,
                BarEnum = BarEnum.FOO,
                BarString = "testctest",
                //ScalarArray = null,
                ObjectArray = null,
            },
        },
    ];

    private static readonly BarNullable[] _barNullableEntities =
    [
        new()
        {
            Foo = new FooNullable
            {
                BarShort = 12,
                BarBool = true,
                BarEnum = BarEnum.BAR,
                BarString = "testatest",
                ObjectArray = new List<BarNullable>
                {
                    new() { Foo = new FooNullable { BarShort = 12, }, },
                },
            },
        },
        new()
        {
            Foo = new FooNullable
            {
                BarShort = null,
                BarBool = null,
                BarEnum = BarEnum.BAZ,
                BarString = "testbtest",
                ObjectArray = new List<BarNullable>
                {
                    new() { Foo = new FooNullable { BarShort = null, }, },
                },
            },
        },
        new()
        {
            Foo = new FooNullable
            {
                BarShort = 14,
                BarBool = false,
                BarEnum = BarEnum.QUX,
                BarString = "testctest",
                ObjectArray = new List<BarNullable>
                {
                    new() { Foo = new FooNullable { BarShort = 14, }, },
                },
            },
        },
        new()
        {
            Foo = new FooNullable
            {
                BarShort = 13,
                BarBool = false,
                BarEnum = BarEnum.FOO,
                BarString = "testdtest",
                ObjectArray = null,
            },
        },
    ];

    public MongoDbFilterVisitorObjectTests(MongoResource resource)
    {
        Init(resource);
    }

    [Fact]
    public async Task Create_ObjectShortEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Bar, BarFilterType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { eq: 12}}}) " +
                    "{ foo{ barShort}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { eq: 13}}}) " +
                    "{ foo{ barShort}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { eq: null}}}) " +
                    "{ foo{ barShort}}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "12"), res2, "13"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectShortIn_Expression()
    {
        // arrange
        var tester = CreateSchema<Bar, BarFilterType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { in: [ 12, 13 ]}}}) " +
                    "{ foo{ barShort}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { in: [ 13, 14 ]}}}) " +
                    "{ foo{ barShort}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { in: [ null, 14 ]}}}) " +
                    "{ foo{ barShort}}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "12and13"), res2, "13and14"), res3, "nullAnd14")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableShortEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<BarNullable, BarNullableFilterType>(_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { eq: 12}}}) " +
                    "{ foo{ barShort}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { eq: 13}}}) " +
                    "{ foo{ barShort}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { eq: null}}}) " +
                    "{ foo{ barShort}}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "12"), res2, "13"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableShortIn_Expression()
    {
        // arrange
        var tester = CreateSchema<BarNullable, BarNullableFilterType>(_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { in: [ 12, 13 ]}}}) " +
                    "{ foo{ barShort}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { in: [ 13, 14 ]}}}) " +
                    "{ foo{ barShort}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { in: [ 13, null ]}}}) " +
                    "{ foo{ barShort}}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "12and13"), res2, "13and14"), res3, "13andNull")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectBooleanEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Bar, BarFilterType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barBool: { eq: true}}}) " +
                    "{ foo{ barBool}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barBool: { eq: false}}}) " +
                    "{ foo{ barBool}}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "true"), res2, "false")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableBooleanEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<BarNullable, BarNullableFilterType>(
            _barNullableEntities);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barBool: { eq: true}}}) " +
                    "{ foo{ barBool}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barBool: { eq: false}}}) " +
                    "{ foo{ barBool}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barBool: { eq: null}}}) " +
                    "{ foo{ barBool}}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "true"), res2, "false"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectEnumEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Bar, BarFilterType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { eq: BAR}}}) " +
                    "{ foo{ barEnum}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { eq: FOO}}}) " +
                    "{ foo{ barEnum}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { eq: null}}}) " +
                    "{ foo{ barEnum}}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "BAR"), res2, "FOO"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectEnumIn_Expression()
    {
        // arrange
        var tester = CreateSchema<Bar, BarFilterType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { in: [ BAR FOO ]}}}) " +
                    "{ foo{ barEnum}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { in: [ FOO ]}}}) " +
                    "{ foo{ barEnum}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { in: [ null FOO ]}}}) " +
                    "{ foo{ barEnum}}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "BarAndFoo"), res2, "FOO"), res3, "nullAndFoo")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableEnumEqual_Expression()
    {
        // assert
        var tester = CreateSchema<BarNullable, BarNullableFilterType>(_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { eq: BAR}}}) " +
                    "{ foo{ barEnum}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { eq: FOO}}}) " +
                    "{ foo{ barEnum}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { eq: null}}}) " +
                    "{ foo{ barEnum}}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "BAR"), res2, "FOO"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableEnumIn_Expression()
    {
        // arrange
        var tester = CreateSchema<BarNullable, BarNullableFilterType>(_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { in: [ BAR FOO ]}}}) " +
                    "{ foo{ barEnum}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { in: [ FOO ]}}}) " +
                    "{ foo{ barEnum}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { in: [ null FOO ]}}}) " +
                    "{ foo{ barEnum}}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "BarAndFoo"), res2, "FOO"), res3, "nullAndFoo")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectStringEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Bar, BarFilterType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barString: { eq: \"testatest\"}}}) " +
                    "{ foo{ barString}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barString: { eq: \"testbtest\"}}}) " +
                    "{ foo{ barString}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barString: { eq: null}}}){ foo{ barString}}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "testatest"), res2, "testbtest"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectStringIn_Expression()
    {
        // arrange
        var tester = CreateSchema<Bar, BarFilterType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barString: { in: " +
                    "[ \"testatest\"  \"testbtest\" ]}}}) " +
                    "{ foo{ barString}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barString: { in: [\"testbtest\" null]}}}) " +
                    "{ foo{ barString}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barString: { in: [ \"testatest\" ]}}}) " +
                    "{ foo{ barString}}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "testatestAndtestb"), res2, "testbtestAndNull"), res3, "testatest")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ArrayObjectNestedArraySomeStringEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Bar, BarFilterType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo:{ objectArray: { " +
                    "some: { foo: { barString: { eq: \"a\"}}}}}}) " +
                    "{ foo { objectArray { foo { barString}}}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo:{ objectArray: { " +
                    "some: { foo: { barString: { eq: \"d\"}}}}}}) " +
                    "{ foo { objectArray { foo { barString}}}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo:{ objectArray: { " +
                    "some: { foo: { barString: { eq: null}}}}}}) " +
                    "{ foo { objectArray { foo {barString}}}}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "a"), res2, "d"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ArrayObjectNestedArrayAnyStringEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Bar, BarFilterType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { objectArray: { any: false}}}) " +
                    "{ foo { objectArray  { foo { barString }}}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { objectArray: { any: true}}}) " +
                    "{ foo { objectArray  { foo { barString }}}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { objectArray: { any: null}}}) " +
                    "{ foo { objectArray  { foo { barString }}}}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "false"), res2, "true"), res3, "null")
            .MatchAsync();
    }

    public class Foo
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public short BarShort { get; set; }

        public string BarString { get; set; } = string.Empty;

        public BarEnum BarEnum { get; set; }

        public bool BarBool { get; set; }

        public List<Bar>? ObjectArray { get; set; } = null!;
    }

    public class FooNullable
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public short? BarShort { get; set; }

        public string? BarString { get; set; }

        public BarEnum? BarEnum { get; set; }

        public bool? BarBool { get; set; }

        public List<BarNullable>? ObjectArray { get; set; }
    }

    public class Bar
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Foo Foo { get; set; } = null!;
    }

    public class BarNullable
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public FooNullable? Foo { get; set; }
    }

    public class BarFilterType
        : FilterInputType<Bar>
    {
    }

    public class BarNullableFilterType
        : FilterInputType<BarNullable>
    {
    }

    public enum BarEnum
    {
        FOO,
        BAR,
        BAZ,
        QUX,
    }
}
