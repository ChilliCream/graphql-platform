using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Xunit;

namespace HotChocolate.Data.Sorting
{
    public class QueryableSortVisitorStringTests
        : IClassFixture<SchemaCache>
    {
        private static readonly Foo[] _fooEntities =
        {
            new Foo { Bar = "testatest" },
            new Foo { Bar = "testbtest" }
        };

        private static readonly FooNullable[] _fooNullableEntities =
        {
            new FooNullable { Bar = "testatest" },
            new FooNullable { Bar = "testbtest" },
            new FooNullable { Bar = null }
        };

        private readonly SchemaCache _cache;

        public QueryableSortVisitorStringTests(
            SchemaCache cache)
        {
            _cache = cache;
        }

        [Fact]
        public async Task Create_String_OrderBy()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema<Foo, FooSortType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { bar: ASC}){ bar}}")
                    .Create());

            res1.MatchSnapshot("ASC");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { bar: DESC}){ bar}}")
                    .Create());

            res2.MatchSnapshot("DESC");
        }

        [Fact]
        public async Task Create_String_OrderBy_Nullable()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema<FooNullable, FooNullableSortType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { bar: ASC}){ bar}}")
                    .Create());

            res1.MatchSnapshot("ASC");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { bar: DESC}){ bar}}")
                    .Create());

            res2.MatchSnapshot("DESC");
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

        public class FooSortType
            : SortInputType<Foo>
        {
            protected override void Configure(
                ISortInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Field(t => t.Bar);
            }
        }

        public class FooNullableSortType
            : SortInputType<FooNullable>
        {
            protected override void Configure(
                ISortInputTypeDescriptor<FooNullable> descriptor)
            {
                descriptor.Field(t => t.Bar);
            }
        }
    }
}
