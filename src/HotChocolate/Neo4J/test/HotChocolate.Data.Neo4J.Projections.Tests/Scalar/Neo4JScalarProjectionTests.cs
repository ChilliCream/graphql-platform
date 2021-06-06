using System.Threading.Tasks;
using HotChocolate.Execution;
using Xunit;

namespace HotChocolate.Data.Neo4J.Projections.Scalar
{
    public class Neo4JScalarProjectionTest
        : IClassFixture<Neo4JFixture>
    {
        private readonly Neo4JFixture _fixture;

        public Neo4JScalarProjectionTest(Neo4JFixture fixture)
        {
            _fixture = fixture;
        }

        private string _fooEntitiesCypher = @"
            CREATE (:Foo {Bar: true, Baz: 'a'}), (:Foo {Bar: false, Baz: 'b'})
        ";

        public class Foo
        {
            public bool Bar { get; set; }
            public string Baz { get; set; } = null!;
        }

        [Fact]
        public async Task Create_ProjectsTwoProperties_Expression()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<Foo>(_fooEntitiesCypher);

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
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<Foo>(_fooEntitiesCypher);

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
