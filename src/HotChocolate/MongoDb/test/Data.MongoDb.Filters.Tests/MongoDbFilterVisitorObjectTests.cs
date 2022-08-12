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
    {
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
                    new() { Foo = new Foo { BarShort = 12, BarString = "a" } }
                }
            }
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
                    new() { Foo = new Foo { BarShort = 14, BarString = "d" } }
                }
            }
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
            }
        }
    };

    private static readonly BarNullable[] _barNullableEntities =
    {
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
                    new() { Foo = new FooNullable { BarShort = 12 } }
                }
            }
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
                    new() { Foo = new FooNullable { BarShort = null } }
                }
            }
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
                    new() { Foo = new FooNullable { BarShort = 14, } }
                }
            }
        },
        new()
        {
            Foo = new FooNullable
            {
                BarShort = 13,
                BarBool = false,
                BarEnum = BarEnum.FOO,
                BarString = "testdtest",
                ObjectArray = null
            }
        }
    };

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
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { eq: 12}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { eq: 13}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { eq: null}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddSqlFrom(res1, "12")
            .AddSqlFrom(res2, "13")
            .AddSqlFrom(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectShortIn_Expression()
    {
        // arrange
        var tester = CreateSchema<Bar, BarFilterType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { in: [ 12, 13 ]}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { in: [ 13, 14 ]}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { in: [ null, 14 ]}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddSqlFrom(res1, "12and13")
            .AddSqlFrom(res2, "13and14")
            .AddSqlFrom(res3, "nullAnd14")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableShortEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<BarNullable, BarNullableFilterType>(_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { eq: 12}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { eq: 13}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { eq: null}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

        // arrange
        await Snapshot
            .Create()
            .AddSqlFrom(res1, "12")
            .AddSqlFrom(res2, "13")
            .AddSqlFrom(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableShortIn_Expression()
    {
        // arrange
        var tester = CreateSchema<BarNullable, BarNullableFilterType>(_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { in: [ 12, 13 ]}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { in: [ 13, 14 ]}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { in: [ 13, null ]}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

        // arrange
        await Snapshot
            .Create()
            .AddSqlFrom(res1, "12and13")
            .AddSqlFrom(res2, "13and14")
            .AddSqlFrom(res3, "13andNull")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectBooleanEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Bar, BarFilterType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barBool: { eq: true}}}) " +
                    "{ foo{ barBool}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barBool: { eq: false}}}) " +
                    "{ foo{ barBool}}}")
                .Create());

        // arrange
        await Snapshot
            .Create()
            .AddSqlFrom(res1, "true")
            .AddSqlFrom(res2, "false")
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
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barBool: { eq: true}}}) " +
                    "{ foo{ barBool}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barBool: { eq: false}}}) " +
                    "{ foo{ barBool}}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barBool: { eq: null}}}) " +
                    "{ foo{ barBool}}}")
                .Create());

        // arrange
        await Snapshot
            .Create()
            .AddSqlFrom(res1, "true")
            .AddSqlFrom(res2, "false")
            .AddSqlFrom(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectEnumEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Bar, BarFilterType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { eq: BAR}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { eq: FOO}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { eq: null}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

        // arrange
        await Snapshot
            .Create()
            .AddSqlFrom(res1, "BAR")
            .AddSqlFrom(res2, "FOO")
            .AddSqlFrom(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectEnumIn_Expression()
    {
        // arrange
        var tester = CreateSchema<Bar, BarFilterType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { in: [ BAR FOO ]}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { in: [ FOO ]}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { in: [ null FOO ]}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

        // arrange
        await Snapshot
            .Create()
            .AddSqlFrom(res1, "BarAndFoo")
            .AddSqlFrom(res2, "FOO")
            .AddSqlFrom(res3, "nullAndFoo")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableEnumEqual_Expression()
    {
        // assert
        var tester = CreateSchema<BarNullable, BarNullableFilterType>(_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { eq: BAR}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { eq: FOO}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { eq: null}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

        // arrange
        await Snapshot
            .Create()
            .AddSqlFrom(res1, "BAR")
            .AddSqlFrom(res2, "FOO")
            .AddSqlFrom(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableEnumIn_Expression()
    {
        // arrange
        var tester = CreateSchema<BarNullable, BarNullableFilterType>(_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { in: [ BAR FOO ]}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { in: [ FOO ]}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { in: [ null FOO ]}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

        // arrange
        await Snapshot
            .Create()
            .AddSqlFrom(res1, "BarAndFoo")
            .AddSqlFrom(res2, "FOO")
            .AddSqlFrom(res3, "nullAndFoo")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectStringEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Bar, BarFilterType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barString: { eq: \"testatest\"}}}) " +
                    "{ foo{ barString}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barString: { eq: \"testbtest\"}}}) " +
                    "{ foo{ barString}}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barString: { eq: null}}}){ foo{ barString}}}")
                .Create());

        // arrange
        await Snapshot
            .Create()
            .AddSqlFrom(res1, "testatest")
            .AddSqlFrom(res2, "testbtest")
            .AddSqlFrom(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectStringIn_Expression()
    {
        // arrange
        var tester = CreateSchema<Bar, BarFilterType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barString: { in: " +
                    "[ \"testatest\"  \"testbtest\" ]}}}) " +
                    "{ foo{ barString}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barString: { in: [\"testbtest\" null]}}}) " +
                    "{ foo{ barString}}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barString: { in: [ \"testatest\" ]}}}) " +
                    "{ foo{ barString}}}")
                .Create());

        // arrange
        await Snapshot
            .Create()
            .AddSqlFrom(res1, "testatestAndtestb")
            .AddSqlFrom(res2, "testbtestAndNull")
            .AddSqlFrom(res3, "testatest")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ArrayObjectNestedArraySomeStringEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Bar, BarFilterType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo:{ objectArray: { " +
                    "some: { foo: { barString: { eq: \"a\"}}}}}}) " +
                    "{ foo { objectArray { foo { barString}}}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo:{ objectArray: { " +
                    "some: { foo: { barString: { eq: \"d\"}}}}}}) " +
                    "{ foo { objectArray { foo { barString}}}}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo:{ objectArray: { " +
                    "some: { foo: { barString: { eq: null}}}}}}) " +
                    "{ foo { objectArray { foo {barString}}}}}")
                .Create());

        // arrange
        await Snapshot
            .Create()
            .AddSqlFrom(res1, "a")
            .AddSqlFrom(res2, "d")
            .AddSqlFrom(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ArrayObjectNestedArrayAnyStringEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Bar, BarFilterType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { objectArray: { any: false}}}) " +
                    "{ foo { objectArray  { foo { barString }}}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { objectArray: { any: true}}}) " +
                    "{ foo { objectArray  { foo { barString }}}}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { objectArray: { any: null}}}) " +
                    "{ foo { objectArray  { foo { barString }}}}}")
                .Create());

        // arrange
        await Snapshot
            .Create()
            .AddSqlFrom(res1, "false")
            .AddSqlFrom(res2, "true")
            .AddSqlFrom(res3, "null")
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
        QUX
    }
}
