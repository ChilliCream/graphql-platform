using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Xunit;

namespace HotChocolate.Data.Filters
{
    public class QueryableFilterVisitorStringTests
        : IClassFixture<SchemaCache>
    {
        private static readonly Foo[] _fooEntities = new[]{
            new Foo { Bar = "testatest" },
            new Foo { Bar = "testbtest" }};

        private static readonly FooNullable[] _fooNullableEntities = new[]{
            new FooNullable { Bar = "testatest" },
            new FooNullable { Bar = "testbtest" },
            new FooNullable { Bar = null }};
        private readonly SchemaCache _cache;

        public QueryableFilterVisitorStringTests(
            SchemaCache cache)
        {
            _cache = cache;
        }

        [Fact]
        public async Task Create_StringEqual_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: \"testatest\"}}){ bar}}")
                .Create());

            res1.MatchSnapshot("testatest");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: \"testbtest\"}}){ bar}}")
                .Create());

            res2.MatchSnapshot("testbtest");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: null}}){ bar}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_StringNotEqual_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: \"testatest\"}}){ bar}}")
                .Create());

            res1.MatchSnapshot("testatest");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: \"testbtest\"}}){ bar}}")
                .Create());

            res2.MatchSnapshot("testbtest");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: null}}){ bar}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_StringIn_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { in: [ \"testatest\"  \"testbtest\" ]}}){ bar}}")
                .Create());

            res1.MatchSnapshot("testatestAndtestb");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { in: [\"testbtest\" null]}}){ bar}}")
                .Create());

            res2.MatchSnapshot("testbtestAndNull");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { in: [ \"testatest\" ]}}){ bar}}")
                .Create());

            res3.MatchSnapshot("testatest");
        }

        [Fact]
        public async Task Create_StringNotIn_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { nin: [ \"testatest\"  \"testbtest\" ]}}){ bar}}")
                .Create());

            res1.MatchSnapshot("testatestAndtestb");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { nin: [\"testbtest\" null]}}){ bar}}")
                .Create());

            res2.MatchSnapshot("testbtestAndNull");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { nin: [ \"testatest\" ]}}){ bar}}")
                .Create());

            res3.MatchSnapshot("testatest");
        }

        [Fact]
        public async Task Create_StringContains_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { contains: \"a\" }}){ bar}}")
                .Create());

            res1.MatchSnapshot("a");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { contains: \"b\" }}){ bar}}")
                .Create());

            res2.MatchSnapshot("b");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { contains: null }}){ bar}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_StringNoContains_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { ncontains: \"a\" }}){ bar}}")
                .Create());

            res1.MatchSnapshot("a");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { ncontains: \"b\" }}){ bar}}")
                .Create());

            res2.MatchSnapshot("b");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { ncontains: null }}){ bar}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_StringStartsWith_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { startsWith: \"testa\" }}){ bar}}")
                .Create());

            res1.MatchSnapshot("testa");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { startsWith: \"testb\" }}){ bar}}")
                .Create());

            res2.MatchSnapshot("testb");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { startsWith: null }}){ bar}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_StringNotStartsWith_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { nstartsWith: \"testa\" }}){ bar}}")
                .Create());

            res1.MatchSnapshot("testa");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { nstartsWith: \"testb\" }}){ bar}}")
                .Create());

            res2.MatchSnapshot("testb");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { nstartsWith: null }}){ bar}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_StringEndsWith_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { endsWith: \"atest\" }}){ bar}}")
                .Create());

            res1.MatchSnapshot("atest");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { endsWith: \"btest\" }}){ bar}}")
                .Create());

            res2.MatchSnapshot("btest");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { endsWith: null }}){ bar}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_StringNotEndsWith_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { nendsWith: \"atest\" }}){ bar}}")
                .Create());

            res1.MatchSnapshot("atest");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { nendsWith: \"btest\" }}){ bar}}")
                .Create());

            res2.MatchSnapshot("btest");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { nendsWith: null }}){ bar}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringEqual_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: \"testatest\"}}){ bar}}")
                .Create());

            res1.MatchSnapshot("testatest");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: \"testbtest\"}}){ bar}}")
                .Create());

            res2.MatchSnapshot("testbtest");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: null}}){ bar}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringNotEqual_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: \"testatest\"}}){ bar}}")
                .Create());

            res1.MatchSnapshot("testatest");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: \"testbtest\"}}){ bar}}")
                .Create());

            res2.MatchSnapshot("testbtest");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: null}}){ bar}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringIn_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { in: [ \"testatest\"  \"testbtest\" ]}}){ bar}}")
                .Create());

            res1.MatchSnapshot("testatestAndtestb");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { in: [\"testbtest\" null]}}){ bar}}")
                .Create());

            res2.MatchSnapshot("testbtestAndNull");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { in: [ \"testatest\" ]}}){ bar}}")
                .Create());

            res3.MatchSnapshot("testatest");
        }

        [Fact]
        public async Task Create_NullableStringNotIn_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { nin: [ \"testatest\"  \"testbtest\" ]}}){ bar}}")
                .Create());

            res1.MatchSnapshot("testatestAndtestb");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { nin: [\"testbtest\" null]}}){ bar}}")
                .Create());

            res2.MatchSnapshot("testbtestAndNull");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { nin: [ \"testatest\" ]}}){ bar}}")
                .Create());

            res3.MatchSnapshot("testatest");
        }

        [Fact]
        public async Task Create_NullableStringContains_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { contains: \"a\" }}){ bar}}")
                .Create());

            res1.MatchSnapshot("a");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { contains: \"b\" }}){ bar}}")
                .Create());

            res2.MatchSnapshot("b");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { contains: null }}){ bar}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringNoContains_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { ncontains: \"a\" }}){ bar}}")
                .Create());

            res1.MatchSnapshot("a");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { ncontains: \"b\" }}){ bar}}")
                .Create());

            res2.MatchSnapshot("b");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { ncontains: null }}){ bar}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringStartsWith_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { startsWith: \"testa\" }}){ bar}}")
                .Create());

            res1.MatchSnapshot("testa");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { startsWith: \"testb\" }}){ bar}}")
                .Create());

            res2.MatchSnapshot("testb");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { startsWith: null }}){ bar}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringNotStartsWith_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { nstartsWith: \"testa\" }}){ bar}}")
                .Create());

            res1.MatchSnapshot("testa");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { nstartsWith: \"testb\" }}){ bar}}")
                .Create());

            res2.MatchSnapshot("testb");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { nstartsWith: null }}){ bar}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringEndsWith_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { endsWith: \"atest\" }}){ bar}}")
                .Create());

            res1.MatchSnapshot("atest");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { endsWith: \"btest\" }}){ bar}}")
                .Create());

            res2.MatchSnapshot("btest");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { endsWith: null }}){ bar}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringNotEndsWith_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { nendsWith: \"atest\" }}){ bar}}")
                .Create());

            res1.MatchSnapshot("atest");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { nendsWith: \"btest\" }}){ bar}}")
                .Create());

            res2.MatchSnapshot("btest");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { nendsWith: null }}){ bar}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        public class Foo
        {
            public int Id { get; set; }

            public string Bar { get; set; } = null!;
        }

        public class FooNullable
        {
            public int Id { get; set; }

            public string? Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Field(t => t.Bar);
            }
        }
        public class FooNullableFilterType
            : FilterInputType<FooNullable>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<FooNullable> descriptor)
            {
                descriptor.Field(t => t.Bar);
            }
        }
    }
}
