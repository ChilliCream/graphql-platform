using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types.Descriptors;
using NetTopologySuite.Geometries;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Spatial
{
    public class GeoJsonMultiPolygonTypeTests
    {
        private readonly MultiPolygon _geom = new MultiPolygon(new[]
        {
            new Polygon(new LinearRing(new[]
            {
                new Coordinate(30, 20),
                new Coordinate(45, 40),
                new Coordinate(10, 40),
                new Coordinate(30, 20)
            })),
            new Polygon(new LinearRing(new[]
            {
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
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddConvention<INamingConventions, MockNamingConvention>()
                .BindClrType<Coordinate, GeoJsonPositionType>()
                .AddType<GeoJsonMultiPolygonType>()
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
        public async Task MultiPolygon_Execution_With_Fragments()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddConvention<INamingConventions, MockNamingConvention>()
                .AddSpatialTypes()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("test")
                        .Type<GeoJsonMultiPolygonType>()
                        .Resolver(_geom))
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ test { ... on MultiPolygon { type coordinates bbox crs }}}");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void MultiPolygon_Execution_Tests()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddConvention<INamingConventions, MockNamingConvention>()
                .BindClrType<Coordinate, GeoJsonPositionType>()
                .AddType<GeoJsonMultiPolygonType>()
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
