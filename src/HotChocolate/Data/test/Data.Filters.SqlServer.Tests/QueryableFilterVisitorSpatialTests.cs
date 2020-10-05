using System.Threading.Tasks;
using HotChocolate.Execution;
using NetTopologySuite.Geometries;
using Xunit;

namespace HotChocolate.Data.Filters
{
    public class QueryableFilterVisitorSpatialTests
    {
        private static readonly Polygon TruePolygon = new Polygon(
            new LinearRing(new[] {
                new Coordinate(0, 0),
                new Coordinate(0, 2),
                new Coordinate(2, 2),
                new Coordinate(2, 0),
                new Coordinate(0, 0)
            })
        );

        private static readonly Polygon FalsePolygon = new Polygon(
            new LinearRing(new[] {
                new Coordinate(0, 0),
                new Coordinate(0, -2),
                new Coordinate(-2, -2),
                new Coordinate(-2, 0),
                new Coordinate(0, 0)
            })
        );

        private static readonly Foo[] _fooEntities =
        {
            new Foo { Id = 1, Bar = TruePolygon },
            new Foo { Id = 0, Bar = FalsePolygon }
        };

        private static readonly FooNullable[] _fooNullableEntities =
        {
            new FooNullable { Bar = TruePolygon },
            new FooNullable { Bar = null },
            new FooNullable { Bar = FalsePolygon }
        };

        private readonly SchemaCache _cache = new SchemaCache();

        [Fact]
        public async Task Create_BooleanEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(@"{
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
                    .SetQuery(@"{
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
            IRequestExecutor tester = _cache.CreateSchema<Foo, FooFilterType>(_fooEntities);

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
            IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterType>(
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
            IRequestExecutor tester = _cache.CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(@"{
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

            public Geometry Bar { get; set; } = null!;
        }

        public class FooNullable
        {
            public int Id { get; set; }

            public Geometry? Bar { get; set; }
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
