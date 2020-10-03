using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using NetTopologySuite.Geometries;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Spatial.Tests
{
    public class GeoJsonPointInputTests
    {
        private readonly ListValueNode _point = new ListValueNode(
            new IntValueNode(30),
            new IntValueNode(10)
        );

        [Fact]
        public void ParseLiteral_Point_With_Valid_Coordinates_Scalar()
        {
            // arrange
            GeometryType type = CreateScalarType();

            // act
            object? result = type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode(
                        "type",
                        new EnumValueNode(nameof(GeoJsonGeometryType.Point))),
                    new ObjectFieldNode("coordinates", _point)));

            // assert
            Assert.Equal(30, Assert.IsType<Point>(result).X);
            Assert.Equal(10, Assert.IsType<Point>(result).Y);
        }

        [Fact]
        public void ParseLiteral_Point_With_Valid_Coordinates()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            object? result = type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode(
                        "type",
                        new EnumValueNode(nameof(GeoJsonGeometryType.Point))),
                    new ObjectFieldNode("coordinates", _point)));

            // assert
            Assert.Equal(30, Assert.IsType<Point>(result).X);
            Assert.Equal(10, Assert.IsType<Point>(result).Y);
        }

        [Fact]
        public void ParseLiteral_Point_With_Valid_Coordinates_With_CRS()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            object? result = type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode(
                        "type",
                        new EnumValueNode(nameof(GeoJsonGeometryType.Point))),
                    new ObjectFieldNode("coordinates", _point),
                    new ObjectFieldNode("crs", 26912)));

            // assert
            Assert.Equal(30, Assert.IsType<Point>(result).X);
            Assert.Equal(10, Assert.IsType<Point>(result).Y);
            Assert.Equal(26912, Assert.IsType<Point>(result).SRID);
        }

        [Fact]
        public void ParseLiteral_Point_Is_Null()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            object? result = type.ParseLiteral(NullValueNode.Default);

            // assert
            Assert.Null(result);
        }

        [Fact]
        public void ParseLiteral_Point_Is_Not_ObjectType_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            // assert
            Assert.Throws<InvalidOperationException>(
                () => type.ParseLiteral(new ListValueNode()));
        }

        [Fact]
        public void ParseLiteral_Point_With_Missing_Fields_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            // assert
            Assert.Throws<SerializationException>(
                () => type.ParseLiteral(
                    new ObjectValueNode(
                        new ObjectFieldNode("missingType", new StringValueNode("ignored")),
                        new ObjectFieldNode("coordinates", _point))));
        }

        [Fact]
        public void ParseLiteral_Point_With_Empty_Coordinates_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            // assert
            Assert.Throws<SerializationException>(
                () => type.ParseLiteral(
                    new ObjectValueNode(
                        new ObjectFieldNode("type", new EnumValueNode(GeoJsonGeometryType.Point)),
                        new ObjectFieldNode("coordinates", new ListValueNode()))));
        }

        [Fact]
        public void ParseLiteral_Point_With_Wrong_Geometry_Type_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            // assert
            Assert.Throws<SerializationException>(
                () => type.ParseLiteral(
                    new ObjectValueNode(
                        new ObjectFieldNode("type", new EnumValueNode(GeoJsonGeometryType.Polygon)),
                        new ObjectFieldNode("coordinates", _point))));
        }

        [Fact]
        public async Task Execution_Tests()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("test")
                        .Argument("arg", a => a.Type<GeoJsonPointInputType>())
                        .Resolver(ctx => ctx.ArgumentValue<Point>("arg").ToString()))
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ test(arg: { type: Point, coordinates:[9,10] })}");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Schema_Tests()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            // assert
            schema.ToString().MatchSnapshot();
        }

        private ISchema CreateSchema() => SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .AddType<MockObjectType>()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("test")
                    .Argument("arg", a => a.Type<GeoJsonPointInputType>())
                    .Resolver("ghi"))
            .Create();

        private InputObjectType CreateInputType()
        {
            ISchema schema = CreateSchema();

            return schema.GetType<InputObjectType>("GeoJSONPointInput");
        }

        private GeometryType CreateScalarType()
        {
            ISchema schema = CreateSchema();

            return schema.GetType<GeometryType>("Geometry");
        }
    }
}
