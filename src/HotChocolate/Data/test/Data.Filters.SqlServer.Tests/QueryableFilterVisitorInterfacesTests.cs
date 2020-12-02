using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
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
                .CreateSchema<BarInterface, FilterInputType<BarInterface>>(
                    _barEntities,
                    configure: Configure,
                    onModelCreating: OnModelCreating);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { test: { prop: { eq: \"a\"}}}) " +
                        "{ test{ prop }}}")
                    .Create());

            res1.MatchSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { test: { prop: { eq: \"b\"}}}) " +
                        "{ test{ prop }}}")
                    .Create());

            res2.MatchSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { test: { prop: { eq: null}}}) " +
                        "{ test{ prop}}}")
                    .Create());

            res3.MatchSnapshot("null");
        }

        public static void Configure(ISchemaBuilder builder)
        {
            builder.AddObjectType<InterfaceImpl1>(x => x.Implements<InterfaceType<Test>>());
            builder.AddObjectType<InterfaceImpl2>(x => x.Implements<InterfaceType<Test>>());
            builder.AddInterfaceType<Test>();
        }

        public static void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Test>()
                .HasDiscriminator<string>("_t")
                .HasValue<InterfaceImpl1>(nameof(InterfaceImpl1))
                .HasValue<InterfaceImpl2>(nameof(InterfaceImpl2));
        }

        public abstract class Test
        {
            [Key]
            public int Id { get; set; }

            public string Prop { get; set; }
        }

        public class InterfaceImpl1 : Test
        {
            public string Specific1 { get; set; }
        }

        public class InterfaceImpl2 : Test
        {
            public string Specific2 { get; set; }
        }

        public class BarInterface
        {
            [Key]
            public int Id { get; set; }

            public Test Test { get; set; }
        }
    }
}
