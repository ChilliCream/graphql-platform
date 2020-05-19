using System.Threading.Tasks;
using HotChocolate.Execution;
using NetTopologySuite.Geometries;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Spatial.Tests
{
    public class GeoJSONMultiLineStringTypeTests
    {
        private readonly MultiLineString geom = new MultiLineString(new [] {
            new LineString(new[] {
                new Coordinate(10, 10),
                new Coordinate(20, 20),
                new Coordinate(10, 40)
            }),
            new LineString(new[] {
                new Coordinate(40, 40),
                new Coordinate(30, 30),
                new Coordinate(40, 20),
                new Coordinate(30, 10)
            })});

        [Fact]
        public async Task MultiLineString_Execution_Output()
        {
            ISchema schema = SchemaBuilder.New()
                .BindClrType<Coordinate, GeoJSONPositionScalar>()
                .AddType<GeoJSONMultiLineStringType>()
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
        public async Task MultiLineString_Execution_With_Fragments()
        {
            ISchema schema = SchemaBuilder.New()
                .AddSpatialTypes()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("test")
                    .Type<GeoJSONMultiLineStringType>()
                    .Resolver(geom))
                .Create();
            IQueryExecutor executor = schema.MakeExecutable();
            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ test { ... on MultiLineString { type coordinates bbox crs }}}");
            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void MultiLineString_Execution_Tests()
        {
            ISchema schema = SchemaBuilder.New()
                   .BindClrType<Coordinate, GeoJSONPositionScalar>()
                   .AddType<GeoJSONMultiLineStringType>()
                   .AddQueryType(d => d
                       .Name("Query")
                       .Field("test")
                       .Resolver(geom))
                   .Create();

            schema.ToString().MatchSnapshot();
        }
    }
}
