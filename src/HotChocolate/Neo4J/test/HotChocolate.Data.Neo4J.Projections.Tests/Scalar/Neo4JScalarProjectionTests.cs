using System.Threading.Tasks;
using HotChocolate.Execution;
using Squadron;
using Xunit;

namespace HotChocolate.Data.Neo4J.Projections.Scalar
{
    public class Neo4JScalarProjectionTest
        : IClassFixture<Neo4jResource>
    {
        private string _fooEntities = @"
            CREATE (:Foo {Bar: true, Baz: 'a'}), (:Foo {Bar: false, Baz: 'b'})
        ";
        public class Foo
        {
            public bool Bar { get; set; }
            public string Baz { get; set; } = null!;
        }

        private readonly SchemaCache _cache;

        public Neo4JScalarProjectionTest(Neo4jResource resource)
        {
            _cache = new SchemaCache(resource);
        }

        [Fact]
        public async Task Create_ProjectsTwoProperties_Expression()
        {
            // arrange
            IRequestExecutor tester = await _cache.CreateSchema<Foo>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ bar baz }}")
                    .Create());

            res1.MatchDocumentSnapshot();
        }

        [Fact]
        public async Task Create_ProjectsOneProperty_Expression()
        {
            // arrange
            IRequestExecutor tester = await _cache.CreateSchema<Foo>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ baz }}")
                    .Create());

            res1.MatchDocumentSnapshot();
        }
    }
}
