using CookieCrumble;
using HotChocolate.Execution;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableFilterVisitorObjectTests : IClassFixture<SchemaCache>
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
                    new()
                    {
                        Foo = new Foo
                        {
                            BarShort = 12,
                            BarString = "a",
                        },
                    },
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
                    new()
                    {
                        Foo = new Foo
                        {
                            BarShort = 14,
                            BarString = "d",
                        },
                    },
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
                    new()
                    {
                        Foo = new FooNullable
                        {
                            BarShort = 12,
                        },
                    },
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
                    new()
                    {
                        Foo = new FooNullable
                        {
                            BarShort = null,
                        },
                    },
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
                    new()
                    {
                        Foo = new FooNullable
                        {
                            BarShort = 14,
                        },
                    },
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
        new()
        {
            Foo = null,
        },
    ];

    private readonly SchemaCache _cache;

    public QueryableFilterVisitorObjectTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Create_ObjectShortEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarFilterInput>(_barEntities);

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
        await Snapshot
            .Create()
            .Add(res1, "12")
            .Add(res2, "13")
            .Add(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectShortIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarFilterInput>(_barEntities);

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
        await Snapshot
            .Create()
            .Add(res1, "12and13")
            .Add(res2, "13and14")
            .Add(res3, "nullAnd14")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableShortEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<BarNullable, BarNullableFilterInput>(_barNullableEntities);

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
        await Snapshot
            .Create()
            .Add(res1, "12")
            .Add(res2, "13")
            .Add(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableShortIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<BarNullable, BarNullableFilterInput>(_barNullableEntities);

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

        // assert
        await Snapshot
            .Create()
            .Add(res1, "12and13")
            .Add(res2, "13and14")
            .Add(res3, "13andNull")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectBooleanEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarFilterInput>(_barEntities);

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

        // assert
        await Snapshot
            .Create()
            .Add(res1, "true")
            .Add(res2, "false")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableBooleanEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<BarNullable, BarNullableFilterInput>(_barNullableEntities);

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

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
            .SetDocument(
                "{ root(where: { foo: { barBool: { eq: null}}}) " +
                "{ foo{ barBool}}}")
            .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "true")
            .Add(res2, "false")
            .Add(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectEnumEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarFilterInput>(_barEntities);

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

        // assert
        await Snapshot
            .Create()
            .Add(res1, "BAR")
            .Add(res2, "FOO")
            .Add(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectEnumIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarFilterInput>(_barEntities);

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

        // assert
        await Snapshot
            .Create()
            .Add(res1, "BarAndFoo")
            .Add(res2, "FOO")
            .Add(res3, "nullAndFoo")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableEnumEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<BarNullable, BarNullableFilterInput>(_barNullableEntities);

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

        // assert
        await Snapshot
            .Create()
            .Add(res1, "BAR")
            .Add(res2, "FOO")
            .Add(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableEnumIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<BarNullable, BarNullableFilterInput>(_barNullableEntities);

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

        // assert
        await Snapshot
            .Create()
            .Add(res1, "BarAndFoo")
            .Add(res2, "FOO")
            .Add(res3, "nullAndFoo")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectStringEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarFilterInput>(_barEntities);

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

        // assert
        await Snapshot
            .Create()
            .Add(res1, "testatest")
            .Add(res2, "testbtest")
            .Add(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectStringIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarFilterInput>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                   "{ root(where: { foo: { barString: { in: [ \"testatest\"  \"testbtest\" ]}}}) " +
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

        // assert
        await Snapshot
            .Create()
            .Add(res1, "testatestAndtestb")
            .Add(res2, "testbtestAndNull")
            .Add(res3, "testatest")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ArrayObjectNestedArraySomeStringEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarFilterInput>(_barEntities);

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

        // assert
        await Snapshot
            .Create()
            .Add(res1, "a")
            .Add(res2, "d")
            .Add(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ArrayObjectNestedArrayAnyStringEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarFilterInput>(_barEntities);

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

        // assert
        await Snapshot
            .Create()
            .Add(res1, "false")
            .Add(res2, "true")
            .Add(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNull()
    {
        // arrange
        var tester = _cache.CreateSchema<BarNullable, BarNullableFilterInput>(_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { neq: BAR}}}) " +
                    "{ foo{ barEnum}}}")
                .Build());
        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { foo: null}) { foo{ barEnum}}}")
                .Build());
        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument( "{ root { foo{ barEnum}}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "selected")
            .Add(res2, "null")
            .Add(res3, "all")
            .MatchAsync();
    }

    public class Foo
    {
        public int Id { get; set; }

        public short BarShort { get; set; }

        public string BarString { get; set; } = "";

        public BarEnum BarEnum { get; set; }

        public bool BarBool { get; set; }

        public List<Bar>? ObjectArray { get; set; } = [];
    }

    public class FooNullable
    {
        public int Id { get; set; }

        public short? BarShort { get; set; }

        public string? BarString { get; set; }

        public BarEnum? BarEnum { get; set; }

        public bool? BarBool { get; set; }

        public List<BarNullable>? ObjectArray { get; set; }
    }

    public class Bar
    {
        public int Id { get; set; }

        public Foo Foo { get; set; } = null!;

    }

    public class BarNullable
    {
        public int Id { get; set; }

        public FooNullable? Foo { get; set; }

    }

    public class BarFilterInput : FilterInputType<Bar>
    {
    }

    public class BarNullableFilterInput : FilterInputType<BarNullable>
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
