using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Xunit;

namespace HotChocolate.Data.Sorting.Expressions
{
    public class QueryableSortVisitorEnumTests
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
            new FooNullable { BarEnum = null },
            new FooNullable { BarEnum = FooEnum.QUX }
        };

        private readonly SchemaCache _cache;

        public QueryableSortVisitorEnumTests(
            SchemaCache cache)
        {
            _cache = cache;
        }

        [Fact]
        public async Task Create_Enum_OrderBy()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema<Foo, FooSortType>(_fooEntities);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { barEnum: ASC}){ barEnum}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { barEnum: DESC}){ barEnum}}")
                    .Create());

            // assert
            res1.MatchSnapshot("ASC");
            res2.MatchSnapshot("DESC");
        }

        [Fact]
        public async Task Create_Enum_OrderBy_Nullable()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema<FooNullable, FooNullableSortType>(
                _fooNullableEntities);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { barEnum: ASC}){ barEnum}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { barEnum: DESC}){ barEnum}}")
                    .Create());

            // assert
            res1.MatchSnapshot("ASC");
            res2.MatchSnapshot("DESC");
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
