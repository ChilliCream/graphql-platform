using System.Threading.Tasks;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Neo4J.Sorting.Boolean
{
    public class Neo4JStringsSortingTests
        : IClassFixture<Neo4JFixture>
    {
        private readonly Neo4JFixture _fixture;

        public Neo4JStringsSortingTests(Neo4JFixture fixture)
        {
            _fixture = fixture;
        }

        private string _fooEntitiesCypher = @"
            CREATE (:Foo {Bar: 'testatest'}), (:Foo {Bar: 'testbtest'})
        ";

        public class Foo
        {
            public string Bar { get; set; }
        }

        public class FooSortType
            : SortInputType<Foo>
        {
        }

        [Fact]
        public async Task Sorting_Strings_SchemaSnapshot()
        {
            // arrange
            IRequestExecutor tester =
                await _fixture.GetOrCreateSchema<Foo, FooSortType>(_fooEntitiesCypher);
            tester.Schema.Print().MatchSnapshot();
        }

        [Fact]
        public async Task Create_String_OrderBy()
        {
            // arrange
            IRequestExecutor tester =
                await _fixture.GetOrCreateSchema<Foo, FooSortType>(_fooEntitiesCypher);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { bar: ASC}){ bar }}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { bar: DESC}){ bar }}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("ASC");
            res2.MatchDocumentSnapshot("DESC");
        }
    }
}
