using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using NetTopologySuite.Geometries;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Spatial
{
    public class GeoJsonLineStringInputTests
    {
        private readonly ListValueNode _linestring = new ListValueNode(
            new ListValueNode(
                new IntValueNode(30),
                new IntValueNode(10)),
            new ListValueNode(
                new IntValueNode(10),
                new IntValueNode(30)),
            new ListValueNode(
                new IntValueNode(40),
                new IntValueNode(40)));

        private ISchema CreateSchema() => SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("test")
                    .Argument("arg", a => a.Type<GeoJsonLineStringInputType>())
                    .Resolver("ghi"))
            .Create();

        private InputObjectType CreateInputType()
        {
            ISchema schema = CreateSchema();

            return schema.GetType<InputObjectType>("GeoJSONLineStringInput");
        }

        [Fact]
        public void ParseLiteral_LineString_With_Valid_Coordinates()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            object? result = type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("type", new EnumValueNode("LineString")),
                    new ObjectFieldNode("coordinates", _linestring)));

            // assert
            Assert.Equal(3, Assert.IsType<LineString>(result).NumPoints);

            Assert.Equal(30, Assert.IsType<LineString>(result).Coordinates[0].X);
            Assert.Equal(10, Assert.IsType<LineString>(result).Coordinates[0].Y);
            Assert.Equal(10, Assert.IsType<LineString>(result).Coordinates[1].X);
            Assert.Equal(30, Assert.IsType<LineString>(result).Coordinates[1].Y);
            Assert.Equal(40, Assert.IsType<LineString>(result).Coordinates[2].X);
            Assert.Equal(40, Assert.IsType<LineString>(result).Coordinates[2].Y);
        }

        [Fact]
        public void ParseLiteral_LineString_With_Valid_Coordinates_And_CRS()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            object? result = type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("type", new EnumValueNode("LineString")),
                    new ObjectFieldNode("coordinates", _linestring),
                    new ObjectFieldNode("crs", 26912)));

            // assert
            Assert.Equal(3, Assert.IsType<LineString>(result).NumPoints);

            Assert.Equal(30, Assert.IsType<LineString>(result).Coordinates[0].X);
            Assert.Equal(10, Assert.IsType<LineString>(result).Coordinates[0].Y);
            Assert.Equal(10, Assert.IsType<LineString>(result).Coordinates[1].X);
            Assert.Equal(30, Assert.IsType<LineString>(result).Coordinates[1].Y);
            Assert.Equal(40, Assert.IsType<LineString>(result).Coordinates[2].X);
            Assert.Equal(40, Assert.IsType<LineString>(result).Coordinates[2].Y);
            Assert.Equal(26912, Assert.IsType<LineString>(result).SRID);
        }

        [Fact]
        public void ParseLiteral_LineString_Is_Null()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            object? result = type.ParseLiteral(NullValueNode.Default);

            // assert
            Assert.Null(result);
        }

        [Fact]
        public void ParseLiteral_LineString_Is_Not_ObjectType_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            // assert
            Assert.Throws<InvalidOperationException>(
                () => type.ParseLiteral(new ListValueNode()));
        }

        [Fact]
        public void ParseLiteral_LineString_With_Missing_Fields_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            // assert
            Assert.Throws<SerializationException>(
                () => type.ParseLiteral(
                    new ObjectValueNode(
                        new ObjectFieldNode("coordinates", _linestring),
                        new ObjectFieldNode("missingType", new StringValueNode("ignored")))));
        }

        [Fact]
        public void ParseLiteral_LineString_With_Empty_Coordinates_Throws()
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
                            new EnumValueNode("LineString")),
                        new ObjectFieldNode("coordinates", new ListValueNode()))));
        }

        [Fact]
        public void ParseLiteral_LineString_With_Wrong_Geometry_Type_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            // assert
            Assert.Throws<SerializationException>(
                () => type.ParseLiteral(
                    new ObjectValueNode(
                        new ObjectFieldNode("type", new EnumValueNode("POLYGON")),
                        new ObjectFieldNode("coordinates", _linestring))));
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
                        .Argument("arg", a => a.Type<GeoJsonLineStringInputType>())
                        .Resolver(ctx => ctx.ArgumentValue<LineString>("arg").ToString()))
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ test(arg: { type: LineString, coordinates: [[30, 10], [10, 30], [40, 40]]})}");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Schema_Tests() =>
            CreateSchema()
                .Print()
                .MatchSnapshot();

        [Fact]
        public void ParseLiteral_With_Input_Crs()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("test")
                        .Argument("arg", a => a.Type<GeoJsonLineStringInputType>())
                        .Resolver("ghi"))
                .Create();

            InputObjectType type = schema.GetType<InputObjectType>("GeoJSONLineStringInput");

            var node = new ObjectValueNode(
                new ObjectFieldNode("type", new EnumValueNode("LineString")),
                new ObjectFieldNode("coordinates", _linestring),
                new ObjectFieldNode("crs", 26912));

            // act
            object? result = type.ParseLiteral(node);

            // assert
            Assert.Equal(26912, Assert.IsType<LineString>(result).SRID);
        }
    }
}

