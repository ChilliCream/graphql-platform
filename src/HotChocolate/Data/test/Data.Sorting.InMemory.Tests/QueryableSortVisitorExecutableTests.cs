using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Xunit;

namespace HotChocolate.Data.Sorting
{
    public class QueryableSortVisitorExecutableTests
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

        public QueryableSortVisitorExecutableTests(
            SchemaCache cache)
        {
            _cache = cache;
        }

        [Fact]
        public async Task Create_Boolean_OrderBy()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema<Foo, FooSortType>(_fooEntities);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ rootExecutable(order: { bar: ASC}){ bar}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ rootExecutable(order: { bar: DESC}){ bar}}")
                    .Create());

            // assert
            res1.MatchSnapshot("ASC");
            res2.MatchSnapshot("DESC");
        }

        [Fact]
        public async Task Create_Boolean_OrderBy_List()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema<Foo, FooSortType>(_fooEntities);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ rootExecutable(order: [{ bar: ASC}]){ bar}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ rootExecutable(order: [{ bar: DESC}]){ bar}}")
                    .Create());

            // assert
            res1.MatchSnapshot("ASC");
            res2.MatchSnapshot("DESC");
        }

        [Fact]
        public async Task Create_Boolean_OrderBy_Nullable()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema<FooNullable, FooNullableSortType>(
                _fooNullableEntities);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ rootExecutable(order: { bar: ASC}){ bar}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ rootExecutable(order: { bar: DESC}){ bar}}")
                    .Create());

            // assert
            res1.MatchSnapshot("ASC");
            res2.MatchSnapshot("DESC");
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

        public class FooSortType
            : SortInputType<Foo>
        {
        }

        public class FooNullableSortType
            : SortInputType<FooNullable>
        {
        }
    }
}
