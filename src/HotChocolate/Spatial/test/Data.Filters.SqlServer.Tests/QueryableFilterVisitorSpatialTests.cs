using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using NetTopologySuite.Geometries;
using Squadron;
using Xunit;

namespace HotChocolate.Spatial.Data.Filters
{
    public class QueryableFilterVisitorSpatialTests
        : SchemaCache,
          IClassFixture<PostgreSqlResource<PostgisConfig>>
    {
        private static readonly Polygon _truePolygon = new Polygon(
            new LinearRing(
                new[]
                {
                    new Coordinate(0, 0),
                    new Coordinate(0, 2),
                    new Coordinate(2, 2),
                    new Coordinate(2, 0),
                    new Coordinate(0, 0)
                })
        );

        private static readonly Polygon _falsePolygon = new Polygon(
            new LinearRing(
                new[]
                {
                    new Coordinate(0, 0),
                    new Coordinate(0, -2),
                    new Coordinate(-2, -2),
                    new Coordinate(-2, 0),
                    new Coordinate(0, 0)
                })
        );

        private static readonly Foo[] _fooEntities =
        {
            new Foo { Id = 1, Bar = _truePolygon },
            new Foo { Id = 2, Bar = _falsePolygon }
        };

        public QueryableFilterVisitorSpatialTests(PostgreSqlResource<PostgisConfig> resource)
            : base(resource)
        {
        }

        [Fact]
        public async Task Create_BooleanEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                        root(where: {
                            bar: {
                                contains: {
                                    geometry: {
                                        type: Point,
                                        coordinates: [1, 1]
                                    },
                                    eq: true
                                }
                            }
                        }){
                            id
                        }
                    }")
                    .Create());

            res1.MatchSqlSnapshot("1");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                        root(where: {
                            bar: {
                                contains: {
                                    geometry: {
                                        type: Point,
                                        coordinates: [-1, -1]
                                    },
                                    eq: true
                                }
                            }
                        }){
                            id
                        }
                    }")
                    .Create());

            res2.MatchSqlSnapshot("2");
        }

        [Fact]
        public async Task Create_BooleanNotEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                        root(where: {
                            bar: {
                                contains: {
                                    geometry: {
                                        type: Point,
                                        coordinates: [1, 1]
                                    },
                                    neq: true
                                }
                            }
                        }){
                            id
                        }
                    }")
                    .Create());

            res1.MatchSqlSnapshot("2");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                        root(where: {
                            bar: {
                                contains: {
                                    geometry: {
                                        type: Point,
                                        coordinates: [-1, -1]
                                    },
                                    neq: true
                                }
                            }
                        }){
                            id
                        }
                    }")
                    .Create());

            res2.MatchSqlSnapshot("1");
        }

        [Fact]
        public async Task Create_ComparableGreaterThan_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
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
                    .Create());

            res1.MatchSqlSnapshot("2");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
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
                    .Create());

            res2.MatchSqlSnapshot("1");
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
