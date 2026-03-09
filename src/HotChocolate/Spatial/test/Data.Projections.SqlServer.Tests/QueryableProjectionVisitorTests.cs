using HotChocolate.Execution;
using NetTopologySuite.Geometries;
using Squadron;

namespace HotChocolate.Data.Projections.Spatial;

public class QueryableProjectionVisitorTests(PostgreSqlResource<PostgisConfig> resource)
    : SchemaCache(resource)
    , IClassFixture<PostgreSqlResource<PostgisConfig>>
{
    private static readonly Polygon s_truePolygon =
        new(new LinearRing(
        [
            new Coordinate(0, 0),
                new Coordinate(0, 2),
                new Coordinate(2, 2),
                new Coordinate(2, 0),
                new Coordinate(0, 0)
        ]));

    private static readonly Polygon s_falsePolygon =
        new(new LinearRing(
        [
            new Coordinate(0, 0),
                new Coordinate(0, -2),
                new Coordinate(-2, -2),
                new Coordinate(-2, 0),
                new Coordinate(0, 0)
        ]));

    private static readonly Foo[] s_fooEntities =
    [
        new() { Id = 1, Bar = s_truePolygon },
        new() { Id = 2, Bar = s_falsePolygon }
    ];

    [Fact]
    public async Task Create_Expression()
    {
        // arrange
        var tester = await CreateSchemaAsync(s_fooEntities);

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
