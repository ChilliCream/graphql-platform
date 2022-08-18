using System.Threading.Tasks;
using CookieCrumble;
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

    private const string _fooEntitiesCypher =
        @"CREATE (:FooBool {Bar: true}), (:FooBool {Bar: false})";

    private const string _fooEntitiesNullableCypher =
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

    public class FooBoolFilterType : FilterInputType<FooBool>
    {
    }

    public class FooBoolNullableFilterType : FilterInputType<FooBoolNullable>
    {
    }

    [Fact]
    public async Task Create_BooleanEqual_Expression()
    {
        // arrange
        var tester =
            await _fixture.GetOrCreateSchema<FooBool, FooBoolFilterType>(_fooEntitiesCypher);

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
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    Snapshot
                        .Create(), res1, "true"), res2, "false")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_And_BooleanEqual_Expression()
    {
        // arrange
        var tester =
            await _fixture.GetOrCreateSchema<FooBool, FooBoolFilterType>(_fooEntitiesCypher);

        // act
        const string query1 =
            "{ root(where: {and: [{ bar: { eq: true}}, { bar: { eq: false}}]} ){ bar }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                Snapshot
                    .Create(), res1, "and")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_Or_BooleanEqual_Expression()
    {
        // arrange
        var tester =
            await _fixture.GetOrCreateSchema<FooBool, FooBoolFilterType>(_fooEntitiesCypher);

        // act
        const string query1 =
            "{ root(where: {or: [{ bar: { eq: true}}, { bar: { eq: false}}]} ){ bar }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        // assert
        await SnapshotExtensions.Add(
                Snapshot
                    .Create(), res1, "or")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_BooleanNotEqual_Expression()
    {
        // arrange
        var tester =
            await _fixture.GetOrCreateSchema<FooBool, FooBoolFilterType>(_fooEntitiesCypher);

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
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    Snapshot
                        .Create(), res1, "true"), res2, "false")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableBooleanEqual_Expression()
    {
        // arrange
        var tester =
            await _fixture.GetOrCreateSchema<FooBoolNullable, FooBoolNullableFilterType>(
                _fooEntitiesNullableCypher);

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
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        Snapshot
                            .Create(), res1, "true"), res2, "false"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableBooleanNotEqual_Expression()
    {
        // arrange
        var tester =
            await _fixture.GetOrCreateSchema<FooBoolNullable, FooBoolNullableFilterType>(
                _fooEntitiesNullableCypher);

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
        await SnapshotExtensions.Add(
                SnapshotExtensions.Add(
                    SnapshotExtensions.Add(
                        Snapshot
                            .Create(), res1, "true"), res2, "false"), res3, "null")
            .MatchAsync();
    }
}
