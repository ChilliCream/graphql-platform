using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using NetTopologySuite.Geometries;
using Squadron;

namespace HotChocolate.Data.Spatial.Filters;

[Collection("Postgres")]
public class QueryableFilterVisitorIntersectsTests
    : SchemaCache
{
    private static readonly Polygon s_truePolygon =
        new(new LinearRing(
        [
            new Coordinate(0, 0),
            new Coordinate(100, 0),
            new Coordinate(100, 100),
            new Coordinate(0, 100),
            new Coordinate(0, 0)
        ]));

    private static readonly Polygon s_falsePolygon =
        new(new LinearRing(
        [
            new Coordinate(1000, 1000),
            new Coordinate(100000, 1000),
            new Coordinate(100000, 100000),
            new Coordinate(1000, 100000),
            new Coordinate(1000, 1000)
        ]));

    private static readonly Foo[] s_fooEntities =
    [
        new() { Id = 1, Bar = s_truePolygon },
        new() { Id = 2, Bar = s_falsePolygon }
    ];

    public QueryableFilterVisitorIntersectsTests(PostgreSqlResource<PostgisConfig> resource)
        : base(resource)
    {
    }

    [Fact]
    public async Task Create_Intersects_Query()
    {
        // arrange
        var tester = await CreateSchemaAsync<Foo, FooFilterType>(s_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    @"{
                        root(where: {
                            bar: {
                                intersects: {
                                    geometry: {
                                        type: Polygon,
                                        coordinates: [
                                            [
                                                [10 10],
                                                [10 90],
                                                [90 90],
                                                [90 10],
                                                [10 10]
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
            OperationRequestBuilder.New()
                .SetDocument(
                    @"{
                        root(where: {
                            bar: {
                                nintersects: {
                                    geometry: {
                                        type: Polygon,
                                        coordinates: [
                                            [
                                                [10 10],
                                                [10 90],
                                                [90 90],
                                                [90 10],
                                                [10 10]
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
        await Snapshot
            .Create(
                postFix: TestEnvironment.TargetFramework == "NET10_0"
                    ? TestEnvironment.TargetFramework
                    : null)
            .AddResult(res1, "true")
            .AddResult(res2, "false")
            .MatchAsync();
    }

    public class Foo
    {
        public int Id { get; set; }

        public Polygon Bar { get; set; } = null!;
    }

    public class FooFilterType : FilterInputType<Foo>;
}
