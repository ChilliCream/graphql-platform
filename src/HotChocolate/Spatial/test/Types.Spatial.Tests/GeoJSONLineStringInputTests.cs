using System;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using NetTopologySuite.Geometries;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Spatial.Tests
{
    public class GeoJSONLineStringInputTests
    {
        private readonly ListValueNode linestring = new ListValueNode(
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
            .AddQueryType(d => d
            .Name("Query")
            .Field("test")
            .Argument("arg", a => a.Type<GeoJSONLineStringInput>())
            .Resolver("ghi"))
            .Create();

        private InputObjectType CreateInputType()
        {
            ISchema schema = CreateSchema();

            return schema.GetType<InputObjectType>("LineStringInput");
        }

        [Fact]
        public void ParseLiteral_LineString_With_Valid_Coordinates()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            object result = type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("type", new EnumValueNode(GeoJSONGeometryType.LineString)),
                    new ObjectFieldNode("coordinates", linestring)));

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
        public void ParseLiteral_LineString_Is_Null()
        {
            // arrange
            InputObjectType type = CreateInputType();

            object result = type.ParseLiteral(NullValueNode.Default);

            Assert.Null(result);
        }

        [Fact]
        public void ParseLiteral_LineString_Is_Not_ObjectType_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            Assert.Throws<InputObjectSerializationException>(
                () => type.ParseLiteral(new ListValueNode()));
        }

        [Fact]
        public void ParseLiteral_LineString_With_Missing_Fields_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            Assert.Throws<InputObjectSerializationException>(() => type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("coordinates", linestring),
                    new ObjectFieldNode("missingType", new StringValueNode("ignored")))));
        }

        [Fact]
        public void ParseLiteral_LineString_With_Empty_Coordinates_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            Assert.Throws<InputObjectSerializationException>(() => type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("type", new EnumValueNode(GeoJSONGeometryType.LineString)),
                    new ObjectFieldNode("coordinates", new ListValueNode()))));
        }

        [Fact]
        public void ParseLiteral_LineString_With_Wrong_Geometry_Type_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            Assert.Throws<InputObjectSerializationException>(() => type.ParseLiteral(
               new ObjectValueNode(
                   new ObjectFieldNode("type", new EnumValueNode(GeoJSONGeometryType.Polygon)),
                   new ObjectFieldNode("coordinates", linestring))));
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
                    .Argument("arg", a => a.Type<GeoJSONLineStringInput>())
                    .Resolver(ctx => ctx.Argument<LineString>("arg").ToString()))
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ test(arg: { type: LINESTRING, coordinates: [ [30, 10], [10, 30], [40, 40] ] })}");

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
