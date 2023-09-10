using CookieCrumble;
using HotChocolate.Data.Neo4J.Testing;
using HotChocolate.Execution;

namespace HotChocolate.Data;

[Collection(Neo4JDatabaseCollectionFixture.DefinitionName)]
public class Neo4JScalarProjectionTest : IClassFixture<Neo4JFixture>
{
    private readonly Neo4JDatabase _database;
    private readonly Neo4JFixture _fixture;

    public Neo4JScalarProjectionTest(Neo4JDatabase database, Neo4JFixture fixture)
    {
        _database = database;
        _fixture = fixture;
    }

    private const string FooEntitiesCypher =
        @"CREATE (:FooScalar {Bar: true, Baz: 'a'}), (:FooScalar {Bar: false, Baz: 'b'})";

    [Fact]
    public async Task Scalar_Projection_Two_Properties()
    {
        // arrange
        var tester = await _fixture.Arrange<FooScalar>(_database, FooEntitiesCypher);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ bar baz }}")
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Scalar_Projection_One_Property()
    {
        // arrange
        var tester = await _fixture.Arrange<FooScalar>(_database, FooEntitiesCypher);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ baz }}")
                .Create());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    public class FooScalar
    {
        public bool Bar { get; set; }
        public string Baz { get; set; } = null!;
    }
}
