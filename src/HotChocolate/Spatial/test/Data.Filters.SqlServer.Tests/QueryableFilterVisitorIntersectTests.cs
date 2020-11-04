using System.Threading.Tasks;
using HotChocolate.Execution;
using NetTopologySuite.Geometries;
using Squadron;
using Xunit;

namespace HotChocolate.Data.Filters.Spatial
{
    public class QueryableFilterVisitorIntersectsTests
        : SchemaCache
        , IClassFixture<PostgreSqlResource<PostgisConfig>>
    {
        private static readonly Polygon _truePolygon = 
            new Polygon(new LinearRing(new[]
            {
                new Coordinate(0, 0),
                new Coordinate(100, 0),
                new Coordinate(100, 100),
                new Coordinate(0, 100),
                new Coordinate(0, 0),
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

        public QueryableFilterVisitorIntersectsTests(PostgreSqlResource<PostgisConfig> resource)
            : base(resource)
        {
        }

        [Fact]
        public async Task Create_Intersects_Query()
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
                                    intersects: {
                                        geometry: {
                                            type: Polygon,
                                            coordinates: [[10 10], [10 90], [90 90], [90 10], [10 10]]
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
                                    intersects: {
                                        geometry: {
                                            type: Polygon,
                                            coordinates: [[10 10], [10 90], [90 90], [90 10], [10 10]]
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
