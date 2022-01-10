using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types.Relay;
using Xunit;

namespace HotChocolate.Data.Filters;

public class QueryableFilterVisitorIdTests
    : IClassFixture<SchemaCache>
{
    private static readonly Foo[] _fooEntities = new[]
    {
            new Foo { Bar = "testatest" },
            new Foo { Bar = "testbtest" }
        };

    private static readonly FooNullable[] _fooNullableEntities = new[]
    {
            new FooNullable { Bar = "testatest" },
            new FooNullable { Bar = "testbtest" },
            new FooNullable { Bar = null }
        };

    private static readonly FooShort[] _fooShortEntities = new[]
    {
            new FooShort { BarShort = 12 },
            new FooShort { BarShort = 14 },
            new FooShort { BarShort = 13 }
        };

    private static readonly FooShortNullable[] _fooShortNullableEntities = new[]
    {
            new FooShortNullable { BarShort = 12 },
            new FooShortNullable { BarShort = null },
            new FooShortNullable { BarShort = 14 },
            new FooShortNullable { BarShort = 13 }
        };

    private readonly SchemaCache _cache;

    public QueryableFilterVisitorIdTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Create_StringIdEqual_Expression()
    {
        // arrange
        IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        // assert
        IExecutionResult? res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: \"Rm8KZHRlc3RhdGVzdA==\"}}){ bar}}")
                .Create());

        res1.MatchSnapshot("testatest");

        IExecutionResult? res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: \"Rm8KZHRlc3RidGVzdA==\"}}){ bar}}")
                .Create());

        res2.MatchSnapshot("testbtest");

        IExecutionResult? res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: null}}){ bar}}")
                .Create());

        res3.MatchSnapshot("null");
    }

    [Fact]
    public async Task Create_StringIdNotEqual_Expression()
    {
        // arrange
        IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        // assert
        IExecutionResult? res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: \"Rm8KZHRlc3RhdGVzdA==\"}}){ bar}}")
                .Create());

        res1.MatchSnapshot("testatest");

        IExecutionResult? res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: \"Rm8KZHRlc3RidGVzdA==\"}}){ bar}}")
                .Create());

        res2.MatchSnapshot("testbtest");

        IExecutionResult? res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: null}}){ bar}}")
                .Create());

        res3.MatchSnapshot("null");
    }

    [Fact]
    public async Task Create_StringIdIn_Expression()
    {
        // arrange
        IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        // assert
        IExecutionResult? res1 = await tester.ExecuteAsync(
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

        res1.MatchSnapshot("testatestAndtestb");

        IExecutionResult? res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { bar: { in: [\"Rm8KZHRlc3RidGVzdA==\" null]}}){ bar}}")
                .Create());

        res2.MatchSnapshot("testbtestAndNull");

        IExecutionResult? res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { in: [ \"Rm8KZHRlc3RhdGVzdA==\" ]}}){ bar}}")
                .Create());

        res3.MatchSnapshot("testatest");
    }

    [Fact]
    public async Task Create_StringIdNotIn_Expression()
    {
        // arrange
        IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        // assert
        IExecutionResult? res1 = await tester.ExecuteAsync(
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

        res1.MatchSnapshot("testatestAndtestb");

        IExecutionResult? res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { bar: { nin: [\"Rm8KZHRlc3RidGVzdA==\" null]}}){ bar}}")
                .Create());

        res2.MatchSnapshot("testbtestAndNull");

        IExecutionResult? res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { nin: [ \"Rm8KZHRlc3RhdGVzdA==\" ]}}){ bar}}")
                .Create());

        res3.MatchSnapshot("testatest");
    }

    [Fact]
    public async Task Create_NullableStringIdEqual_Expression()
    {
        // arrange
        IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(
            _fooNullableEntities);

        // act
        // assert
        IExecutionResult? res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: \"Rm8KZHRlc3RhdGVzdA==\"}}){ bar}}")
                .Create());

        res1.MatchSnapshot("testatest");

        IExecutionResult? res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: \"Rm8KZHRlc3RidGVzdA==\"}}){ bar}}")
                .Create());

        res2.MatchSnapshot("testbtest");

        IExecutionResult? res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: null}}){ bar}}")
                .Create());

        res3.MatchSnapshot("null");
    }

    [Fact]
    public async Task Create_NullableStringIdNotEqual_Expression()
    {
        // arrange
        IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(
            _fooNullableEntities);

        // act
        // assert
        IExecutionResult? res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: \"Rm8KZHRlc3RhdGVzdA==\"}}){ bar}}")
                .Create());

        res1.MatchSnapshot("testatest");

        IExecutionResult? res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: \"Rm8KZHRlc3RidGVzdA==\"}}){ bar}}")
                .Create());

        res2.MatchSnapshot("testbtest");

        IExecutionResult? res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: null}}){ bar}}")
                .Create());

        res3.MatchSnapshot("null");
    }

    [Fact]
    public async Task Create_NullableStringIdIn_Expression()
    {
        // arrange
        IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(
            _fooNullableEntities);

        // act
        // assert
        IExecutionResult? res1 = await tester.ExecuteAsync(
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

        res1.MatchSnapshot("testatestAndtestb");

        IExecutionResult? res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { bar: { in: [\"Rm8KZHRlc3RidGVzdA==\" null]}}){ bar}}")
                .Create());

        res2.MatchSnapshot("testbtestAndNull");

        IExecutionResult? res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { in: [ \"Rm8KZHRlc3RhdGVzdA==\" ]}}){ bar}}")
                .Create());

        res3.MatchSnapshot("testatest");
    }

    [Fact]
    public async Task Create_NullableStringIdNotIn_Expression()
    {
        // arrange
        IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(
            _fooNullableEntities);

        // act
        // assert
        IExecutionResult? res1 = await tester.ExecuteAsync(
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

        res1.MatchSnapshot("testatestAndtestb");

        IExecutionResult? res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { bar: { nin: [\"Rm8KZHRlc3RidGVzdA==\" null]}}){ bar}}")
                .Create());

        res2.MatchSnapshot("testbtestAndNull");

        IExecutionResult? res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { nin: [ \"Rm8KZHRlc3RhdGVzdA==\" ]}}){ bar}}")
                .Create());

        res3.MatchSnapshot("testatest");
    }

    [Fact]
    public async Task Create_ShortEqual_Expression()
    {
        // arrange
        IRequestExecutor? tester =
            _cache.CreateSchema<FooShort, FooShortFilterInput>(_fooShortEntities);

        // act
        // assert
        IExecutionResult? res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { eq: \"Rm9vCnMxMg==\"}}){ barShort}}")
                .Create());

        res1.MatchSnapshot("12");

        IExecutionResult? res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { eq: \"Rm9vCnMxMw==\"}}){ barShort}}")
                .Create());

        res2.MatchSnapshot("13");

        IExecutionResult? res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { eq: null}}){ barShort}}")
                .Create());

        res3.MatchSnapshot("null");
    }

    [Fact]
    public async Task Create_ShortNotEqual_Expression()
    {
        IRequestExecutor? tester =
            _cache.CreateSchema<FooShort, FooShortFilterInput>(_fooShortEntities);

        // act
        // assert
        IExecutionResult? res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { neq: \"Rm9vCnMxMg==\"}}){ barShort}}")
                .Create());

        res1.MatchSnapshot("12");

        IExecutionResult? res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { neq: \"Rm9vCnMxMw==\"}}){ barShort}}")
                .Create());

        res2.MatchSnapshot("13");

        IExecutionResult? res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { neq: null}}){ barShort}}")
                .Create());

        res3.MatchSnapshot("null");
    }

    [Fact]
    public async Task Create_ShortNullableEqual_Expression()
    {
        // arrange
        IRequestExecutor? tester =
            _cache.CreateSchema<FooShortNullable, FooShortNullableFilterInput>(
                _fooShortNullableEntities);

        // act
        // assert
        IExecutionResult? res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { eq: \"Rm9vCnMxMg==\"}}){ barShort}}")
                .Create());

        res1.MatchSnapshot("12");

        IExecutionResult? res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { eq: \"Rm9vCnMxMw==\"}}){ barShort}}")
                .Create());

        res2.MatchSnapshot("13");

        IExecutionResult? res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { eq: null}}){ barShort}}")
                .Create());

        res3.MatchSnapshot("null");
    }

    [Fact]
    public async Task Create_ShortNullableNotEqual_Expression()
    {
        IRequestExecutor? tester =
            _cache.CreateSchema<FooShortNullable, FooShortNullableFilterInput>(
                _fooShortNullableEntities);

        // act
        // assert
        IExecutionResult? res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { neq: \"Rm9vCnMxMg==\"}}){ barShort}}")
                .Create());

        res1.MatchSnapshot("12");

        IExecutionResult? res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { neq: \"Rm9vCnMxMw==\"}}){ barShort}}")
                .Create());

        res2.MatchSnapshot("13");

        IExecutionResult? res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { neq: null}}){ barShort}}")
                .Create());

        res3.MatchSnapshot("null");
    }

    [Fact]
    public async Task Create_ShortIn_Expression()
    {
        IRequestExecutor? tester =
            _cache.CreateSchema<FooShort, FooShortFilterInput>(_fooShortEntities);

        IExecutionResult? res1 = await tester.ExecuteAsync(
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

        res1.MatchSnapshot("12and13");

        IExecutionResult? res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { barShort: { in: [ null, \"Rm9vCnMxNA==\"]}}){ barShort}}")
                .Create());

        res2.MatchSnapshot("13and14");

        IExecutionResult? res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { barShort: { in: [ null, \"Rm9vCnMxNA==\"]}}){ barShort}}")
                .Create());

        res3.MatchSnapshot("nullAnd14");
    }

    [Fact]
    public async Task Create_ShortNotIn_Expression()
    {
        IRequestExecutor? tester =
            _cache.CreateSchema<FooShort, FooShortFilterInput>(_fooShortEntities);

        IExecutionResult? res1 = await tester.ExecuteAsync(
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

        res1.MatchSnapshot("12and13");

        IExecutionResult? res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { barShort: { nin: [ null, \"Rm9vCnMxNA==\"]}}){ barShort}}")
                .Create());

        res2.MatchSnapshot("13and14");

        IExecutionResult? res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { barShort: { nin: [ null, \"Rm9vCnMxNA==\"]}}){ barShort}}")
                .Create());

        res3.MatchSnapshot("nullAnd14");
    }

    [Fact]
    public async Task Create_ShortNullableIn_Expression()
    {
        IRequestExecutor? tester =
            _cache.CreateSchema<FooShortNullable, FooShortNullableFilterInput>(
                _fooShortNullableEntities);


        IExecutionResult? res1 = await tester.ExecuteAsync(
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

        res1.MatchSnapshot("12and13");

        IExecutionResult? res2 = await tester.ExecuteAsync(
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

        res2.MatchSnapshot("13and14");

        IExecutionResult? res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { barShort: { in: [ \"Rm9vCnMxMw==\", null ]}}){ barShort}}")
                .Create());

        res3.MatchSnapshot("13andNull");
    }

    [Fact]
    public async Task Create_ShortNullableNotIn_Expression()
    {
        IRequestExecutor? tester =
            _cache.CreateSchema<FooShortNullable, FooShortNullableFilterInput>(
                _fooShortNullableEntities);

        IExecutionResult? res1 = await tester.ExecuteAsync(
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

        res1.MatchSnapshot("12and13");

        IExecutionResult? res2 = await tester.ExecuteAsync(
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

        res2.MatchSnapshot("13and14");

        IExecutionResult? res3 = await tester.ExecuteAsync(
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

        res3.MatchSnapshot("13andNull");
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

    public class FooShortFilterInput
        : FilterInputType<FooShort>
    {
        protected override void Configure(IFilterInputTypeDescriptor<FooShort> descriptor)
        {
            descriptor.Field(t => t.BarShort);
        }
    }

    public class FooShortNullableFilterInput
        : FilterInputType<FooShortNullable>
    {
        protected override void Configure(
            IFilterInputTypeDescriptor<FooShortNullable> descriptor)
        {
            descriptor.Field(t => t.BarShort);
        }
    }

    public class FooFilterInput
        : FilterInputType<Foo>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(t => t.Bar);
        }
    }

    public class FooNullableFilterInput
        : FilterInputType<FooNullable>
    {
        protected override void Configure(IFilterInputTypeDescriptor<FooNullable> descriptor)
        {
            descriptor.Field(t => t.Bar);
        }
    }
}
