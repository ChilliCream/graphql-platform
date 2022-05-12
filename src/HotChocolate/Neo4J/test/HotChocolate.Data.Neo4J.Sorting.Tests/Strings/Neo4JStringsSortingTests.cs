using System.Threading.Tasks;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Neo4J.Sorting.Boolean;

[Collection("Database")]
public class Neo4JStringsSortingTests
{
    private readonly Neo4JFixture _fixture;

    public Neo4JStringsSortingTests(Neo4JFixture fixture)
    {
        _fixture = fixture;
    }

    private string _fooEntitiesCypher = @"
            CREATE (:FooString {Bar: 'testatest'}), (:FooString {Bar: 'testbtest'})
        ";

    public class FooString
    {
        public string Bar { get; set; }
    }

    public class FooStringSortType
        : SortInputType<FooString>
    {
    }

    [Fact]
    public async Task Sorting_Strings_SchemaSnapshot()
    {
        // arrange
        IRequestExecutor tester =
            await _fixture.GetOrCreateSchema<FooString, FooStringSortType>(_fooEntitiesCypher);
        tester.Schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task Create_String_OrderBy()
    {
        // arrange
        IRequestExecutor tester =
            await _fixture.GetOrCreateSchema<FooString, FooStringSortType>(_fooEntitiesCypher);

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
