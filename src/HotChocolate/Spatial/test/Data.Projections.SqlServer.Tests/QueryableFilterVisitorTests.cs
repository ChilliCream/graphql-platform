using CookieCrumble;
using HotChocolate.Execution;
using NetTopologySuite.Geometries;
using Squadron;

namespace HotChocolate.Data.Projections.Spatial;

public class QueryableProjectionVisitorTests
    : SchemaCache
    , IClassFixture<PostgreSqlResource<PostgisConfig>>
{
    private static readonly Polygon _truePolygon =
        new(new LinearRing(
        [
            new Coordinate(0, 0),
                new Coordinate(0, 2),
                new Coordinate(2, 2),
                new Coordinate(2, 0),
                new Coordinate(0, 0),
        ]));

    private static readonly Polygon _falsePolygon =
        new(new LinearRing(
        [
            new Coordinate(0, 0),
                new Coordinate(0, -2),
                new Coordinate(-2, -2),
                new Coordinate(-2, 0),
                new Coordinate(0, 0),
        ]));

    private static readonly Foo[] _fooEntities =
    [
        new() { Id = 1, Bar = _truePolygon, },
        new() { Id = 2, Bar = _falsePolygon, },
    ];

    public QueryableProjectionVisitorTests(PostgreSqlResource<PostgisConfig> resource)
        : base(resource)
    {
    }

    [Fact]

    public async Task Create_Expression()
    {
        // arrange
        var tester = await CreateSchemaAsync(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"{
                        root {
                            id
                            bar { coordinates }
                        }
                    }")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                Snapshot
                    .Create(), res1)
            .MatchAsync();
    }

    public class Foo
    {
        public int Id { get; set; }

        public Polygon Bar { get; set; } = null!;
    }
}
