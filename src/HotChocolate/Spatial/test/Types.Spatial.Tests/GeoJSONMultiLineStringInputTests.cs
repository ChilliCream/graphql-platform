using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using NetTopologySuite.Geometries;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Spatial.Tests
{
    public class GeoJSONMultiLineStringInputTests
    {
        private readonly ListValueNode multiLinestring = new ListValueNode(
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

        private ISchema CreateSchema() => SchemaBuilder.New()
            .AddQueryType(d => d
            .Name("Query")
            .Field("test")
            .Argument("arg", a => a.Type<GeoJSONMultiLineStringInput>())
            .Resolver("ghi"))
            .Create();

        private InputObjectType CreateInputType()
        {
            ISchema schema = CreateSchema();

            return schema.GetType<InputObjectType>("MultiLineStringInput");
        }

        [Fact]
        public void ParseLiteral_MultiLineString_With_Valid_Coordinates()
        {
            // arrange
            InputObjectType type = CreateInputType();

            // act
            object result = type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("type",
                        new EnumValueNode(GeoJSONGeometryType.MultiLineString)),
                    new ObjectFieldNode("coordinates", multiLinestring)));

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
            object result = type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("type",
                        new EnumValueNode(GeoJSONGeometryType.MultiLineString)),
                    new ObjectFieldNode("coordinates", multiLinestring),
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

            object result = type.ParseLiteral(NullValueNode.Default);

            Assert.Null(result);
        }

        [Fact]
        public void ParseLiteral_MultiLineString_Is_Not_ObjectType_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            Assert.Throws<InputObjectSerializationException>(
                () => type.ParseLiteral(new ListValueNode()));
        }

        [Fact]
        public void ParseLiteral_MultiLineString_With_Missing_Fields_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            Assert.Throws<InputObjectSerializationException>(() => type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("coordinates", multiLinestring),
                    new ObjectFieldNode("missingType", new StringValueNode("ignored")))));
        }

        [Fact]
        public void ParseLiteral_MultiLineString_With_Empty_Coordinates_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            Assert.Throws<InputObjectSerializationException>(() => type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("type",
                        new EnumValueNode(GeoJSONGeometryType.MultiLineString)),
                    new ObjectFieldNode("coordinates", new ListValueNode()))));
        }

        [Fact]
        public void ParseLiteral_MultiLineString_With_Wrong_Geometry_Type_Throws()
        {
            // arrange
            InputObjectType type = CreateInputType();

            Assert.Throws<InputObjectSerializationException>(() => type.ParseLiteral(
               new ObjectValueNode(
                   new ObjectFieldNode("type", new EnumValueNode(GeoJSONGeometryType.Polygon)),
                   new ObjectFieldNode("coordinates", multiLinestring))));
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
                    .Argument("arg", a => a.Type<GeoJSONMultiLineStringInput>())
                    .Resolver(ctx => ctx.Argument<MultiLineString>("arg").ToString()))
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ test(arg: { type: MULTILINESTRING, coordinates: [ [" +
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
    }
}
