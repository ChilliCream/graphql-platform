using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Xunit;

namespace HotChocolate.Data.Filters.Expressions
{

    public class QueryableFilterVisitorEnumTests
        : IClassFixture<SchemaCache>
    {
        private static readonly Foo[] _fooEntities =
        {
            new Foo { BarEnum = FooEnum.BAR },
            new Foo { BarEnum = FooEnum.BAZ },
            new Foo { BarEnum = FooEnum.FOO },
            new Foo { BarEnum = FooEnum.QUX }
        };

        private static readonly FooNullable[] _fooNullableEntities =
        {
            new FooNullable { BarEnum = FooEnum.BAR },
            new FooNullable { BarEnum = FooEnum.BAZ },
            new FooNullable { BarEnum = FooEnum.FOO },
            new FooNullable { BarEnum = null},
            new FooNullable { BarEnum = FooEnum.QUX }
        };

        private readonly SchemaCache _cache;

        public QueryableFilterVisitorEnumTests(
            SchemaCache cache)
        {
            _cache = cache;
        }

        [Fact]
        public async Task Create_EnumEqual_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { eq: BAR}}){ barEnum}}")
                .Create());

            res1.MatchSnapshot("BAR");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { eq: FOO}}){ barEnum}}")
                .Create());

            res2.MatchSnapshot("FOO");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { eq: null}}){ barEnum}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_EnumNotEqual_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { neq: BAR}}){ barEnum}}")
                .Create());

            res1.MatchSnapshot("BAR");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { neq: FOO}}){ barEnum}}")
                .Create());

            res2.MatchSnapshot("FOO");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { neq: null}}){ barEnum}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_EnumIn_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { in: [ BAR FOO ]}}){ barEnum}}")
                .Create());

            res1.MatchSnapshot("BarAndFoo");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { in: [ FOO ]}}){ barEnum}}")
                .Create());

            res2.MatchSnapshot("FOO");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { in: [ null FOO ]}}){ barEnum}}")
                .Create());

            res3.MatchSnapshot("nullAndFoo");
        }

        [Fact]
        public async Task Create_EnumNotIn_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { nin: [ BAR FOO ]}}){ barEnum}}")
                .Create());

            res1.MatchSnapshot("BarAndFoo");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { nin: [ FOO ]}}){ barEnum}}")
                .Create());

            res2.MatchSnapshot("FOO");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { nin: [ null FOO ]}}){ barEnum}}")
                .Create());

            res3.MatchSnapshot("nullAndFoo");
        }

        [Fact]
        public async Task Create_NullableEnumEqual_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { eq: BAR}}){ barEnum}}")
                .Create());

            res1.MatchSnapshot("BAR");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { eq: FOO}}){ barEnum}}")
                .Create());

            res2.MatchSnapshot("FOO");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { eq: null}}){ barEnum}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableEnumNotEqual_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { neq: BAR}}){ barEnum}}")
                .Create());

            res1.MatchSnapshot("BAR");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { neq: FOO}}){ barEnum}}")
                .Create());

            res2.MatchSnapshot("FOO");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { neq: null}}){ barEnum}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableEnumIn_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { in: [ BAR FOO ]}}){ barEnum}}")
                .Create());

            res1.MatchSnapshot("BarAndFoo");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { in: [ FOO ]}}){ barEnum}}")
                .Create());

            res2.MatchSnapshot("FOO");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { in: [ null FOO ]}}){ barEnum}}")
                .Create());

            res3.MatchSnapshot("nullAndFoo");
        }

        [Fact]
        public async Task Create_NullableEnumNotIn_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { nin: [ BAR FOO ]}}){ barEnum}}")
                .Create());

            res1.MatchSnapshot("BarAndFoo");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { nin: [ FOO ]}}){ barEnum}}")
                .Create());

            res2.MatchSnapshot("FOO");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barEnum: { nin: [ null FOO ]}}){ barEnum}}")
                .Create());

            res3.MatchSnapshot("nullAndFoo");
        }


        public class Foo
        {
            public int Id { get; set; }

            public FooEnum BarEnum { get; set; }
        }


        public class FooNullable
        {
            public int Id { get; set; }

            public FooEnum? BarEnum { get; set; }
        }

        public enum FooEnum
        {
            FOO,
            BAR,
            BAZ,
            QUX
        }

        public class FooFilterInput
            : FilterInputType<Foo>
        {
        }

        public class FooNullableFilterInput
            : FilterInputType<FooNullable>
        {
        }
    }
}
