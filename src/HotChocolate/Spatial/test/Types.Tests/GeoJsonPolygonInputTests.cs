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
    public class GeoJsonPolygonInputTests
    {
        private readonly ListValueNode _polygon = new ListValueNode(
            new ListValueNode(
                new IntValueNode(30),
                new IntValueNode(10)),
            new ListValueNode(
                new IntValueNode(40),
                new IntValueNode(40)),
            new ListValueNode(
                new IntValueNode(20),
                new IntValueNode(40)),
            new ListValueNode(
                new IntValueNode(10),
                new IntValueNode(20)),
            new ListValueNode(
                new IntValueNode(30),
                new IntValueNode(10)));

        [Fact]
        public void ParseLiteral_Polygon_With_Single_Ring()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            object? result = type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode(
                        "type",
                        new EnumValueNode(nameof(GeoJsonGeometryType.Polygon))),
                    new ObjectFieldNode("coordinates", _polygon)));

            // assert
            Assert.Equal(5, Assert.IsType<Polygon>(result).NumPoints);
            Assert.Equal(1, Assert.IsType<Polygon>(result).NumGeometries);
            Assert.Equal(30, Assert.IsType<Polygon>(result).Coordinates[0].X);
            Assert.Equal(10, Assert.IsType<Polygon>(result).Coordinates[0].Y);
            Assert.Equal(40, Assert.IsType<Polygon>(result).Coordinates[1].X);
            Assert.Equal(40, Assert.IsType<Polygon>(result).Coordinates[1].Y);
            Assert.Equal(20, Assert.IsType<Polygon>(result).Coordinates[2].X);
            Assert.Equal(40, Assert.IsType<Polygon>(result).Coordinates[2].Y);
            Assert.Equal(10, Assert.IsType<Polygon>(result).Coordinates[3].X);
            Assert.Equal(20, Assert.IsType<Polygon>(result).Coordinates[3].Y);
        }

        [Fact]
        public void ParseLiteral_Polygon_With_CRS()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            object? result = type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode(
                        "type",
                        new EnumValueNode(nameof(GeoJsonGeometryType.Polygon))),
                    new ObjectFieldNode("coordinates", _polygon),
                    new ObjectFieldNode("crs", 26912)));

            // assert
            Assert.Equal(5, Assert.IsType<Polygon>(result).NumPoints);
            Assert.Equal(1, Assert.IsType<Polygon>(result).NumGeometries);
            Assert.Equal(30, Assert.IsType<Polygon>(result).Coordinates[0].X);
            Assert.Equal(10, Assert.IsType<Polygon>(result).Coordinates[0].Y);
            Assert.Equal(40, Assert.IsType<Polygon>(result).Coordinates[1].X);
            Assert.Equal(40, Assert.IsType<Polygon>(result).Coordinates[1].Y);
            Assert.Equal(20, Assert.IsType<Polygon>(result).Coordinates[2].X);
            Assert.Equal(40, Assert.IsType<Polygon>(result).Coordinates[2].Y);
            Assert.Equal(10, Assert.IsType<Polygon>(result).Coordinates[3].X);
            Assert.Equal(20, Assert.IsType<Polygon>(result).Coordinates[3].Y);
            Assert.Equal(26912, Assert.IsType<Polygon>(result).SRID);
        }

        [Fact]
        public void ParseLiteral_Polygon_Is_Null()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            object? result = type.ParseLiteral(NullValueNode.Default);

            // assert
            Assert.Null(result);
        }

        [Fact]
        public void ParseLiteral_Polygon_Is_Not_ObjectType_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            // assert
            Assert.Throws<InvalidOperationException>(
                () => type.ParseLiteral(new ListValueNode()));
        }

        [Fact]
        public void ParseLiteral_Polygon_With_Missing_Fields_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            // assert
            Assert.Throws<SerializationException>(
                () => type.ParseLiteral(
                    new ObjectValueNode(
                        new ObjectFieldNode("coordinates", _polygon),
                        new ObjectFieldNode("missingType", new StringValueNode("ignored")))));
        }

        [Fact]
        public void ParseLiteral_Polygon_With_Empty_Coordinates_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            // assert
            Assert.Throws<SerializationException>(
                () => type.ParseLiteral(
                    new ObjectValueNode(
                        new ObjectFieldNode("type", new EnumValueNode(GeoJsonGeometryType.Polygon)),
                        new ObjectFieldNode("coordinates", new ListValueNode()))));
        }

        [Fact]
        public void ParseLiteral_Polygon_With_Wrong_Geometry_Type_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            // assert
            Assert.Throws<SerializationException>(
                () => type.ParseLiteral(
                    new ObjectValueNode(
                        new ObjectFieldNode("type", new EnumValueNode(GeoJsonGeometryType.Point)),
                        new ObjectFieldNode("coordinates", _polygon))));
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
                        .Argument("arg", a => a.Type<GeoJsonPolygonInputType>())
                        .Resolver(ctx => ctx.ArgumentValue<Polygon>("arg").ToString()))
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ test(arg: { type: Polygon, coordinates:[ [30, 10], [40, 40], [20, 40], [10, 20], [30, 10] ] })}");

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

        private ISchema CreateSchema() => SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("test")
                    .Argument("arg", a => a.Type<GeoJsonPolygonInputType>())
                    .Resolver("ghi"))
            .Create();

        private InputObjectType CreateInputType()
        {
            ISchema schema = CreateSchema();

            return schema.GetType<InputObjectType>("GeoJSONPolygonInput");
        }
    }
}
