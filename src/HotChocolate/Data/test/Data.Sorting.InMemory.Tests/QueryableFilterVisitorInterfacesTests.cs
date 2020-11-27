using System.Threading.Tasks;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableFilterVisitorInterfacesTests
            : IClassFixture<SchemaCache>
        {
        private static readonly BarInterface[] _barEntities =
        {
            new BarInterface { Test = new InterfaceImpl1 { Prop = "a" } },
            new BarInterface { Test = new InterfaceImpl1 { Prop = "b" } }
        };

        private readonly SchemaCache _cache;

        public QueryableFilterVisitorInterfacesTests(
            SchemaCache cache)
        {
            _cache = cache;
        }

        [Fact]
        public async Task Create_InterfaceStringEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = _cache
                .CreateSchema<BarInterface, SortInputType<BarInterface>>(
                    _barEntities,
                    configure: Configure);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(order: { test: { prop: ASC}}) " +
                        "{ test{ prop }}}")
                    .Create());

            res1.MatchSnapshot("ASC");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(order: { test: { prop: DESC}}) " +
                        "{ test{ prop }}}")
                    .Create());

            res2.MatchSnapshot("DESC");
        }

        public static void Configure(ISchemaBuilder builder)
        {
            builder.AddObjectType<InterfaceImpl1>();
            builder.AddObjectType<InterfaceImpl2>();
        }

        public interface ITest
        {
            string Prop { get; set; }
        }

        public class InterfaceImpl1 : ITest
        {
            public string Prop { get; set; }

            public string Specific1 { get; set; }
        }

        public class InterfaceImpl2 : ITest
        {
            public string Prop { get; set; }

            public string Specific2 { get; set; }
        }

        public class BarInterface
        {
            public ITest Test { get; set; }
        }
    }
}
