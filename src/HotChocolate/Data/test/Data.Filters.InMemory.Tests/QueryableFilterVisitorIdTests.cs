using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Types.Relay;

namespace HotChocolate.Data.Filters;

public class QueryableFilterVisitorIdTests : IClassFixture<SchemaCache>
{
    private static readonly Foo[] _fooEntities =
    [
        new() { Bar = "testatest", },
        new() { Bar = "testbtest", },
    ];

    private static readonly FooNullable[] _fooNullableEntities =
    [
        new() { Bar = "testatest", },
        new() { Bar = "testbtest", },
        new() { Bar = null, },
    ];

    private static readonly FooShort[] _fooShortEntities =
    [
        new() { BarShort = 12, },
        new() { BarShort = 14, },
        new() { BarShort = 13, },
    ];

    private static readonly FooShortNullable[] _fooShortNullableEntities =
    [
        new() { BarShort = 12, },
        new() { BarShort = null, },
        new() { BarShort = 14, },
        new() { BarShort = 13, },
    ];

    private readonly SchemaCache _cache;

    public QueryableFilterVisitorIdTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Create_StringIdEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(
            _fooEntities,
            configure: sb => sb.AddGlobalObjectIdentification(false));

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: \"Rm8KZHRlc3RhdGVzdA==\"}}){ bar}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: \"Rm8KZHRlc3RidGVzdA==\"}}){ bar}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: null}}){ bar}}")
                .Create());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "testatest")
            .Add(res2, "testbtest")
            .Add(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_StringIdNotEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(
            _fooEntities,
            configure: sb => sb.AddGlobalObjectIdentification(false));

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: \"Rm8KZHRlc3RhdGVzdA==\"}}){ bar}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: \"Rm8KZHRlc3RidGVzdA==\"}}){ bar}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: null}}){ bar}}")
                .Create());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "testatest")
            .Add(res2, "testbtest")
            .Add(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_StringIdIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(
            _fooEntities,
            configure: sb => sb.AddGlobalObjectIdentification(false));

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(@"{
                            root(where: {
                                bar: {
                                    in: [ ""Rm8KZHRlc3RhdGVzdA==""  ""Rm8KZHRlc3RidGVzdA=="" ]
                                }
                            }){
                                bar
                            }
                        }")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { bar: { in: [\"Rm8KZHRlc3RidGVzdA==\" null]}}){ bar}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { in: [ \"Rm8KZHRlc3RhdGVzdA==\" ]}}){ bar}}")
                .Create());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "testatestAndtestb")
            .Add(res2, "testbtestAndNull")
            .Add(res3, "testatest")
            .MatchAsync();

    }

    [Fact]
    public async Task Create_StringIdNotIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(
            _fooEntities,
            configure: sb => sb.AddGlobalObjectIdentification(false));

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(@"{
                            root(where: {
                                bar: {
                                    nin: [ ""Rm8KZHRlc3RhdGVzdA==""  ""Rm8KZHRlc3RidGVzdA=="" ]
                                }
                            }){
                                bar
                            }
                        }")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { bar: { nin: [\"Rm8KZHRlc3RidGVzdA==\" null]}}){ bar}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { nin: [ \"Rm8KZHRlc3RhdGVzdA==\" ]}}){ bar}}")
                .Create());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "testatestAndtestb")
            .Add(res2, "testbtestAndNull")
            .Add(res3, "testatest")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringIdEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(
            _fooNullableEntities,
            configure: sb => sb.AddGlobalObjectIdentification(false));

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: \"Rm8KZHRlc3RhdGVzdA==\"}}){ bar}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: \"Rm8KZHRlc3RidGVzdA==\"}}){ bar}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: null}}){ bar}}")
                .Create());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "testatest")
            .Add(res2, "testbtest")
            .Add(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringIdNotEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(
            _fooNullableEntities,
            configure: sb => sb.AddGlobalObjectIdentification(false));

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: \"Rm8KZHRlc3RhdGVzdA==\"}}){ bar}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: \"Rm8KZHRlc3RidGVzdA==\"}}){ bar}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: null}}){ bar}}")
                .Create());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "testatest")
            .Add(res2, "testbtest")
            .Add(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringIdIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(
            _fooNullableEntities,
            configure: sb => sb.AddGlobalObjectIdentification(false));

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(@"{
                            root(where: {
                                bar: {
                                    in: [ ""Rm8KZHRlc3RhdGVzdA==""  ""Rm8KZHRlc3RidGVzdA=="" ]
                                }
                            }){
                                bar
                            }
                        }")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { bar: { in: [\"Rm8KZHRlc3RidGVzdA==\" null]}}){ bar}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { in: [ \"Rm8KZHRlc3RhdGVzdA==\" ]}}){ bar}}")
                .Create());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "testatestAndtestb")
            .Add(res2, "testbtestAndNull")
            .Add(res3, "testatest")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringIdNotIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(
            _fooNullableEntities,
            configure: sb => sb.AddGlobalObjectIdentification(false));

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"{
                        root(where: {
                            bar: {
                                nin: [ ""Rm8KZHRlc3RhdGVzdA==""  ""Rm8KZHRlc3RidGVzdA=="" ]
                            }
                        }){
                            bar
                        }
                    }")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { nin: [\"Rm8KZHRlc3RidGVzdA==\" null]}}){ bar}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { nin: [ \"Rm8KZHRlc3RhdGVzdA==\" ]}}){ bar}}")
                .Create());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "testatestAndtestb")
            .Add(res2, "testbtestAndNull")
            .Add(res3, "testatest")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooShort, FooShortFilterInput>(
            _fooShortEntities,
            configure: sb => sb.AddGlobalObjectIdentification(false));

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { eq: \"Rm9vCnMxMg==\"}}){ barShort}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { eq: \"Rm9vCnMxMw==\"}}){ barShort}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { eq: null}}){ barShort}}")
                .Create());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "12")
            .Add(res2, "13")
            .Add(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNotEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooShort, FooShortFilterInput>(
            _fooShortEntities,
            configure: sb => sb.AddGlobalObjectIdentification(false));

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { neq: \"Rm9vCnMxMg==\"}}){ barShort}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { neq: \"Rm9vCnMxMw==\"}}){ barShort}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { neq: null}}){ barShort}}")
                .Create());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "12")
            .Add(res2, "13")
            .Add(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableEqual_Expression()
    {
        // arrange
        var tester =
            _cache.CreateSchema<FooShortNullable, FooShortNullableFilterInput>(
                _fooShortNullableEntities,
                configure: sb => sb.AddGlobalObjectIdentification(false));

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { eq: \"Rm9vCnMxMg==\"}}){ barShort}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { eq: \"Rm9vCnMxMw==\"}}){ barShort}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { eq: null}}){ barShort}}")
                .Create());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "12")
            .Add(res2, "13")
            .Add(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotEqual_Expression()
    {
        // arrange
        var tester =
            _cache.CreateSchema<FooShortNullable, FooShortNullableFilterInput>(
                _fooShortNullableEntities,
                configure: sb => sb.AddGlobalObjectIdentification(false));

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { neq: \"Rm9vCnMxMg==\"}}){ barShort}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { neq: \"Rm9vCnMxMw==\"}}){ barShort}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { neq: null}}){ barShort}}")
                .Create());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "12")
            .Add(res2, "13")
            .Add(res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooShort, FooShortFilterInput>(
            _fooShortEntities,
            configure: sb => sb.AddGlobalObjectIdentification(false));

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"{
                        root(where: {
                            barShort: {
                                in: [ ""Rm9vCnMxMg=="", ""Rm9vCnMxMw==""]
                            }
                        }){
                            barShort
                        }
                    }")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"{
                        root(where: {
                            barShort: {
                                in: [ ""Rm9vCnMxMw=="", ""Rm9vCnMxNA==""]
                            }
                        }){
                            barShort
                        }
                    }")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { barShort: { in: [ null, \"Rm9vCnMxNA==\"]}}){ barShort}}")
                .Create());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "12and13")
            .Add(res2, "13and14")
            .Add(res3, "nullAnd14")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNotIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooShort, FooShortFilterInput>(
            _fooShortEntities,
            configure: sb => sb.AddGlobalObjectIdentification(false));

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"{
                        root(where: {
                            barShort: {
                                nin: [ ""Rm9vCnMxMg=="", ""Rm9vCnMxMw==""]
                            }
                        }){
                            barShort
                        }
                    }")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { barShort: { nin: " +
                    "[ \"Rm9vCnMxMg==\", \"Rm9vCnMxNA==\"]}}){ barShort}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { barShort: { nin: [ null, \"Rm9vCnMxNA==\"]}}){ barShort}}")
                .Create());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "12and13")
            .Add(res2, "13and14")
            .Add(res3, "nullAnd14")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableIn_Expression()
    {
        // arrange
        var tester =
            _cache.CreateSchema<FooShortNullable, FooShortNullableFilterInput>(
                _fooShortNullableEntities,
                configure: sb => sb.AddGlobalObjectIdentification(false));

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"{
                        root(where: {
                            barShort: {
                                in: [ ""Rm9vCnMxMg=="", ""Rm9vCnMxMw==""]
                            }
                        }){
                            barShort
                        }
                    }")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"{ root(where: {
                            barShort: {
                                in: [ ""Rm9vCnMxMw=="", ""Rm9vCnMxNA==""]
                            }
                        }){
                            barShort
                        }
                    }")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { barShort: { in: [ \"Rm9vCnMxMw==\", null ]}}){ barShort}}")
                .Create());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "12and13")
            .Add(res2, "13and14")
            .Add(res3, "13andNull")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotIn_Expression()
    {
        // assert
        var tester =
            _cache.CreateSchema<FooShortNullable, FooShortNullableFilterInput>(
                _fooShortNullableEntities,
                configure: sb => sb.AddGlobalObjectIdentification(false));

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"{
                        root(where: {
                            barShort: {
                                nin: [ ""Rm9vCnMxMg=="", ""Rm9vCnMxMw==""]
                            }
                        }){
                            barShort
                        }
                    }")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"{
                        root(where: {
                            barShort: {
                                nin: [ ""Rm9vCnMxMw=="", ""Rm9vCnMxNA==""]
                            }
                        }){
                            barShort
                        }
                    }")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"{
                        root(where: {
                            barShort: {
                                nin: [ ""Rm9vCnMxMw=="", null ]
                            }
                        }){
                            barShort
                        }
                    }")
                .Create());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "12and13")
            .Add(res2, "13and14")
            .Add(res3, "13andNull")
            .MatchAsync();
    }

    public class Foo
    {
        public int Id { get; set; }

        [ID]
        public string Bar { get; set; } = null!;
    }

    public class FooShort
    {
        public int Id { get; set; }

        [ID]
        public short BarShort { get; set; }
    }

    public class FooShortNullable
    {
        public int Id { get; set; }

        [ID]
        public short? BarShort { get; set; }
    }

    public class FooNullable
    {
        public int Id { get; set; }

        [ID]
        public string? Bar { get; set; }
    }

    public class FooShortFilterInput : FilterInputType<FooShort>
    {
        protected override void Configure(IFilterInputTypeDescriptor<FooShort> descriptor)
        {
            descriptor.Field(t => t.BarShort);
        }
    }

    public class FooShortNullableFilterInput : FilterInputType<FooShortNullable>
    {
        protected override void Configure(
            IFilterInputTypeDescriptor<FooShortNullable> descriptor)
        {
            descriptor.Field(t => t.BarShort);
        }
    }

    public class FooFilterInput : FilterInputType<Foo>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(t => t.Bar);
        }
    }

    public class FooNullableFilterInput : FilterInputType<FooNullable>
    {
        protected override void Configure(IFilterInputTypeDescriptor<FooNullable> descriptor)
        {
            descriptor.Field(t => t.Bar);
        }
    }
}
