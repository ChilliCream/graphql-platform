using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using NetTopologySuite.Geometries;
using Squadron;

namespace HotChocolate.Data.Spatial.Filters;

[Collection("Postgres")]
public class QueryableFilterVisitorWithinTests : SchemaCache
{
    private static readonly Polygon _truePolygon =
        new(new LinearRing(
        [
            new Coordinate(20, 20),
            new Coordinate(20, 100),
            new Coordinate(120, 100),
            new Coordinate(140, 20),
            new Coordinate(20, 20),
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

    public QueryableFilterVisitorWithinTests(PostgreSqlResource<PostgisConfig> resource)
        : base(resource)
    {
    }

    [Fact]
    public async Task Create_Within_Query()
    {
        // arrange
        var tester = await CreateSchemaAsync<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    @"{
                        root(where: {
                            bar: {
                                within: {
                                    geometry: {
                                        type: Polygon,
                                        coordinates: [
                                            [
                                                [20 20],
                                                [140 20],
                                                [120 100],
                                                [20 100 ],
                                                [20 20]
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
                                nwithin: {
                                    geometry: {
                                        type: Polygon,
                                        coordinates: [
                                            [
                                                [20 20],
                                                [140 20],
                                                [120 100],
                                                [20 100 ],
                                                [20 20]
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
            .Create()
            .AddResult(res1, "true")
            .AddResult(res2, "false")
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
