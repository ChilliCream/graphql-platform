using System.Threading.Tasks;
using HotChocolate.Execution;
using NetTopologySuite.Geometries;
using Squadron;
using Xunit;

namespace HotChocolate.Data.Filters.Spatial
{
    public class QueryableFilterVisitorWithinTests
        : SchemaCache
        , IClassFixture<PostgreSqlResource<PostgisConfig>>
    {
        private static readonly Polygon _truePolygon =
            new Polygon(new LinearRing(new[]
            {
                new Coordinate(20, 20),
                new Coordinate(20, 100),
                new Coordinate(120, 100),
                new Coordinate(140, 20),
                new Coordinate(20, 20),
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

        public QueryableFilterVisitorWithinTests(PostgreSqlResource<PostgisConfig> resource)
            : base(resource)
        {
        }

        [Fact]
        public async Task Create_Within_Query()
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
                                    within: {
                                        geometry: {
                                            type: Polygon,
                                            coordinates: [
                                                [20 20],
                                                [140 20],
                                                [120 100],
                                                [20 100 ],
                                                [20 20]
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
                                    within: {
                                        geometry: {
                                            type: Polygon,
                                            coordinates: [
                                                [20 20],
                                                [140 20],
                                                [120 100],
                                                [20 100 ],
                                                [20 20]
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
