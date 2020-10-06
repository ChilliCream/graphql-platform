using System.Threading.Tasks;
using HotChocolate.Execution;
using Xunit;

namespace HotChocolate.Data.Projections
{
    public class QueryableProjectionVisitorScalarTests
    {
        private static readonly Foo[] _fooEntities =
        {
            new Foo { Bar = true, Baz = "a" }, new Foo { Bar = false, Baz = "b" }
        };

        private static readonly FooNullable[] _fooNullableEntities =
        {
            new FooNullable { Bar = true, Baz = "a" },
            new FooNullable { Bar = null, Baz = null },
            new FooNullable { Bar = false, Baz = "c" }
        };

        private readonly SchemaCache _cache = new SchemaCache();

        [Fact]
        public async Task Create_ProjectsTwoProperties_Expression()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ bar baz }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task Create_ProjectsOneProperty_Expression()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ bar baz }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        public class Foo
        {
            public int Id { get; set; }

            public bool Bar { get; set; }

            public string Baz { get; set; }
        }

        public class FooNullable
        {
            public int Id { get; set; }

            public bool? Bar { get; set; }

            public string? Baz { get; set; }
        }
    }
}
