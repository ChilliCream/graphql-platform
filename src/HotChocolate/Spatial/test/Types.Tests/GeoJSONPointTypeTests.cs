using System.Threading.Tasks;
using HotChocolate.Execution;
using NetTopologySuite.Geometries;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Spatial.Types.Tests
{
    public class GeoJSONPointTypeTests
    {
        private readonly Point geom = new Point(new Coordinate(30, 10));

        [Fact]
        public async Task Point_Execution_Output()
        {
            ISchema schema = SchemaBuilder.New()
                .BindClrType<Coordinate, GeoJSONPositionScalar>()
                .AddType<GeoJSONPointType>()
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
        public async Task Point_Execution_With_Fragments()
        {
            ISchema schema = SchemaBuilder.New()
                .AddSpatialTypes()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("test")
                    .Type<GeoJSONPointType>()
                    .Resolver(geom))
                .Create();
            IQueryExecutor executor = schema.MakeExecutable();
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
                   .BindClrType<Coordinate, GeoJSONPositionScalar>()
                   .AddType<GeoJSONPointType>()
                   .AddQueryType(d => d
                       .Name("Query")
                       .Field("test")
                       .Resolver(geom))
                   .Create();

            schema.ToString().MatchSnapshot();
        }
    }
}
