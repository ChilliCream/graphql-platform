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
    public class GeoJsonMultiLineStringInputTests
    {
        private readonly ListValueNode _multiLinestring = new ListValueNode(
            new ListValueNode(
                new ListValueNode(
                    new IntValueNode(10),
                    new IntValueNode(10)),
                new ListValueNode(
                    new IntValueNode(20),
                    new IntValueNode(20)),
                new ListValueNode(
                    new IntValueNode(10),
                    new IntValueNode(40))),
            new ListValueNode(
                new ListValueNode(
                    new IntValueNode(40),
                    new IntValueNode(40)),
                new ListValueNode(
                    new IntValueNode(30),
                    new IntValueNode(30)),
                new ListValueNode(
                    new IntValueNode(40),
                    new IntValueNode(20)),
                new ListValueNode(
                    new IntValueNode(30),
                    new IntValueNode(10))
            ));

        [Fact]
        public void ParseLiteral_MultiLineString_With_Valid_Coordinates()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            object? result = type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode(
                        "type",
                        new EnumValueNode("MultiLineString")),
                    new ObjectFieldNode("coordinates", _multiLinestring)));

            // assert
            Assert.Equal(2, Assert.IsType<MultiLineString>(result).NumGeometries);
            Assert.Equal(3, Assert.IsType<MultiLineString>(result).Geometries[0].NumPoints);
            Assert.Equal(4, Assert.IsType<MultiLineString>(result).Geometries[1].NumPoints);

            Assert.Equal(10, Assert.IsType<MultiLineString>(result).Coordinates[0].X);
            Assert.Equal(10, Assert.IsType<MultiLineString>(result).Coordinates[0].Y);
            Assert.Equal(20, Assert.IsType<MultiLineString>(result).Coordinates[1].X);
            Assert.Equal(20, Assert.IsType<MultiLineString>(result).Coordinates[1].Y);
            Assert.Equal(10, Assert.IsType<MultiLineString>(result).Coordinates[2].X);
            Assert.Equal(40, Assert.IsType<MultiLineString>(result).Coordinates[2].Y);
        }

        [Fact]
        public void ParseLiteral_MultiLineString_With_Valid_Coordinates_And_CRS()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            object? result = type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode(
                        "type",
                        new EnumValueNode("MultiLineString")),
                    new ObjectFieldNode("coordinates", _multiLinestring),
                    new ObjectFieldNode("crs", 26912)));

            // assert
            Assert.Equal(2, Assert.IsType<MultiLineString>(result).NumGeometries);
            Assert.Equal(3, Assert.IsType<MultiLineString>(result).Geometries[0].NumPoints);
            Assert.Equal(4, Assert.IsType<MultiLineString>(result).Geometries[1].NumPoints);

            Assert.Equal(10, Assert.IsType<MultiLineString>(result).Coordinates[0].X);
            Assert.Equal(10, Assert.IsType<MultiLineString>(result).Coordinates[0].Y);
            Assert.Equal(20, Assert.IsType<MultiLineString>(result).Coordinates[1].X);
            Assert.Equal(20, Assert.IsType<MultiLineString>(result).Coordinates[1].Y);
            Assert.Equal(10, Assert.IsType<MultiLineString>(result).Coordinates[2].X);
            Assert.Equal(40, Assert.IsType<MultiLineString>(result).Coordinates[2].Y);
            Assert.Equal(26912, Assert.IsType<MultiLineString>(result).SRID);
        }

        [Fact]
        public void ParseLiteral_MultiLineString_Is_Null()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            object? result = type.ParseLiteral(NullValueNode.Default);

            // assert
            Assert.Null(result);
        }

        [Fact]
        public void ParseLiteral_MultiLineString_Is_Not_ObjectType_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            // assert
            Assert.Throws<InvalidOperationException>(
                () => type.ParseLiteral(new ListValueNode()));
        }

        [Fact]
        public void ParseLiteral_MultiLineString_With_Missing_Fields_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            // assert
            Assert.Throws<SerializationException>(
                () => type.ParseLiteral(
                    new ObjectValueNode(
                        new ObjectFieldNode("coordinates", _multiLinestring),
                        new ObjectFieldNode("missingType", new StringValueNode("ignored")))));
        }

        [Fact]
        public void ParseLiteral_MultiLineString_With_Empty_Coordinates_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            // assert
            Assert.Throws<SerializationException>(
                () => type.ParseLiteral(
                    new ObjectValueNode(
                        new ObjectFieldNode(
                            "type",
                            new EnumValueNode("MultiLineString")),
                        new ObjectFieldNode("coordinates", new ListValueNode()))));
        }

        [Fact]
        public void ParseLiteral_MultiLineString_With_Wrong_Geometry_Type_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            // assert
            Assert.Throws<SerializationException>(
                () => type.ParseLiteral(
                    new ObjectValueNode(
                        new ObjectFieldNode("type", new EnumValueNode(GeoJsonGeometryType.Polygon)),
                        new ObjectFieldNode("coordinates", _multiLinestring))));
        }

        [Fact]
        public async Task Execution_Tests()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("test")
                        .Argument("arg", a => a.Type<GeoJsonMultiLineStringInputType>())
                        .Resolver(ctx => ctx.ArgumentValue<MultiLineString>("arg").ToString()))
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ test(arg: { type: MultiLineString, coordinates: [ [" +
                "[10, 10], [20, 20], [10, 40]], [[40, 40], [30, 30], [40, 20], [30, 10]] ] })}");

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
                    .Argument("arg", a => a.Type<GeoJsonMultiLineStringInputType>())
                    .Resolver("ghi"))
            .Create();

        private InputObjectType CreateInputType()
        {
            ISchema schema = CreateSchema();

            return schema.GetType<InputObjectType>("GeoJSONMultiLineStringInput");
        }
    }
}
