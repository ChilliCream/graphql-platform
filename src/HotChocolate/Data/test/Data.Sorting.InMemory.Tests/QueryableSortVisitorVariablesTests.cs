using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Xunit;

namespace HotChocolate.Data.Sorting
{
    public class QueryableSortVisitorVariablesTests
        : IClassFixture<SchemaCache>
    {
        private static readonly Foo[] _fooEntities =
        {
            new() { Bar = true },
            new() { Bar = false }
        };

        private readonly SchemaCache _cache;

        public QueryableSortVisitorVariablesTests(
            SchemaCache cache)
        {
            _cache = cache;
        }

        [Fact]
        public async Task Create_Boolean_OrderBy()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema<Foo, FooSortType>(_fooEntities);
            const string query =
                "query Test($order: SortEnumType){ root(order: [{ bar: $order}]){ bar}}";

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query)
                    .AddVariableValue("order", "ASC")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query)
                    .AddVariableValue("order", "DESC")
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

        public class FooSortType
            : SortInputType<Foo>
        {
        }
    }
}
