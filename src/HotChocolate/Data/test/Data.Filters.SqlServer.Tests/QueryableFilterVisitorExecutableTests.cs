using System.Threading.Tasks;
using HotChocolate.Execution;
using Xunit;

namespace HotChocolate.Data.Filters
{
    public class QueryableFilterVisitorExecutableTests
        : IClassFixture<SchemaCache>
    {
        private static readonly Foo[] _fooEntities =
        {
            new Foo { Bar = true }, new Foo { Bar = false }
        };

        private static readonly FooNullable[] _fooNullableEntities =
        {
            new FooNullable { Bar = true },
            new FooNullable { Bar = null },
            new FooNullable { Bar = false }
        };

        private readonly SchemaCache _cache;

        public QueryableFilterVisitorExecutableTests(
            SchemaCache cache)
        {
            _cache = cache;
        }

        [Fact]
        public async Task Create_BooleanEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ rootExecutable(where: { bar: { eq: true}}){ bar}}")
                    .Create());

            res1.MatchSqlSnapshot("true");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ rootExecutable(where: { bar: { eq: false}}){ bar}}")
                    .Create());

            res2.MatchSqlSnapshot("false");
        }

        [Fact]
        public async Task Create_BooleanNotEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ rootExecutable(where: { bar: { neq: true}}){ bar}}")
                    .Create());

            res1.MatchSqlSnapshot("true");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ rootExecutable(where: { bar: { neq: false}}){ bar}}")
                    .Create());

            res2.MatchSqlSnapshot("false");
        }

        [Fact]
        public async Task Create_NullableBooleanEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ rootExecutable(where: { bar: { eq: true}}){ bar}}")
                    .Create());

            res1.MatchSqlSnapshot("true");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ rootExecutable(where: { bar: { eq: false}}){ bar}}")
                    .Create());

            res2.MatchSqlSnapshot("false");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ rootExecutable(where: { bar: { eq: null}}){ bar}}")
                    .Create());

            res3.MatchSqlSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableBooleanNotEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ rootExecutable(where: { bar: { neq: true}}){ bar}}")
                    .Create());

            res1.MatchSqlSnapshot("true");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ rootExecutable(where: { bar: { neq: false}}){ bar}}")
                    .Create());

            res2.MatchSqlSnapshot("false");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ rootExecutable(where: { bar: { neq: null}}){ bar}}")
                    .Create());

            res3.MatchSqlSnapshot("null");
        }

        public class Foo
        {
            public int Id { get; set; }

            public bool Bar { get; set; }
        }

        public class FooNullable
        {
            public int Id { get; set; }

            public bool? Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
        }

        public class FooNullableFilterType
            : FilterInputType<FooNullable>
        {
        }
    }
}
