using CookieCrumble;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Testing;
using HotChocolate.Execution;

namespace HotChocolate.Data.Neo4J.Filtering;

[Collection(Neo4JDatabaseCollectionFixture.DefinitionName)]
public class Neo4JBooleanFilterTests : IClassFixture<Neo4JFixture>
{
    private readonly Neo4JDatabase _database;
    private readonly Neo4JFixture _fixture;

    public Neo4JBooleanFilterTests(Neo4JDatabase database, Neo4JFixture fixture)
    {
        _database = database;
        _fixture = fixture;
    }

    private const string FooEntitiesCypher =
        @"CREATE (:FooBool {Bar: true}), (:FooBool {Bar: false})";

    private const string FooEntitiesNullableCypher =
        @"CREATE
            (:FooBoolNullable {Bar: true}),
            (:FooBoolNullable {Bar: false}),
            (:FooBoolNullable {Bar: NULL})";

    [Fact]
    public async Task BooleanFilter_SchemaSnapshot()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooBool, FooBoolFilterType>(_database, FooEntitiesCypher);

        tester.Schema.MatchSnapshot();
    }

    [Fact]
    public async Task Nullable_BooleanFilter_SchemaSnapshot()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooBoolNullable, FooBoolNullableFilterType>(_database, FooEntitiesCypher);

        tester.Schema.MatchSnapshot();
    }

    [Fact]
    public async Task BooleanFilter_Equal()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooBool, FooBoolFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { bar: { eq: true}}){ bar }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { eq: false}}){ bar }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        // assert
        await Snapshot.Create()
            .AddResult(res1, "BooleanFilter_Equal_True")
            .AddResult(res2, "BooleanFilter_Equal_False")
            .MatchAsync();
    }

    [Fact]
    public async Task BooleanFilter_Equal_And_Combinator()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooBool, FooBoolFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 =
            "{ root(where: {and: [{ bar: { eq: true}}, { bar: { eq: false}}]} ){ bar }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "BooleanFilter_Equal_And_Combinator")
            .MatchAsync();
    }

    [Fact]
    public async Task BooleanFilter_Equal_Or_Combinator()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooBool, FooBoolFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 =
            "{ root(where: {or: [{ bar: { eq: true}}, { bar: { eq: false}}]} ){ bar }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "BooleanFilter_Equal_Or_Combinator")
            .MatchAsync();
    }

    [Fact]
    public async Task BooleanFilter_NotEqual()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooBool, FooBoolFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { bar: { neq: true}}){ bar}}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { neq: false}}){ bar}}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "BooleanFilter_NotEqual_True")
            .AddResult(res2, "BooleanFilter_NotEqual_False")
            .MatchAsync();
    }

    [Fact]
    public async Task Nullable_BooleanFilter_Equal()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooBoolNullable, FooBoolNullableFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { bar: { eq: true}}){ bar }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { eq: false}}){ bar }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { bar: { eq: null}}){ bar }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "Nullable_BooleanFilter_Equal_True")
            .AddResult(res2, "Nullable_BooleanFilter_Equal_False")
            .AddResult(res3, "Nullable_BooleanFilter_Equal_Null")
            .MatchAsync();
    }

    [Fact]
    public async Task Nullable_BooleanFilter_NotEqual()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooBoolNullable, FooBoolNullableFilterType>(
                _database,
                FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { bar: { neq: true}}){ bar }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { neq: false}}){ bar }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { bar: { neq: null}}){ bar }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "Nullable_BooleanFilter_NotEqual_True")
            .AddResult(res2, "Nullable_BooleanFilter_NotEqual_False")
            .AddResult(res3, "Nullable_BooleanFilter_NotEqual_null")
            .MatchAsync();
    }

    public class FooBool
    {
        public bool Bar { get; set; }
    }

    public class FooBoolNullable
    {
        public bool? Bar { get; set; }
    }

    public class FooBoolFilterType : FilterInputType<FooBool>
    {
    }

    public class FooBoolNullableFilterType : FilterInputType<FooBoolNullable>
    {
    }
}
