using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Xunit;

namespace HotChocolate.Data.SqlKata.Filters
{
    public class SqlKataFilterVisitorBooleanTests
    {
        private static readonly Foo[] _fooEntities =
        {
            new() { Bar = true },
            new() { Bar = false }
        };

        private static readonly FooNullable[] _fooNullableEntities =
        {
            new() { Bar = true },
            new() { Bar = null },
            new() { Bar = false }
        };

        private readonly SchemaCache _cache = new();

        [Fact]
        public async Task Create_BooleanEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: true}}){ bar}}")
                    .Create());

            res1.MatchSqlSnapshot("true");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: false}}){ bar}}")
                    .Create());

            res2.MatchSqlSnapshot("false");
        }

        [Fact]
        public async Task Create_BooleanNotEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: true}}){ bar}}")
                    .Create());

            res1.MatchSqlSnapshot("true");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: false}}){ bar}}")
                    .Create());

            res2.MatchSqlSnapshot("false");
        }

        [Fact]
        public async Task Create_NullableBooleanEqual_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: true}}){ bar}}")
                    .Create());

            res1.MatchSqlSnapshot("true");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: false}}){ bar}}")
                    .Create());

            res2.MatchSqlSnapshot("false");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: null}}){ bar}}")
                    .Create());

            res3.MatchSqlSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableBooleanNotEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: true}}){ bar}}")
                    .Create());

            res1.MatchSqlSnapshot("true");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: false}}){ bar}}")
                    .Create());

            res2.MatchSqlSnapshot("false");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: null}}){ bar}}")
                    .Create());

            res3.MatchSqlSnapshot("null");
        }

        [Table("ExampleData")]
        public class Foo
        {
            public int Id { get; set; }

            public bool Bar { get; set; }
        }

        [Table("ExampleData")]
        public class FooNullable
        {
            public int Id { get; set; }

            public bool? Bar { get; set; }
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
