using CookieCrumble;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using NetTopologySuite.Geometries;
using Squadron;

namespace HotChocolate.Data.Spatial.Filters;

[Collection("Postgres")]
public class QueryableFilterVisitorTouchesTests : SchemaCache
{
    private static readonly Polygon _truePolygon =
        new(new LinearRing(
        [
            new Coordinate(140, 120),
            new Coordinate(160, 20),
            new Coordinate(20, 20),
            new Coordinate(20, 120),
            new Coordinate(140, 120),
        ]));

    private static readonly Polygon _falsePolygon =
        new(new LinearRing(
        [
            new Coordinate(1000, 1000),
            new Coordinate(100000, 1000),
            new Coordinate(100000, 100000),
            new Coordinate(1000, 100000),
            new Coordinate(1000, 1000),
        ]));

    private static readonly Foo[] _fooEntities =
    [
        new() { Id = 1, Bar = _truePolygon, },
        new() { Id = 2, Bar = _falsePolygon, },
    ];

    public QueryableFilterVisitorTouchesTests(PostgreSqlResource<PostgisConfig> resource)
        : base(resource)
    {
    }

    [Fact]
    public async Task Create_Touches_Query()
    {
        // arrange
        var tester = await CreateSchemaAsync<Foo, FooFilterType>(_fooEntities);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"{
                        root(where: {
                            bar: {
                                touches: {
                                    geometry: {
                                        type: Polygon,
                                        coordinates: [
                                            [
                                                [240 80],
                                                [140 120],
                                                [180 240],
                                                [280 200],
                                                [240 80]
                                            ]
                                        ]
                                    }
                                }
                            }
                        }){
                            id
                        }
                    }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"{
                        root(where: {
                            bar: {
                                ntouches: {
                                    geometry: {
                                        type: Polygon,
                                        coordinates: [
                                            [
                                                [240 80],
                                                [140 120],
                                                [180 240],
                                                [280 200],
                                                [240 80]
                                            ]
                                        ]
                                    }
                                }
                            }
                        }){
                            id
                        }
                    }")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "true"), res2, "false")
            .MatchAsync();
    }

    public class Foo
    {
        public int Id { get; set; }

        public Polygon Bar { get; set; } = null!;
    }

    public class FooFilterType : FilterInputType<Foo>
    {
    }
}
