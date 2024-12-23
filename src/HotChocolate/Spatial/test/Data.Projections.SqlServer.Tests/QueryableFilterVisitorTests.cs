using HotChocolate.Execution;
using NetTopologySuite.Geometries;
using Squadron;

namespace HotChocolate.Data.Projections.Spatial;

public class QueryableProjectionVisitorTests(PostgreSqlResource<PostgisConfig> resource)
    : SchemaCache(resource)
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

    [Fact]
    public async Task Create_Expression()
    {
        // arrange
        var tester = await CreateSchemaAsync(_fooEntities);

        // act
        var result = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    {
                        root {
                            id
                            bar { coordinates }
                        }
                    }
                    """)
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync();
    }

    public class Foo
    {
        public int Id { get; set; }

        public Polygon Bar { get; set; } = null!;
    }
}
