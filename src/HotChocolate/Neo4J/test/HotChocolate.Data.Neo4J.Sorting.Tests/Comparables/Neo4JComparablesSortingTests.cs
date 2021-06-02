using System.Threading.Tasks;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using Squadron;
using Xunit;

namespace HotChocolate.Data.Neo4J.Sorting.Boolean
{
    public class Neo4JComparablesSortingTests
        : IClassFixture<Neo4JFixture>
    {
        private readonly Neo4JFixture _fixture;

        public Neo4JComparablesSortingTests(Neo4JFixture fixture)
        {
            _fixture = fixture;
        }

        private string _fooEntitiesCypher = @"
            CREATE (:Foo {Bar: 12}), (:Foo {Bar: 14}), (:Foo {Bar: 13})
        ";

        public class Foo
        {
            public short Bar { get; set; }
        }

        public class FooSortType
            : SortInputType<Foo>
        {
        }

        [Fact]
        public async Task Create_Short_OrderBy()
        {
            // arrange
            IRequestExecutor tester =
                await _fixture.GetOrCreateSchema<Foo, FooSortType>(_fooEntitiesCypher);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { bar: ASC}){ bar}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { bar: DESC}){ bar}}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("ASC");
            res2.MatchDocumentSnapshot("DESC");
        }
    }
}
