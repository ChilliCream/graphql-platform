using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types.Descriptors;
using NetTopologySuite.Geometries;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Spatial.Tests
{
    public class GeoJSONPointTypeTests
    {
        private readonly Point _geom = new Point(new Coordinate(30, 10));

        [Fact]
        public async Task Point_Execution_Output()
        {
            ISchema schema = SchemaBuilder.New()
                .AddConvention<INamingConventions, MockNamingConvention>()
                .BindClrType<Coordinate, GeoJSONPositionScalar>()
                .AddType<GeoJSONPointType>()
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
        public async Task Point_Execution_With_Fragments()
        {
            ISchema schema = SchemaBuilder.New()
                .AddConvention<INamingConventions, MockNamingConvention>()
                .AddSpatialTypes()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("test")
                        .Type<GeoJSONPointType>()
                        .Resolver(_geom))
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();
            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ test { ... on Point { type coordinates bbox crs }}}");
            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Point_Execution_Tests()
        {
            ISchema schema = SchemaBuilder.New()
                .AddConvention<INamingConventions, MockNamingConvention>()
                .BindClrType<Coordinate, GeoJSONPositionScalar>()
                .AddType<GeoJSONPointType>()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("test")
                        .Resolver(_geom))
                .Create();

            schema.ToString().MatchSnapshot();
        }
    }
}
