using System.Threading.Tasks;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using Squadron;
using Xunit;

namespace HotChocolate.Data.Neo4J.Sorting.Boolean
{
    public class Neo4JStringsSortingTests
        : SchemaCache,
        IClassFixture<Neo4jResource>
    {
        private string _fooEntities = @"
            CREATE (:Foo {Bar: 'testatest'}), (:Foo {Bar: 'testbtest'})
        ";

        public Neo4JStringsSortingTests(Neo4jResource resource)
        {
            Init(resource);
        }

        public class Foo
        {
            public string Bar { get; set; }
        }

        public class FooSortType
            : SortInputType<Foo>
        {
        }

        [Fact]
        public async Task Create_String_OrderBy()
        {
            // arrange
            IRequestExecutor tester = await CreateSchema<Foo, FooSortType>(_fooEntities);

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
