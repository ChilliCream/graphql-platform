using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Xunit;

namespace HotChocolate.Data.Sorting
{
    public class QueryableSortVisitorComparableTests
        : IClassFixture<SchemaCache>
    {
        private static readonly Foo[] _fooEntities =
        {
            new Foo { BarShort = 12 },
            new Foo { BarShort = 14 },
            new Foo { BarShort = 13 }
        };

        private static readonly FooNullable[] _fooNullableEntities =
        {
            new FooNullable { BarShort = 12 },
            new FooNullable { BarShort = null },
            new FooNullable { BarShort = 14 },
            new FooNullable { BarShort = 13 }
        };

        private readonly SchemaCache _cache;

        public QueryableSortVisitorComparableTests(
            SchemaCache cache)
        {
            _cache = cache;
        }

        [Fact]
        public async Task Create_Short_OrderBy()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema<Foo, FooSortType>(_fooEntities);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { barShort: ASC}){ barShort}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { barShort: DESC}){ barShort}}")
                    .Create());

            // assert
            res1.MatchSqlSnapshot("ASC");
            res2.MatchSqlSnapshot("DESC");
        }

        [Fact]
        public async Task Create_Short_OrderBy_Nullable()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema<FooNullable, FooNullableSortType>(
                _fooNullableEntities);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { barShort: ASC}){ barShort}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { barShort: DESC}){ barShort}}")
                    .Create());

            // assert
            res1.MatchSqlSnapshot("ASC");
            res2.MatchSqlSnapshot("DESC");
        }

        public class Foo
        {
            public int Id { get; set; }

            public short BarShort { get; set; }

            public int BarInt { get; set; }

            public long BarLong { get; set; }

            public float BarFloat { get; set; }

            public double BarDouble { get; set; }

            public decimal BarDecimal { get; set; }
        }

        public class FooNullable
        {
            public int Id { get; set; }
            public short? BarShort { get; set; }
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
