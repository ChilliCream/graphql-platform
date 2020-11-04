using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using NetTopologySuite.Geometries;
using Squadron;
using Xunit;

namespace HotChocolate.Spatial.Data.Filters
{
    public class QueryableFilterVisitorTouchesTests
        : SchemaCache
        , IClassFixture<PostgreSqlResource<PostgisConfig>>
    {
        private static readonly Polygon _truePolygon =
            new Polygon(new LinearRing(new[]
            {
                new Coordinate(140, 120),
                new Coordinate(160, 20),
                new Coordinate(20, 20),
                new Coordinate(20, 120),
                new Coordinate(140, 120)
            }));

        private static readonly Polygon _falsePolygon =
            new Polygon(new LinearRing(new[]
            {
                new Coordinate(1000, 1000),
                new Coordinate(100000, 1000),
                new Coordinate(100000, 100000),
                new Coordinate(1000, 100000),
                new Coordinate(1000, 1000),
            }));

        private static readonly Foo[] _fooEntities =
        {
            new Foo { Id = 1, Bar = _truePolygon },
            new Foo { Id = 2, Bar = _falsePolygon }
        };

        public QueryableFilterVisitorTouchesTests(PostgreSqlResource<PostgisConfig> resource)
            : base(resource)
        {
        }

        [Fact]
        public async Task Create_Touches_Query()
        {
            // arrange
            IRequestExecutor tester = await CreateSchemaAsync<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            root(where: {
                                bar: {
                                    touches: {
                                        geometry: {
                                            type: Polygon,
                                            coordinates: [
                                                [240 80],
                                                [140 120],
                                                [180 240],
                                                [280 200],
                                                [240 80]
                                            ]
                                        },
                                        eq: true
                                    }
                                }
                            }){
                                id
                            }
                        }")
                    .Create());

            res1.MatchSqlSnapshot("true");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            root(where: {
                                bar: {
                                    touches: {
                                        geometry: {
                                            type: Polygon,
                                            coordinates: [
                                                [240 80],
                                                [140 120],
                                                [180 240],
                                                [280 200],
                                                [240 80]
                                            ]
                                        },
                                        eq: false
                                    }
                                }
                            }){
                                id
                            }
                        }")
                    .Create());

            res2.MatchSqlSnapshot("false");
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
}
