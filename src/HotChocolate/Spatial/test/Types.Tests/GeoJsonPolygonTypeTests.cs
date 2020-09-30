using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types.Descriptors;
using NetTopologySuite.Geometries;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Spatial.Tests
{
    public class GeoJsonPolygonTypeTests
    {
        private readonly Polygon _geom = new Polygon(
            new LinearRing(
                new[]
                {
                    new Coordinate(30, 10),
                    new Coordinate(40, 40),
                    new Coordinate(20, 40),
                    new Coordinate(10, 20),
                    new Coordinate(30, 10)
                }));

        [Fact]
        public async Task Polygon_Execution_Output()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddConvention<INamingConventions, MockNamingConvention>()
                .BindClrType<Coordinate, GeoJsonPositionType>()
                .AddType<GeoJsonPolygonType>()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("test")
                        .Resolver(_geom))
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ test { type coordinates bbox crs }}");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Polygon_Execution_With_Fragments()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddConvention<INamingConventions, MockNamingConvention>()
                .AddSpatialTypes()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("test")
                        .Type<GeoJsonPolygonType>()
                        .Resolver(_geom))
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ test { ... on Polygon { type coordinates bbox crs }}}");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Polygon_Execution_Tests()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddConvention<INamingConventions, MockNamingConvention>()
                .BindClrType<Coordinate, GeoJsonPositionType>()
                .AddType<GeoJsonPolygonType>()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("test")
                        .Resolver(_geom))
                .Create();

            // act
            // assert
            schema.ToString().MatchSnapshot();
        }
    }
}
