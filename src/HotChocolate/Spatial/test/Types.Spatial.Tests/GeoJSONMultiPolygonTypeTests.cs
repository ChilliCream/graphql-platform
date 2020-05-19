using System.Threading.Tasks;
using HotChocolate.Execution;
using NetTopologySuite.Geometries;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Spatial.Tests
{
    public class GeoJSONMultiPolygonTypeTests
    {
        private readonly MultiPolygon geom = new MultiPolygon(new [] {
            new Polygon(
                new LinearRing(new [] {
                    new Coordinate(30, 20),
                    new Coordinate(45, 40),
                    new Coordinate(10, 40),
                    new Coordinate(30, 20)
            })),
            new Polygon(
                new LinearRing(new [] {
                    new Coordinate(15, 5),
                    new Coordinate(40, 10),
                    new Coordinate(10, 20),
                    new Coordinate(5, 15),
                    new Coordinate(15, 5)
            }))
        });

        [Fact]
        public async Task MultiPolygon_Execution_Output()
        {
            ISchema schema = SchemaBuilder.New()
                .BindClrType<Coordinate, GeoJSONPositionScalar>()
                .AddType<GeoJSONMultiPolygonType>()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("test")
                    .Resolver(geom))
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ test { type coordinates bbox crs }}");
            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task MultiPolygon_Execution_With_Fragments()
        {
            ISchema schema = SchemaBuilder.New()
                .AddSpatialTypes()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("test")
                    .Type<GeoJSONMultiPolygonType>()
                    .Resolver(geom))
                .Create();
            IQueryExecutor executor = schema.MakeExecutable();
            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ test { ... on MultiPolygon { type coordinates bbox crs }}}");
            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void MultiPolygon_Execution_Tests()
        {
            ISchema schema = SchemaBuilder.New()
                   .BindClrType<Coordinate, GeoJSONPositionScalar>()
                   .AddType<GeoJSONMultiPolygonType>()
                   .AddQueryType(d => d
                       .Name("Query")
                       .Field("test")
                       .Resolver(geom))
                   .Create();

            schema.ToString().MatchSnapshot();
        }
    }
}
