using HotChocolate.Execution;

namespace HotChocolate.Data.Filters;

public class QueryableFilterVisitorObjectTests
{
    private static readonly Bar[] s_barEntities =
    [
        new()
        {
            Foo = new Foo
            {
                BarShort = 12,
                BarBool = true,
                BarEnum = BarEnum.BAR,
                BarString = "testatest",
                ObjectArray = [new() { Foo = new Foo { BarShort = 12, BarString = "a" } }]
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
                ObjectArray = [new() { Foo = new Foo { BarShort = 14, BarString = "d" } }]
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
                ObjectArray = null
            }
        }
    ];

    private static readonly BarNullable[] s_barNullableEntities =
    [
        new()
        {
            Foo = new FooNullable
            {
                BarShort = 12,
                BarBool = true,
                BarEnum = BarEnum.BAR,
                BarString = "testatest",
                ObjectArray = [new() { Foo = new FooNullable { BarShort = 12 } }]
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
                ObjectArray = [new() { Foo = new FooNullable { BarShort = null } }]
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
                ObjectArray = [new() { Foo = new FooNullable { BarShort = 14 } }]
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
    ];

    private static readonly Baz[] s_bazEntities =
        s_barEntities.Select(b => new Baz { Bar = b }).ToArray();

    private readonly SchemaCache _cache = new();

    [Fact]
    public async Task Create_ObjectShortEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarFilterInput>(s_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { eq: 12}}}) "
                    + "{ foo{ barShort}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { eq: 13}}}) "
                    + "{ foo{ barShort}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { eq: null}}}) "
                    + "{ foo{ barShort}}}")
                .Build());

        // assert
        await Snapshot
            .Create(
                postFix: TestEnvironment.TargetFramework == "NET10_0"
                    ? TestEnvironment.TargetFramework
                    : null)
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectShortIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarFilterInput>(s_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { in: [ 12, 13 ]}}}) "
                    + "{ foo{ barShort}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { in: [ 13, 14 ]}}}) "
                    + "{ foo{ barShort}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { in: [ null, 14 ]}}}) "
                    + "{ foo{ barShort}}}")
                .Build());

        // assert
        await Snapshot
            .Create(
                postFix: TestEnvironment.TargetFramework == "NET10_0"
                    ? TestEnvironment.TargetFramework
                    : null)
            .AddResult(res1, "12and13")
            .AddResult(res2, "13and14")
            .AddResult(res3, "nullAnd14")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableShortEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<BarNullable, BarNullableFilterInput>(s_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { eq: 12}}}) "
                    + "{ foo{ barShort}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { eq: 13}}}) "
                    + "{ foo{ barShort}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { eq: null}}}) "
                    + "{ foo{ barShort}}}")
                .Build());

        // assert
        await Snapshot
            .Create(
                postFix: TestEnvironment.TargetFramework == "NET10_0"
                    ? TestEnvironment.TargetFramework
                    : null)
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableShortIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<BarNullable, BarNullableFilterInput>(s_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { in: [ 12, 13 ]}}}) "
                    + "{ foo{ barShort}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { in: [ 13, 14 ]}}}) "
                    + "{ foo{ barShort}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barShort: { in: [ 13, null ]}}}) "
                    + "{ foo{ barShort}}}")
                .Build());

        // assert
        await Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddResult(res1, "12and13")
            .AddResult(res2, "13and14")
            .AddResult(res3, "13andNull")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectBooleanEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarFilterInput>(s_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barBool: { eq: true}}}) "
                    + "{ foo{ barBool}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barBool: { eq: false}}}) "
                    + "{ foo{ barBool}}}")
                .Build());

        // assert
        await Snapshot
            .Create(
                postFix: TestEnvironment.TargetFramework == "NET10_0"
                    ? TestEnvironment.TargetFramework
                    : null)
            .AddResult(res1, "true")
            .AddResult(res2, "false")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableBooleanEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<BarNullable, BarNullableFilterInput>(s_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barBool: { eq: true}}}) "
                    + "{ foo{ barBool}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barBool: { eq: false}}}) "
                    + "{ foo{ barBool}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barBool: { eq: null}}}) "
                    + "{ foo{ barBool}}}")
                .Build());

        // assert
        await Snapshot
            .Create(
                postFix: TestEnvironment.TargetFramework == "NET10_0"
                    ? TestEnvironment.TargetFramework
                    : null)
            .AddResult(res1, "true")
            .AddResult(res2, "false")
            .AddResult(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectEnumEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarFilterInput>(s_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { eq: BAR}}}) "
                    + "{ foo{ barEnum}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { eq: FOO}}}) "
                    + "{ foo{ barEnum}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { eq: null}}}) "
                    + "{ foo{ barEnum}}}")
                .Build());

        // assert
        await Snapshot
            .Create(
                postFix: TestEnvironment.TargetFramework == "NET10_0"
                    ? TestEnvironment.TargetFramework
                    : null)
            .AddResult(res1, "BAR")
            .AddResult(res2, "FOO")
            .AddResult(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectEnumIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarFilterInput>(s_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { in: [ BAR FOO ]}}}) "
                    + "{ foo{ barEnum}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { in: [ FOO ]}}}) "
                    + "{ foo{ barEnum}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { in: [ null FOO ]}}}) "
                    + "{ foo{ barEnum}}}")
                .Build());

        // assert
        await Snapshot
            .Create(
                postFix: TestEnvironment.TargetFramework == "NET10_0"
                    ? TestEnvironment.TargetFramework
                    : null)
            .AddResult(res1, "BarAndFoo")
            .AddResult(res2, "FOO")
            .AddResult(res3, "nullAndFoo")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableEnumEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<BarNullable, BarNullableFilterInput>(
            s_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { eq: BAR}}}) "
                    + "{ foo{ barEnum}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { eq: FOO}}}) "
                    + "{ foo{ barEnum}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { eq: null}}}) "
                    + "{ foo{ barEnum}}}")
                .Build());

        // assert
        await Snapshot
            .Create(
                postFix: TestEnvironment.TargetFramework == "NET10_0"
                    ? TestEnvironment.TargetFramework
                    : null)
            .AddResult(res1, "BAR")
            .AddResult(res2, "FOO")
            .AddResult(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableEnumIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<BarNullable, BarNullableFilterInput>(s_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { in: [ BAR FOO ]}}}) "
                    + "{ foo{ barEnum}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { in: [ FOO ]}}}) "
                    + "{ foo{ barEnum}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barEnum: { in: [ null FOO ]}}}) "
                    + "{ foo{ barEnum}}}")
                .Build());

        // assert
        await Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddResult(res1, "BarAndFoo")
            .AddResult(res2, "FOO")
            .AddResult(res3, "nullAndFoo")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectStringEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarFilterInput>(s_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barString: { eq: \"testatest\"}}}) "
                    + "{ foo{ barString}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barString: { eq: \"testbtest\"}}}) "
                    + "{ foo{ barString}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barString: { eq: null}}}){ foo{ barString}}}")
                .Build());

        // assert
        await Snapshot
            .Create(
                postFix: TestEnvironment.TargetFramework == "NET10_0"
                    ? TestEnvironment.TargetFramework
                    : null)
            .AddResult(res1, "testatest")
            .AddResult(res2, "testbtest")
            .AddResult(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectStringIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarFilterInput>(s_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barString: { in: "
                    + "[ \"testatest\"  \"testbtest\" ]}}}) "
                    + "{ foo{ barString}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barString: { in: [\"testbtest\" null]}}}) "
                    + "{ foo{ barString}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { barString: { in: [ \"testatest\" ]}}}) "
                    + "{ foo{ barString}}}")
                .Build());

        // assert
        await Snapshot
            .Create(
                postFix: TestEnvironment.TargetFramework == "NET10_0"
                    ? TestEnvironment.TargetFramework
                    : null)
            .AddResult(res1, "testatestAndtestb")
            .AddResult(res2, "testbtestAndNull")
            .AddResult(res3, "testatest")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ArrayObjectNestedArraySomeStringEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarFilterInput>(s_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo:{ objectArray: { "
                    + "some: { foo: { barString: { eq: \"a\"}}}}}}) "
                    + "{ foo { objectArray { foo { barString}}}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo:{ objectArray: { "
                    + "some: { foo: { barString: { eq: \"d\"}}}}}}) "
                    + "{ foo { objectArray { foo { barString}}}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo:{ objectArray: { "
                    + "some: { foo: { barString: { eq: null}}}}}}) "
                    + "{ foo { objectArray { foo {barString}}}}}")
                .Build());

        // assert
        await Snapshot
            .Create(
                postFix: TestEnvironment.TargetFramework == "NET10_0"
                    ? TestEnvironment.TargetFramework
                    : null)
            .AddResult(res1, "a")
            .AddResult(res2, "b")
            .AddResult(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ArrayObjectNestedArrayAnyStringEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarFilterInput>(s_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { objectArray: { any: false}}}) "
                    + "{ foo { objectArray  { foo { barString }}}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { objectArray: { any: true}}}) "
                    + "{ foo { objectArray  { foo { barString }}}}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { foo: { objectArray: { any: null}}}) "
                    + "{ foo { objectArray  { foo { barString }}}}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "false")
            .AddResult(res2, "true")
            .AddResult(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectStringEqual_Flattened()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarFilterInput>(s_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { fooBarString: { eq: \"testatest\" } }) "
                    + "{ foo { barString } } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { fooBarString: { eq: \"testbtest\" } }) "
                    + "{ foo { barString } } }")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { fooBarString: { eq: null } }) { foo { barString } } }")
                .Build());

        // assert
        Assert.Null(Assert.IsType<OperationResult>(res1).Errors);
        Assert.Null(Assert.IsType<OperationResult>(res2).Errors);
        Assert.Null(Assert.IsType<OperationResult>(res3).Errors);
        await Snapshot
            .Create(
                postFix: TestEnvironment.TargetFramework == "NET10_0"
                    ? TestEnvironment.TargetFramework
                    : null)
            .AddResult(res1, "testatest")
            .AddResult(res2, "testbtest")
            .AddResult(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectStringEquals_Related_Flattened()
    {
        // arrange
        var tester = _cache.CreateSchema<Baz, BazFilterInput>(s_bazEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { bar: { fooBarString: { eq: \"testatest\" } } }) "
                    + "{ bar { foo { barString } } } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { bar: { fooBarString: { eq: \"testbtest\" } } }) "
                    + "{ bar { foo { barString } } } }")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { bar: { fooBarString: { eq: null } } }) "
                    + "{ bar { foo { barString } } } }")
                .Build());

        // assert
        Assert.Null(Assert.IsType<OperationResult>(res1).Errors);
        Assert.Null(Assert.IsType<OperationResult>(res2).Errors);
        Assert.Null(Assert.IsType<OperationResult>(res3).Errors);
        await Snapshot
            .Create(
                postFix: TestEnvironment.TargetFramework == "NET10_0"
                    ? TestEnvironment.TargetFramework
                    : null)
            .AddResult(res1, "testatest")
            .AddResult(res2, "testbtest")
            .AddResult(res3, "null")
            .MatchAsync();
    }

    public class Foo
    {
        public int Id { get; set; }

        public short BarShort { get; set; }

        public string BarString { get; set; } = string.Empty;

        public BarEnum BarEnum { get; set; }

        public bool BarBool { get; set; }

        public List<Bar>? ObjectArray { get; set; }
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

    public class Baz
    {
        public int Id { get; set; }
        public Bar Bar { get; set; } = null!;
    }

    public class BarFilterInput : FilterInputType<Bar>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Bar> descriptor)
        {
            descriptor.Field(t => t.Foo.BarString)
                .Name("fooBarString");
        }
    }

    public class BarNullableFilterInput : FilterInputType<BarNullable>;

    public class BazFilterInput : FilterInputType<Baz>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Baz> descriptor)
        {
            descriptor.Field(b => b.Bar)
                .Type<BarFilterInput>();
        }
    }

    public enum BarEnum
    {
        FOO,
        BAR,
        BAZ,
        QUX
    }
}
