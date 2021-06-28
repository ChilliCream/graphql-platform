using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using Xunit;

namespace HotChocolate.Data.Filters
{
    public class FilteringAndPaging
    {
        private static readonly Foo[] _fooEntities =
        {
            new Foo { Bar = true },
            new Foo { Bar = false }
        };

        private readonly SchemaCache _cache = new SchemaCache();

        [Fact]
        public async Task Create_BooleanEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities, true);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: true}}){ nodes { bar } }}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: false}}){ nodes { bar }}}")
                    .Create());

            // assert
            res1.MatchSqlSnapshot("true");
            res2.MatchSqlSnapshot("false");
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
