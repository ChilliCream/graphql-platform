using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types.Descriptors;
using NetTopologySuite.Geometries;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Spatial
{
    public class GeoJsonMultiLineStringTypeTests
    {
        private readonly MultiLineString _geom = new MultiLineString(
            new[]
            {
                new LineString(
                    new[]
                    {
                        new Coordinate(10, 10), new Coordinate(20, 20), new Coordinate(10, 40)
                    }),
                new LineString(
                    new[]
                    {
                        new Coordinate(40, 40),
                        new Coordinate(30, 30),
                        new Coordinate(40, 20),
                        new Coordinate(30, 10)
                    })
            });

        [Fact]
        public async Task MultiLineString_Execution_Output()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddConvention<INamingConventions, MockNamingConvention>()
                .BindClrType<Coordinate, GeoJsonPositionType>()
                .AddType<GeoJsonMultiLineStringType>()
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
        public async Task MultiLineString_Execution_With_Fragments()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddConvention<INamingConventions, MockNamingConvention>()
                .AddSpatialTypes()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("test")
                        .Type<GeoJsonMultiLineStringType>()
                        .Resolver(_geom))
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ test { ... on MultiLineString { type coordinates bbox crs }}}");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void MultiLineString_Execution_Tests()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddConvention<INamingConventions, MockNamingConvention>()
                .BindClrType<Coordinate, GeoJsonPositionType>()
                .AddType<GeoJsonMultiLineStringType>()
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
