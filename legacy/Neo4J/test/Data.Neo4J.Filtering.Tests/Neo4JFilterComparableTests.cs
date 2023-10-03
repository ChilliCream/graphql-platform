using CookieCrumble;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Testing;
using HotChocolate.Execution;
using HotChocolate.Types;

namespace HotChocolate.Data.Neo4J.Filtering;

[Collection(Neo4JDatabaseCollectionFixture.DefinitionName)]
public class Neo4JFilterComparableTests : IClassFixture<Neo4JFixture>
{
    private readonly Neo4JDatabase _database;
    private readonly Neo4JFixture _fixture;

    public Neo4JFilterComparableTests(Neo4JDatabase database, Neo4JFixture fixture)
    {
        _database = database;
        _fixture = fixture;
    }

    private const string FooEntitiesCypher =
        "CREATE (:FooComp {BarShort: 12}), (:FooComp {BarShort: 14}), (:FooComp {BarShort: 13})";

    private const string FooNullableEntitiesCypher =
        @"CREATE
            (:FooCompNullable {BarShort: 12}),
            (:FooCompNullable {BarShort: NULL}),
            (:FooCompNullable {BarShort: 14}),
            (:FooCompNullable {BarShort: 13})";

    [Fact]
    public async Task Short_Equal()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooComp, FooCompFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { eq: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { eq: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { eq: null}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "Short_Equal_12")
            .AddResult(res2, "Short_Equal_13")
            .AddResult(res3, "Short_Equal_null")
            .MatchAsync();
    }

    [Fact]
    public async Task Short_NotEqual()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooComp, FooCompFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { neq: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { neq: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { neq: null}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "Short_NotEqual_12")
            .AddResult(res2, "Short_NotEqual_13")
            .AddResult(res3, "Short_NotEqual_null")
            .MatchAsync();
    }

    [Fact]
    public async Task Short_GreaterThan()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooComp, FooCompFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { gt: 12}}){ barShort}}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { gt: 13}}){ barShort}}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { gt: 14}}){ barShort}}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { gt: null}}){ barShort}}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "Short_GreaterThan_12")
            .AddResult(res2, "Short_GreaterThan_13")
            .AddResult(res3, "Short_GreaterThan_14")
            .AddResult(res4, "Short_GreaterThan_null")
            .MatchAsync();
    }

    [Fact]
    public async Task Short_NotGreaterThan()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooComp, FooCompFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { ngt: 12}}){ barShort}}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { ngt: 13}}){ barShort}}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { ngt: 14}}){ barShort}}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { ngt: null}}){ barShort}}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "Short_NotGreaterThan_12")
            .AddResult(res2, "Short_NotGreaterThan_13")
            .AddResult(res3, "Short_NotGreaterThan_14")
            .AddResult(res4, "Short_NotGreaterThan_null")
            .MatchAsync();
    }

    [Fact]
    public async Task Short_GreaterThanOrEquals()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooComp, FooCompFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { gte: 12}}){ barShort}}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { gte: 13}}){ barShort}}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { gte: 14}}){ barShort}}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { gte: null}}){ barShort}}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "Short_GreaterThanOrEquals_12")
            .AddResult(res2, "Short_GreaterThanOrEquals_13")
            .AddResult(res3, "Short_GreaterThanOrEquals_14")
            .AddResult(res4, "Short_GreaterThanOrEquals_null")
            .MatchAsync();
    }

    [Fact]
    public async Task Short_NotGreaterThanOrEquals()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooComp, FooCompFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { ngte: 12}}){ barShort}}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { ngte: 13}}){ barShort}}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { ngte: 14}}){ barShort}}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { ngte: null}}){ barShort}}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "Short_NotGreaterThanOrEquals_12")
            .AddResult(res2, "Short_NotGreaterThanOrEquals_13")
            .AddResult(res3, "Short_NotGreaterThanOrEquals_14")
            .AddResult(res4, "Short_NotGreaterThanOrEquals_null")
            .MatchAsync();
    }

    [Fact]
    public async Task Short_LowerThan()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooComp, FooCompFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { lt: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { lt: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { lt: 14}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { lt: null}}){ barShort }}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "Short_LowerThan_12")
            .AddResult(res2, "Short_LowerThan_13")
            .AddResult(res3, "Short_LowerThan_14")
            .AddResult(res4, "Short_LowerThan_null")
            .MatchAsync();
    }

    [Fact]
    public async Task Short_NotLowerThan()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooComp, FooCompFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { nlt: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { nlt: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { nlt: 14}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { nlt: null}}){ barShort }}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "Short_NotLowerThan_12")
            .AddResult(res2, "Short_NotLowerThan_13")
            .AddResult(res3, "Short_NotLowerThan_14")
            .AddResult(res4, "Short_NotLowerThan_null")
            .MatchAsync();
    }

    [Fact]
    public async Task Short_LowerThanOrEquals()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooComp, FooCompFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { lte: 12}}){ barShort}}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { lte: 13}}){ barShort}}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { lte: 14}}){ barShort}}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { lte: null}}){ barShort}}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "Short_LowerThanOrEquals_12")
            .AddResult(res2, "Short_LowerThanOrEquals_13")
            .AddResult(res3, "Short_LowerThanOrEquals_14")
            .AddResult(res4, "Short_LowerThanOrEquals_null")
            .MatchAsync();
    }

    [Fact]
    public async Task Short_NotLowerThanOrEquals()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooComp, FooCompFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { nlte: 12}}){ barShort}}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { nlte: 13}}){ barShort}}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { nlte: 14}}){ barShort}}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { nlte: null}}){ barShort}}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "Short_NotLowerThanOrEquals_12")
            .AddResult(res2, "Short_NotLowerThanOrEquals_13")
            .AddResult(res3, "Short_NotLowerThanOrEquals_14")
            .AddResult(res4, "Short_NotLowerThanOrEquals_null")
            .MatchAsync();
    }

    [Fact]
    public async Task Short_In()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooComp, FooCompFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { in: [ 12, 13 ]}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { in: [ 13, 14 ]}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { in: [ null, 14 ]}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "Short_In_12and13")
            .AddResult(res2, "Short_In_13and14")
            .AddResult(res3, "Short_In_Nulland14")
            .MatchAsync();
    }

    [Fact]
    public async Task Short_NotIn()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooComp, FooCompFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { nin: [ 12, 13 ]}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { nin: [ 13, 14 ]}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { nin: [ null, 14 ]}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "Short_NotIn_12and13")
            .AddResult(res2, "Short_NotIn_13and14")
            .AddResult(res3, "Short_NotIn_NullAnd14")
            .MatchAsync();
    }

    [Fact]
    public async Task Nullable_Short_Equal()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooCompNullable, FooCompNullableFilterType>(_database, FooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { eq: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { eq: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { eq: null}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "Nullable_Short_Equal_12")
            .AddResult(res2, "Nullable_Short_Equal_13")
            .AddResult(res3, "Nullable_Short_Equal_null")
            .MatchAsync();
    }

    [Fact]
    public async Task Nullable_Short_NotEqual()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooCompNullable, FooCompNullableFilterType>(_database, FooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { neq: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { neq: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { neq: null}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "Nullable_Short_NotEqual_12")
            .AddResult(res2, "Nullable_Short_NotEqual_13")
            .AddResult(res3, "Nullable_Short_NotEqual_null")
            .MatchAsync();
    }

    [Fact]
    public async Task Nullable_Short_GreaterThan()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooCompNullable, FooCompNullableFilterType>(_database, FooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { gt: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { gt: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { gt: 14}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { gt: null}}){ barShort }}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "Nullable_Short_GreaterThan_12")
            .AddResult(res2, "Nullable_Short_GreaterThan_13")
            .AddResult(res3, "Nullable_Short_GreaterThan_14")
            .AddResult(res4, "Nullable_Short_GreaterThan_null")
            .MatchAsync();
    }

    [Fact]
    public async Task Nullable_Short_NotGreaterThan()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooCompNullable, FooCompNullableFilterType>(_database, FooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { ngt: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { ngt: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { ngt: 14}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { ngt: null}}){ barShort }}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "Nullable_Short_NotGreaterThan_12")
            .AddResult(res2, "Nullable_Short_NotGreaterThan_13")
            .AddResult(res3, "Nullable_Short_NotGreaterThan_14")
            .AddResult(res4, "Nullable_Short_NotGreaterThan_null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableGreaterThanOrEquals_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooCompNullable, FooCompNullableFilterType>(
                _database, FooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { gte: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { gte: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { gte: 14}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { gte: null}}){ barShort }}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "14")
            .AddResult(res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotGreaterThanOrEquals_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooCompNullable, FooCompNullableFilterType>(
                _database, FooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { ngte: 12}}){ barShort}}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { ngte: 13}}){ barShort}}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { ngte: 14}}){ barShort}}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { ngte: null}}){ barShort}}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "14")
            .AddResult(res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableLowerThan_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooCompNullable, FooCompNullableFilterType>(
                _database, FooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { lt: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { lt: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { lt: 14}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { lt: null}}){ barShort }}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "14")
            .AddResult(res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotLowerThan_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooCompNullable, FooCompNullableFilterType>(
                _database, FooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { nlt: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { nlt: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { nlt: 14}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { nlt: null}}){ barShort }}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "14")
            .AddResult(res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableLowerThanOrEquals_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooCompNullable, FooCompNullableFilterType>(
                _database, FooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { lte: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { lte: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { lte: 14}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { lte: null}}){ barShort }}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "14")
            .AddResult(res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotLowerThanOrEquals_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooCompNullable, FooCompNullableFilterType>(
                _database, FooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { nlte: 12}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { nlte: 13}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { nlte: 14}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        const string query4 = "{ root(where: { barShort: { nlte: null}}){ barShort }}";
        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query4)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12")
            .AddResult(res2, "13")
            .AddResult(res3, "14")
            .AddResult(res4, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableIn_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooCompNullable, FooCompNullableFilterType>(
                _database, FooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { barShort: { in: [ 12, 13 ]}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { in: [ 13, 14 ]}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { in: [ 13, null ]}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12and13")
            .AddResult(res2, "13and14")
            .AddResult(res3, "13andNull")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ShortNullableNotIn_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooCompNullable, FooCompNullableFilterType>(
                _database, FooNullableEntitiesCypher);

        // arrange
        const string query1 = "{ root(where: { barShort: { nin: [ 12, 13 ]}}){ barShort }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { barShort: { nin: [ 13, 14 ]}}){ barShort }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { barShort: { nin: [ 13, null ]}}){ barShort }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "12and13")
            .AddResult(res2, "13and14")
            .AddResult(res3, "13andNull")
            .MatchAsync();
    }

    [Fact]
    public void Create_Implicit_Operation()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                t => t
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("foo")
                    .Argument("test", a => a.Type<FilterInputType<FooComp>>()))
            .AddNeo4JFiltering(compatabilityMode: true)
            .Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void Create_Implicit_Operation_Normalized()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                t => t
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("foo")
                    .Argument("test", a => a.Type<FilterInputType<FooComp>>()))
            .AddNeo4JFiltering()
            .Create();

        // assert
        schema.MatchSnapshot();
    }

    public class FooComp
    {
        public short BarShort { get; set; }

        public int BarInt { get; set; }

        public long BarLong { get; set; }

        public float BarFloat { get; set; }

        public double BarDouble { get; set; }

        public decimal BarDecimal { get; set; }

        public DateTime BarDateTime { get; set; }
    }

    public class FooCompNullable
    {
        public short? BarShort { get; set; }

        public DateTime? BarDateTime { get; set; }
    }

    public class FooCompFilterType : FilterInputType<FooComp>
    {
    }

    public class FooCompNullableFilterType : FilterInputType<FooCompNullable>
    {
    }
}
