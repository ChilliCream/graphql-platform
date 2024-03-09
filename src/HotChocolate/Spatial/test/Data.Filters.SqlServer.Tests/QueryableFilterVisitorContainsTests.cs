using CookieCrumble;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using NetTopologySuite.Geometries;
using Squadron;

namespace HotChocolate.Data.Spatial.Filters;

[Collection("Postgres")]
public class QueryableFilterVisitorContainsTests : SchemaCache
{
    private static readonly Polygon _truePolygon = new(
        new LinearRing(
        [
            new Coordinate(0, 0),
            new Coordinate(0, 2),
            new Coordinate(2, 2),
            new Coordinate(2, 0),
            new Coordinate(0, 0),
        ]));

    private static readonly Polygon _falsePolygon = new(
        new LinearRing(
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

    public QueryableFilterVisitorContainsTests(PostgreSqlResource<PostgisConfig> resource)
        : base(resource)
    {
    }

    [Fact]
    public async Task Create_Contains_Expression()
    {
        // arrange
        var tester = await CreateSchemaAsync<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"{
                            root(where: {
                                bar: {
                                    contains: {
                                        geometry: {
                                            type: Point,
                                            coordinates: [1, 1]
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
                                contains: {
                                    geometry: {
                                        type: Point,
                                        coordinates: [-1, -1]
                                    }
                                }
                            }}){
                                id
                            }
                        }")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "1"), res2, "2")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NotContains_Expression()
    {
        // arrange
        var tester = await CreateSchemaAsync<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"{
                        root(where: {
                            bar: {
                                ncontains: {
                                    geometry: {
                                        type: Point,
                                        coordinates: [1, 1]
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
                                ncontains: {
                                    geometry: {
                                        type: Point,
                                        coordinates: [-1, -1]
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
                        .Create(), res1, "2"), res2, "1")
            .MatchAsync();
    }

    public class Foo
    {
        public int Id { get; set; }

        public Polygon Bar { get; set; } = null!;
    }

    public class FooFilterType
        : FilterInputType<Foo>
    {
    }
}
