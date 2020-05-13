using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using NetTopologySuite.Geometries;
using Snapshooter.Xunit;
using Xunit;

namespace Types.Spatial.Tests
{
    public class GeoJsonPointInputTests
    {
        [Fact]
        public void ParseLiteral_Point_WithXandY()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("test")
                    .Argument("arg", a => a.Type<GeoJSONPointInput>())
                    .Resolver("ghi"))
                .Create();

            InputObjectType type =
                schema.GetType<InputObjectType>("PointInput");

            // act
            object result = type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("type", new EnumValueNode(GeoJSONGeometryType.Point)),
                    new ObjectFieldNode(
                        "coordinates",
                            new ListValueNode(
                                new IntValueNode(9),
                                new IntValueNode(8)))));

            // assert
            Assert.Equal(9, Assert.IsType<Point>(result).X);
            Assert.Equal(8, Assert.IsType<Point>(result).Y);
        }

        [Fact]
        public async Task Execution_Tests()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("test")
                    .Argument("arg", a => a.Type<GeoJSONPointInput>())
                    .Resolver(ctx => ctx.Argument<Point>("arg").ToString()))
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ test(arg: { type: POINT, coordinates:[9,10] })}");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Schema_Tests()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("test")
                    .Argument("arg", a => a.Type<GeoJSONPointInput>())
                    .Resolver("ghi"))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }
    }
}
