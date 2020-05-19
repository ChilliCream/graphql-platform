using System.Threading.Tasks;
using HotChocolate.Execution;
using NetTopologySuite.Geometries;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Spatial.Tests
{
    public class GeoJSONLineStringTypeTests
    {
        private readonly LineString geom = new LineString(new[] {
            new Coordinate(30, 10),
            new Coordinate(10, 30),
            new Coordinate(40, 40)
        });

        [Fact]
        public async Task LineString_Execution_Output()
        {
            ISchema schema = SchemaBuilder.New()
                .BindClrType<Coordinate, GeoJSONPositionScalar>()
                .AddType<GeoJSONLineStringType>()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("test")
                    .Resolver(geom))
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ test { type coordinates bbox }}");
            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task LineString_Execution_With_Fragments()
        {
            ISchema schema = SchemaBuilder.New()
                .AddSpatialTypes()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("test")
                    .Type<GeoJSONLineStringType>()
                    .Resolver(geom))
                .Create();
            IQueryExecutor executor = schema.MakeExecutable();
            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ test { ... on LineString { type coordinates bbox }}}");
            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void LineString_Execution_Tests()
        {
            ISchema schema = SchemaBuilder.New()
                   .BindClrType<Coordinate, GeoJSONPositionScalar>()
                   .AddType<GeoJSONLineStringType>()
                   .AddQueryType(d => d
                       .Name("Query")
                       .Field("test")
                       .Resolver(geom))
                   .Create();

            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task LineString_Execution_With_CRS()
        {
            ISchema schema = SchemaBuilder.New()
                .BindClrType<Coordinate, GeoJSONPositionScalar>()
                .AddType<GeoJSONLineStringType>()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("test")
                    .Resolver(geom))
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();
            IExecutionResult result = await executor.ExecuteAsync(
                "{ test { crs }}");

            // assert
            result.MatchSnapshot();
        }
    }
}
