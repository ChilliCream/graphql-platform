using System.Threading.Tasks;
using HotChocolate.Execution;
using NetTopologySuite.Geometries;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Spatial.Tests
{
    public class GeoJSONMultiPointTypeTests
    {
        private readonly MultiPoint geom = new MultiPoint(new [] {
            new Point(new Coordinate(10, 40)),
            new Point(new Coordinate(40, 30)),
            new Point(new Coordinate(20, 20)),
            new Point(new Coordinate(30, 10)),
        });

        [Fact]
        public async Task MultiPoint_Execution_Output()
        {
            ISchema schema = SchemaBuilder.New()
                .BindClrType<Coordinate, GeoJSONPositionScalar>()
                .AddType<GeoJSONMultiPointType>()
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
        public async Task MultiPoint_Execution_With_Fragments()
        {
            ISchema schema = SchemaBuilder.New()
                .AddSpatialTypes()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("test")
                    .Type<GeoJSONMultiPointType>()
                    .Resolver(geom))
                .Create();
            IQueryExecutor executor = schema.MakeExecutable();
            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ test { ... on MultiPoint { type coordinates bbox crs }}}");
            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void MultiPoint_Execution_Tests()
        {
            ISchema schema = SchemaBuilder.New()
                   .BindClrType<Coordinate, GeoJSONPositionScalar>()
                   .AddType<GeoJSONMultiPointType>()
                   .AddQueryType(d => d
                       .Name("Query")
                       .Field("test")
                       .Resolver(geom))
                   .Create();

            schema.ToString().MatchSnapshot();
        }
    }
}
