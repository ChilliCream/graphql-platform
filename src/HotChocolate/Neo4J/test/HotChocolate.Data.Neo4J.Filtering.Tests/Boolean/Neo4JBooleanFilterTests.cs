using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using Xunit;

namespace HotChocolate.Data.Neo4J.Filtering;

[Collection("Database")]
public class Neo4JBooleanFilterTests
{
    private readonly Neo4JFixture _fixture;

    public Neo4JBooleanFilterTests(Neo4JFixture fixture)
    {
        _fixture = fixture;
    }

    private readonly string _fooEntitiesCypher =
        @"CREATE (:FooBool {Bar: true}), (:FooBool {Bar: false})";
    private readonly string _fooEntitiesNullableCypher =
        @"CREATE
                (:FooBoolNullable {Bar: true}),
                (:FooBoolNullable {Bar: false}),
                (:FooBoolNullable {Bar: NULL})";

    public class FooBool
    {
        public bool Bar { get; set; }
    }

    public class FooBoolNullable
    {
        public bool? Bar { get; set; }
    }

    public class FooBoolFilterType
        : FilterInputType<FooBool>
    {
    }

    public class FooBoolNullableFilterType
        : FilterInputType<FooBoolNullable>
    {
    }

    [Fact]
    public async Task Create_BooleanEqual_Expression()
    {
        // arrange
        IRequestExecutor tester =
            await _fixture.GetOrCreateSchema<FooBool, FooBoolFilterType>(_fooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { bar: { eq: true}}){ bar }}";
        IExecutionResult res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { eq: false}}){ bar }}";
        IExecutionResult res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        // assert
        res1.MatchDocumentSnapshot("true");
        res2.MatchDocumentSnapshot("false");
    }

    [Fact]
    public async Task Create_And_BooleanEqual_Expression()
    {
        // arrange
        IRequestExecutor tester =
            await _fixture.GetOrCreateSchema<FooBool, FooBoolFilterType>(_fooEntitiesCypher);

        // act
        const string query1 =
            "{ root(where: {and: [{ bar: { eq: true}}, { bar: { eq: false}}]} ){ bar }}";
        IExecutionResult res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        // assert
        res1.MatchDocumentSnapshot("and");
    }

    [Fact]
    public async Task Create_Or_BooleanEqual_Expression()
    {
        // arrange
        IRequestExecutor tester =
            await _fixture.GetOrCreateSchema<FooBool, FooBoolFilterType>(_fooEntitiesCypher);

        // act
        const string query1 =
            "{ root(where: {or: [{ bar: { eq: true}}, { bar: { eq: false}}]} ){ bar }}";
        IExecutionResult res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        // assert
        res1.MatchDocumentSnapshot("or");
    }

    [Fact]
    public async Task Create_BooleanNotEqual_Expression()
    {
        // arrange
        IRequestExecutor tester =
            await _fixture.GetOrCreateSchema<FooBool, FooBoolFilterType>(_fooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { bar: { neq: true}}){ bar}}";
        IExecutionResult res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { neq: false}}){ bar}}";
        IExecutionResult res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        // assert
        res1.MatchDocumentSnapshot("true");
        res2.MatchDocumentSnapshot("false");
    }

    [Fact]
    public async Task Create_NullableBooleanEqual_Expression()
    {
        // arrange
        IRequestExecutor tester =
            await _fixture.GetOrCreateSchema<FooBoolNullable, FooBoolNullableFilterType>(
                _fooEntitiesNullableCypher);

        // act
        const string query1 = "{ root(where: { bar: { eq: true}}){ bar }}";
        IExecutionResult res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { eq: false}}){ bar }}";
        IExecutionResult res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { bar: { eq: null}}){ bar }}";
        IExecutionResult res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        res1.MatchDocumentSnapshot("true");
        res2.MatchDocumentSnapshot("false");
        res3.MatchDocumentSnapshot("null");
    }

    [Fact]
    public async Task Create_NullableBooleanNotEqual_Expression()
    {
        // arrange
        IRequestExecutor tester =
            await _fixture.GetOrCreateSchema<FooBoolNullable, FooBoolNullableFilterType>(
                _fooEntitiesNullableCypher);

        // act
        const string query1 = "{ root(where: { bar: { neq: true}}){ bar }}";
        IExecutionResult res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { neq: false}}){ bar }}";
        IExecutionResult res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { bar: { neq: null}}){ bar }}";
        IExecutionResult res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        res1.MatchDocumentSnapshot("true");
        res2.MatchDocumentSnapshot("false");
        res3.MatchDocumentSnapshot("null");
    }
}
