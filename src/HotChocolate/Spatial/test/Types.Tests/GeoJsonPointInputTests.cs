using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using NetTopologySuite.Geometries;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Spatial.Types.Tests
{
    public class GeoJSONPointInputTests
    {
        private readonly ListValueNode point = new ListValueNode(
            new IntValueNode(30),
            new IntValueNode(10)
        );

        private ISchema CreateSchema() => SchemaBuilder.New()
            .AddQueryType(d => d
            .Name("Query")
            .Field("test")
            .Argument("arg", a => a.Type<GeoJSONPointInput>())
            .Resolver("ghi"))
            .Create();

        private InputObjectType CreateInputType()
        {
            ISchema schema = CreateSchema();

            return schema.GetType<InputObjectType>("PointInput");
        }

        [Fact]
        public void ParseLiteral_Point_With_Valid_Coordinates()
        {
            InputObjectType type = CreateInputType();

            // act
            object result = type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("type", new EnumValueNode(GeoJSONGeometryType.Point)),
                    new ObjectFieldNode("coordinates", point)));

            // assert
            Assert.Equal(30, Assert.IsType<Point>(result).X);
            Assert.Equal(10, Assert.IsType<Point>(result).Y);
        }

        [Fact]
        public void ParseLiteral_Point_With_Valid_Coordinates_With_CRS()
        {
            InputObjectType type = CreateInputType();

            // act
            object result = type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("type", new EnumValueNode(GeoJSONGeometryType.Point)),
                    new ObjectFieldNode("coordinates", point),
                    new ObjectFieldNode("crs", 26912)));

            // assert
            Assert.Equal(30, Assert.IsType<Point>(result).X);
            Assert.Equal(10, Assert.IsType<Point>(result).Y);
            Assert.Equal(26912, Assert.IsType<Point>(result).SRID);

        }

        [Fact]
        public void ParseLiteral_Point_Is_Null()
        {
            InputObjectType type = CreateInputType();

            object result = type.ParseLiteral(NullValueNode.Default);

            Assert.Null(result);
        }

        [Fact]
        public void ParseLiteral_Point_Is_Not_ObjectType_Throws()
        {
            InputObjectType type = CreateInputType();

            Assert.Throws<SerializationException>(
                () => type.ParseLiteral(new ListValueNode()));
        }

        [Fact]
        public void ParseLiteral_Point_With_Missing_Fields_Throws()
        {
            InputObjectType type = CreateInputType();

            Assert.Throws<SerializationException>(() => type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("missingType", new StringValueNode("ignored")),
                    new ObjectFieldNode("coordinates", point))));
        }

        [Fact]
        public void ParseLiteral_Point_With_Empty_Coordinates_Throws()
        {
            InputObjectType type = CreateInputType();

            Assert.Throws<ScalarSerializationException>(() => type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("type", new EnumValueNode(GeoJSONGeometryType.Point)),
                    new ObjectFieldNode("coordinates", new ListValueNode()))));
        }

        [Fact]
        public void ParseLiteral_Point_With_Wrong_Geometry_Type_Throws()
        {
            InputObjectType type = CreateInputType();

            Assert.Throws<SerializationException>(() => type.ParseLiteral(
               new ObjectValueNode(
                   new ObjectFieldNode("type", new EnumValueNode(GeoJSONGeometryType.Polygon)),
                   new ObjectFieldNode("coordinates", point))));
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
            ISchema schema = CreateSchema();

            // assert
            schema.ToString().MatchSnapshot();
        }
    }
}
