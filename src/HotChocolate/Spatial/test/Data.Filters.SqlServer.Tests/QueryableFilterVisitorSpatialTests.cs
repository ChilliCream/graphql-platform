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
            new Foo { Id = 1, Bar = _truePolygon }, new Foo { Id = 0, Bar = _falsePolygon }
        };

        private static readonly FooNullable[] _fooNullableEntities =
        {
            new FooNullable { Bar = _truePolygon },
            new FooNullable { Bar = null },
            new FooNullable { Bar = _falsePolygon }
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
                                    eq: false
                                }
                            }
                        }){
                            id
                        }
                    }")
                    .Create());

            res2.MatchSqlSnapshot("0");
        }

        // [Fact]
        public async Task Create_BooleanNotEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: true}}){ bar}}")
                    .Create());

            res1.MatchSqlSnapshot("true");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: false}}){ bar}}")
                    .Create());

            res2.MatchSqlSnapshot("false");
        }

        // [Fact]
        public async Task Create_NullableBooleanEqual_Expression()
        {
            // arrange
            IRequestExecutor? tester = CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: true}}){ bar}}")
                    .Create());

            res1.MatchSqlSnapshot("true");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: false}}){ bar}}")
                    .Create());

            res2.MatchSqlSnapshot("false");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: null}}){ bar}}")
                    .Create());

            res3.MatchSqlSnapshot("null");
        }

        // [Fact]
        public async Task Create_NullableBooleanNotEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

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
                                        coordinates: [-109.31324, 37.87099]
                                        crs: 4326
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
                    .SetQuery("{ root(where: { bar: { neq: false}}){ bar}}")
                    .Create());

            res2.MatchSqlSnapshot("false");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: null}}){ bar}}")
                    .Create());

            res3.MatchSqlSnapshot("null");
        }

        public class Foo
        {
            public int Id { get; set; }

            public Polygon Bar { get; set; } = null!;
        }

        public class FooNullable
        {
            public int Id { get; set; }

            public Polygon? Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
        }

        public class FooNullableFilterType
            : FilterInputType<FooNullable>
        {
        }
    }
}
