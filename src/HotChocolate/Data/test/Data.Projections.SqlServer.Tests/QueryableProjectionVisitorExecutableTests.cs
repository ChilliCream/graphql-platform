using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Data.Projections.Expressions
{
    public class QueryableProjectionVisitorExecutableTests
    {
        private static readonly Foo[] _fooEntities =
        {
            new Foo { Bar = true, Baz = "a" }, new Foo { Bar = false, Baz = "b" }
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
                    .SetQuery("{ rootExecutable{ bar baz }}")
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
                    .SetQuery("{ rootExecutable{ baz }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task Create_ProjectsOneProperty_WithResolver()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooEntities,
                objectType: new ObjectType<Foo>(
                    x => x
                        .Field("foo")
                        .Resolver(new[] { "foo" })
                        .Type<ListType<StringType>>()));

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ rootExecutable{ baz foo }}")
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
