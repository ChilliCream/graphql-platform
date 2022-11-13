using CookieCrumble;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Testing;
using HotChocolate.Execution;

namespace HotChocolate.Data.Neo4J.Filtering.Tests;

[Collection(Neo4JDatabaseCollectionFixture.DefinitionName)]
public class Neo4JStringFilterTests : IClassFixture<Neo4JFixture>
{
    private readonly Neo4JDatabase _database;
    private readonly Neo4JFixture _fixture;

    public Neo4JStringFilterTests(Neo4JDatabase database, Neo4JFixture fixture)
    {
        _database = database;
        _fixture = fixture;
    }

    private const string FooEntitiesCypher =
        @"CREATE (:FooString {Bar: 'testatest'}), (:FooString {Bar: 'testbtest'})";

    private const string FooNullableEntitiesCypher =
        @"CREATE
                (:FooStringNullable {Bar: 'testatest'}),
                (:FooStringNullable {Bar: 'testbtest'}),
                (:FooStringNullable {Bar: NULL})";

    [Fact]
    public async Task Create_StringEqual_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooString, FooStringFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { bar: { eq: \"testatest\"}}){ bar }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { eq: \"testbtest\"}}){ bar }}";
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
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(),
                        res1, "testatest"),
                    res2, "testbtest"),
                res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_StringNotEqual_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooString, FooStringFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { bar: { neq: \"testatest\"}}){ bar }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { neq: \"testbtest\"}}){ bar }}";
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
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(),
                        res1, "testatest"),
                    res2, "testbtest"),
                res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_StringStartsWith_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooString, FooStringFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { bar: { startsWith: \"testa\" }}){ bar }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { startsWith: \"testb\" }}){ bar }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { bar: { startsWith: null }}){ bar }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(),
                        res1, "testa"),
                    res2, "testb"),
                res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_StringNotStartsWith_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooString, FooStringFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { bar: { nstartsWith: \"testa\" }}){ bar}}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { nstartsWith: \"testb\" }}){ bar}}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { bar: { nstartsWith: null }}){ bar}}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(),
                        res1, "testa"),
                    res2, "testb"),
                res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_StringIn_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooString, FooStringFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 =
            "{ root(where: { bar: { in: [ \"testatest\"  \"testbtest\" ]}}){ bar }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { in: [\"testbtest\" null]}}){ bar }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { bar: { in: [ \"testatest\" ]}}){ bar }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(),
                        res1, "testatestAndtestb"),
                    res2, "testbtestAndNull"),
                res3, "testatest")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_StringNotIn_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooString, FooStringFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 =
            "{ root(where: { bar: { nin: [ \"testatest\"  \"testbtest\" ]}}){ bar }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { nin: [\"testbtest\" null]}}){ bar }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { bar: { nin: [ \"testatest\" ]}}){ bar }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(),
                        res1, "testatestAndtestb"),
                    res2, "testbtestAndNull"),
                res3, "testatest")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_StringContains_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooString, FooStringFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { bar: { contains: \"a\" }}){ bar}}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { contains: \"b\" }}){ bar}}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { bar: { contains: null }}){ bar}}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(),
                        res1, "a"),
                    res2, "b"),
                res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_StringNotContains_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooString, FooStringFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { bar: { ncontains: \"a\" }}){ bar}}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { ncontains: \"b\" }}){ bar}}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { bar: { ncontains: null }}){ bar}}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(),
                        res1, "a"),
                    res2, "b"),
                res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_StringEndsWith_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooString, FooStringFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { bar: { endsWith: \"atest\" }}){ bar }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { endsWith: \"btest\" }}){ bar }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { bar: { endsWith: null }}){ bar }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(),
                        res1, "a"),
                    res2, "b"),
                res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_StringNotEndsWith_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooString, FooStringFilterType>(_database, FooEntitiesCypher);

        // act
        const string query1 = "{ root(where: { bar: { nendsWith: \"atest\" }}){ bar }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { nendsWith: \"btest\" }}){ bar }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { bar: { nendsWith: null }}){ bar }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(),
                        res1, "atest"),
                    res2, "btest"),
                res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringEqual_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooStringNullable, FooStringNullableFilterType>(
                _database, FooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { bar: { eq: \"testatest\"}}){ bar }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { eq: \"testbtest\"}}){ bar }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { bar: { eq: null}}){ bar}}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(),
                        res1, "testatest"),
                    res2, "testbtest"),
                res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringNotEqual_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooStringNullable, FooStringNullableFilterType>(
                _database, FooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { bar: { neq: \"testatest\"}}){ bar }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { neq: \"testbtest\"}}){ bar }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { bar: { neq: null}}){ bar}}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(),
                        res1, "testatest"),
                    res2, "testbtest"),
                res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringIn_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooStringNullable, FooStringNullableFilterType>(
                _database, FooNullableEntitiesCypher);

        // act
        const string query1 =
            "{ root(where: { bar: { in: [ \"testatest\"  \"testbtest\" ]}}){ bar }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { in: [\"testbtest\" null]}}){ bar }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { bar: { in: [ \"testatest\" ]}}){ bar }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(),
                        res1, "testatestAndtestb"),
                    res2, "testbtestAndNull"),
                res3, "testatest")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringNotIn_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooStringNullable, FooStringNullableFilterType>(
                _database, FooNullableEntitiesCypher);

        // act
        const string query1 =
            "{ root(where: { bar: { nin: [ \"testatest\"  \"testbtest\" ]}}){ bar }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { nin: [\"testbtest\" null]}}){ bar }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { bar: { nin: [ \"testatest\" ]}}){ bar }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(),
                        res1, "testatestAndtestb"),
                    res2, "testbtestAndNull"),
                res3, "testatest")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringContains_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooStringNullable, FooStringNullableFilterType>(
                _database, FooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { bar: { contains: \"a\" }}){ bar}}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { contains: \"b\" }}){ bar}}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { bar: { contains: null }}){ bar}}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(),
                        res1, "a"),
                    res2, "b"),
                res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringNotContains_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooStringNullable, FooStringNullableFilterType>(
                _database, FooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { bar: { ncontains: \"a\" }}){ bar }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { ncontains: \"b\" }}){ bar }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { bar: { ncontains: null }}){ bar }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(),
                        res1, "a"),
                    res2, "b"),
                res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringStartsWith_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooStringNullable, FooStringNullableFilterType>(
                _database, FooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { bar: { startsWith: \"testa\" }}){ bar }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { startsWith: \"testb\" }}){ bar }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { bar: { startsWith: null }}){ bar }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(),
                        res1, "testa"),
                    res2, "testb"),
                res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringNotStartsWith_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooStringNullable, FooStringNullableFilterType>(
                _database, FooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { bar: { nstartsWith: \"testa\" }}){ bar}}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { nstartsWith: \"testb\" }}){ bar}}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { bar: { nstartsWith: null }}){ bar}}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(),
                        res1, "testa"),
                    res2, "testb"),
                res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringEndsWith_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooStringNullable, FooStringNullableFilterType>(
                _database, FooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { bar: { endsWith: \"atest\" }}){ bar }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { endsWith: \"btest\" }}){ bar }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { bar: { endsWith: null }}){ bar }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(),
                        res1, "atest"),
                    res2, "btest"),
                res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringNotEndsWith_Expression()
    {
        // arrange
        var tester =
            await _fixture.Arrange<FooStringNullable, FooStringNullableFilterType>(
                _database, FooNullableEntitiesCypher);

        // act
        const string query1 = "{ root(where: { bar: { nendsWith: \"atest\" }}){ bar }}";
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query1)
                .Create());

        const string query2 = "{ root(where: { bar: { nendsWith: \"btest\" }}){ bar }}";
        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query2)
                .Create());

        const string query3 = "{ root(where: { bar: { nendsWith: null }}){ bar }}";
        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query3)
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(),
                        res1, "atest"),
                    res2, "btest"),
                res3, "null")
            .MatchAsync();
    }

    public class FooString
    {
        public string Bar { get; set; }
    }

    public class FooStringFilterType : FilterInputType<FooString>
    {
    }

    public class FooStringNullable
    {
        public string? Bar { get; set; }
    }

    public class FooStringNullableFilterType : FilterInputType<FooStringNullable>
    {
    }
}
