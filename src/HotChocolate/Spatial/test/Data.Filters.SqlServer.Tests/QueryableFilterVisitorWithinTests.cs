using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using NetTopologySuite.Geometries;
using Squadron;
using static CookieCrumble.TestEnvironment;

namespace HotChocolate.Data.Spatial.Filters;

[Collection("Postgres")]
public class QueryableFilterVisitorWithinTests : SchemaCache
{
    private static readonly Polygon s_truePolygon =
        new(new LinearRing(
        [
            new Coordinate(20, 20),
            new Coordinate(20, 100),
            new Coordinate(120, 100),
            new Coordinate(140, 20),
            new Coordinate(20, 20)
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

    public QueryableFilterVisitorWithinTests(PostgreSqlResource<PostgisConfig> resource)
        : base(resource)
    {
    }

    [Fact]
    public async Task Create_Within_Query()
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
                                within: {
                                    geometry: {
                                        type: Polygon,
                                        coordinates: [
                                            [
                                                [20 20],
                                                [140 20],
                                                [120 100],
                                                [20 100],
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
                .Build(),
            TestContext.Current.CancellationToken);

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
                                                [20 100],
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
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create(Postfix([NET8_0, NET9_0]))
            .AddResult(res1, "true")
            .AddResult(res2, "false")
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    public class Foo
    {
        public int Id { get; set; }

        public Polygon Bar { get; set; } = null!;
    }

    public class FooFilterType : FilterInputType<Foo>;
}
