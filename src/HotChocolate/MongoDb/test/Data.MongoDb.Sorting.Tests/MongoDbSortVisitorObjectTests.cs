using CookieCrumble;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;

namespace HotChocolate.Data.MongoDb.Sorting;

public class MongoDbSortVisitorObjectTests
    : SchemaCache,
      IClassFixture<MongoResource>
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
                ObjectArray =
                [
                    new() { Foo = new Foo { BarShort = 12, BarString = "a", }, },
                ],
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
                ObjectArray =
                [
                    new() { Foo = new Foo { BarShort = 14, BarString = "d", }, },
                ],
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
                ObjectArray = null!,
            },
        },
    ];

    private static readonly BarNullable?[] _barNullableEntities =
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
        new() { Foo = null, },
    ];

    public MongoDbSortVisitorObjectTests(MongoResource resource)
    {
        Init(resource);
    }

    [Fact]
    public async Task Create_ObjectShort_OrderBy()
    {
        // arrange
        var tester = CreateSchema<Bar, BarSortType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(order: { foo: { barShort: ASC}}) " +
                    "{ foo{ barShort}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(order: { foo: { barShort: DESC}}) " +
                    "{ foo{ barShort}}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "ASC"), res2, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableShort_OrderBy()
    {
        // arrange
        var tester =
            CreateSchema<BarNullable, BarNullableSortType>(_barNullableEntities!);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(order: { foo: { barShort: ASC}}) " +
                    "{ foo{ barShort}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(order: { foo: { barShort: DESC}}) " +
                    "{ foo{ barShort}}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "ASC"), res2, "13")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectEnum_OrderBy()
    {
        // arrange
        var tester = CreateSchema<Bar, BarSortType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(order: { foo: { barEnum: ASC}}) " +
                    "{ foo{ barEnum}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(order: { foo: { barEnum: DESC}}) " +
                    "{ foo{ barEnum}}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "ASC"), res2, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableEnum_OrderBy()
    {
        // arrange
        var tester =
            CreateSchema<BarNullable, BarNullableSortType>(_barNullableEntities!);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(order: { foo: { barEnum: ASC}}) " +
                    "{ foo{ barEnum}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(order: { foo: { barEnum: DESC}}) " +
                    "{ foo{ barEnum}}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "ASC"), res2, "13")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectString_OrderBy()
    {
        // arrange
        var tester = CreateSchema<Bar, BarSortType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(order: { foo: { barString: ASC}}) " +
                    "{ foo{ barString}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(order: { foo: { barString: DESC}}) " +
                    "{ foo{ barString}}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "ASC"), res2, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableString_OrderBy()
    {
        // arrange
        var tester =
            CreateSchema<BarNullable, BarNullableSortType>(_barNullableEntities!);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(order: { foo: { barString: ASC}}) " +
                    "{ foo{ barString}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(order: { foo: { barString: DESC}}) " +
                    "{ foo{ barString}}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "ASC"), res2, "13")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectBool_OrderBy()
    {
        // arrange
        var tester = CreateSchema<Bar, BarSortType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(order: { foo: { barBool: ASC}}) " +
                    "{ foo{ barBool}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(order: { foo: { barBool: DESC}}) " +
                    "{ foo{ barBool}}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "ASC"), res2, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableBool_OrderBy()
    {
        // arrange
        var tester =
            CreateSchema<BarNullable, BarNullableSortType>(_barNullableEntities!);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(order: { foo: { barBool: ASC}}) " +
                    "{ foo{ barBool}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(order: { foo: { barBool: DESC}}) " +
                    "{ foo{ barBool}}}")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "ASC"), res2, "13")
            .MatchAsync();
    }

    public class Foo
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public short BarShort { get; set; }

        public string BarString { get; set; } = "";

        public BarEnum BarEnum { get; set; }

        public bool BarBool { get; set; }

        //Not supported in SQL
        //public string[] ScalarArray { get; set; }

        public List<Bar> ObjectArray { get; set; } = [];
    }

    public class FooNullable
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public short? BarShort { get; set; }

        public string? BarString { get; set; }

        public BarEnum? BarEnum { get; set; }

        public bool? BarBool { get; set; }

        //Not supported in SQL
        //public string?[] ScalarArray { get; set; }

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

    public class BarSortType : SortInputType<Bar>
    {
    }

    public class BarNullableSortType : SortInputType<BarNullable>
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
