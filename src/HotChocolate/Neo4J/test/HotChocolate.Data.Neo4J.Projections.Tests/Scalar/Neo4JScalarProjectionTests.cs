using CookieCrumble;
using HotChocolate.Execution;

namespace HotChocolate.Data.Neo4J.Projections.Scalar;

[Collection("Database")]
public class Neo4JScalarProjectionTest
{
    private readonly Neo4JFixture _fixture;

    public Neo4JScalarProjectionTest(Neo4JFixture fixture)
    {
        _fixture = fixture;
    }

    private string _fooEntitiesCypher = @"
            CREATE (:FooScalar {Bar: true, Baz: 'a'}), (:FooScalar {Bar: false, Baz: 'b'})
        ";

    public class FooScalar
    {
        public bool Bar { get; set; }
        public string Baz { get; set; } = null!;
    }

    [Fact]
    public async Task Create_ProjectsTwoProperties_Expression()
    {
        // arrange
        var tester = await _fixture.GetOrCreateSchema<FooScalar>(_fooEntitiesCypher);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ bar baz }}")
                .Create());

        // assert
        await SnapshotExtensions.Add(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ProjectsOneProperty_Expression()
    {
        // arrange
        var tester = await _fixture.GetOrCreateSchema<FooScalar>(_fooEntitiesCypher);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root{ baz }}")
                .Create());

        // assert
        await SnapshotExtensions.Add(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }
}
