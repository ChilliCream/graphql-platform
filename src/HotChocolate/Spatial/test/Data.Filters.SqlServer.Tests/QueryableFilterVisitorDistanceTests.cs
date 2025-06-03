using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using NetTopologySuite.Geometries;
using Squadron;

namespace HotChocolate.Data.Spatial.Filters;

[Collection("Postgres")]
public class QueryableFilterVisitorDistanceTests
    : SchemaCache
{
    private static readonly Polygon s_truePolygon = new(
        new LinearRing(
        [
            new Coordinate(0, 0),
            new Coordinate(0, 2),
            new Coordinate(2, 2),
            new Coordinate(2, 0),
            new Coordinate(0, 0)
        ]));

    private static readonly Polygon s_falsePolygon = new(
        new LinearRing(
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

    public QueryableFilterVisitorDistanceTests(PostgreSqlResource<PostgisConfig> resource)
        : base(resource)
    {
    }

    [Fact]
    public async Task Create_Distance_Expression()
    {
        // arrange
        var tester = await CreateSchemaAsync<Foo, FooFilterType>(s_fooEntities);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    @"{
                            root(where: {
                                bar: {
                                    distance: {
                                        geometry: {
                                            type: Point,
                                            coordinates: [1, 1]
                                        },
                                        gt: 1
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
                                    distance: {
                                        geometry: {
                                            type: Point,
                                            coordinates: [-1, -1]
                                        },
                                        gt: 1
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
            .AddResult(res1, "2")
            .AddResult(res2, "1")
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
