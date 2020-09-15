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
    public class GeoJsonMultiPointInputTests
    {
        private readonly ListValueNode _multipoint = new ListValueNode(
            new ListValueNode(
                new IntValueNode(10),
                new IntValueNode(40)
            ),
            new ListValueNode(
                new IntValueNode(40),
                new IntValueNode(30)
            ),
            new ListValueNode(
                new IntValueNode(20),
                new IntValueNode(20)
            ),
            new ListValueNode(
                new IntValueNode(30),
                new IntValueNode(10)
            ));

        private ISchema CreateSchema() => SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("test")
                    .Argument("arg", a => a.Type<GeoJsonMultiPointInput>())
                    .Resolver("ghi"))
            .Create();

        private InputObjectType CreateInputType()
        {
            ISchema schema = CreateSchema();

            return schema.GetType<InputObjectType>("GeoJSONMultiPointInput");
        }

        [Fact]
        public void ParseLiteral_MultiPoint_With_Valid_Coordinates()
        {
            InputObjectType type = CreateInputType();

            // act
            object? result = type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("type", new EnumValueNode("MULTI_POINT")),
                    new ObjectFieldNode("coordinates", _multipoint)));

            // assert
            Assert.Equal(4, Assert.IsType<MultiPoint>(result).NumPoints);

            Assert.Equal(10, Assert.IsType<MultiPoint>(result).Coordinates[0].X);
            Assert.Equal(40, Assert.IsType<MultiPoint>(result).Coordinates[0].Y);
            Assert.Equal(40, Assert.IsType<MultiPoint>(result).Coordinates[1].X);
            Assert.Equal(30, Assert.IsType<MultiPoint>(result).Coordinates[1].Y);
            Assert.Equal(20, Assert.IsType<MultiPoint>(result).Coordinates[2].X);
            Assert.Equal(20, Assert.IsType<MultiPoint>(result).Coordinates[2].Y);
            Assert.Equal(30, Assert.IsType<MultiPoint>(result).Coordinates[3].X);
            Assert.Equal(10, Assert.IsType<MultiPoint>(result).Coordinates[3].Y);
        }

        [Fact]
        public void ParseLiteral_MultiPoint_With_Valid_Coordinates_And_CRS()
        {
            InputObjectType type = CreateInputType();

            // act
            object? result = type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("type", new EnumValueNode("MULTI_POINT")),
                    new ObjectFieldNode("coordinates", _multipoint),
                    new ObjectFieldNode("crs", 26912)));

            // assert
            Assert.Equal(4, Assert.IsType<MultiPoint>(result).NumPoints);

            Assert.Equal(10, Assert.IsType<MultiPoint>(result).Coordinates[0].X);
            Assert.Equal(40, Assert.IsType<MultiPoint>(result).Coordinates[0].Y);
            Assert.Equal(40, Assert.IsType<MultiPoint>(result).Coordinates[1].X);
            Assert.Equal(30, Assert.IsType<MultiPoint>(result).Coordinates[1].Y);
            Assert.Equal(20, Assert.IsType<MultiPoint>(result).Coordinates[2].X);
            Assert.Equal(20, Assert.IsType<MultiPoint>(result).Coordinates[2].Y);
            Assert.Equal(30, Assert.IsType<MultiPoint>(result).Coordinates[3].X);
            Assert.Equal(10, Assert.IsType<MultiPoint>(result).Coordinates[3].Y);
            Assert.Equal(26912, Assert.IsType<MultiPoint>(result).SRID);
        }

        [Fact]
        public void ParseLiteral_MultiPoint_Is_Null()
        {
            InputObjectType type = CreateInputType();

            object? result = type.ParseLiteral(NullValueNode.Default);

            Assert.Null(result);
        }

        [Fact]
        public void ParseLiteral_MultiPoint_Is_Not_ObjectType_Throws()
        {
            InputObjectType type = CreateInputType();

            Assert.Throws<InvalidOperationException>(
                () => type.ParseLiteral(new ListValueNode()));
        }

        [Fact]
        public void ParseLiteral_MultiPoint_With_Missing_Fields_Throws()
        {
            InputObjectType type = CreateInputType();

            Assert.Throws<SerializationException>(
                () => type.ParseLiteral(
                    new ObjectValueNode(
                        new ObjectFieldNode("missingType", new StringValueNode("ignored")),
                        new ObjectFieldNode("coordinates", _multipoint))));
        }

        [Fact]
        public void ParseLiteral_MultiPoint_With_Empty_Coordinates_Throws()
        {
            InputObjectType type = CreateInputType();

            Assert.Throws<SerializationException>(
                () => type.ParseLiteral(
                    new ObjectValueNode(
                        new ObjectFieldNode(
                            "type",
                            new EnumValueNode("MULTI_POINT")),
                        new ObjectFieldNode("coordinates", new ListValueNode()))));
        }

        [Fact]
        public void ParseLiteral_MultiPoint_With_Wrong_Geometry_Type_Throws()
        {
            InputObjectType type = CreateInputType();

            Assert.Throws<SerializationException>(
                () => type.ParseLiteral(
                    new ObjectValueNode(
                        new ObjectFieldNode("type", new EnumValueNode(GeoJsonGeometryType.Point)),
                        new ObjectFieldNode("coordinates", _multipoint))));
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
                        .Argument("arg", a => a.Type<GeoJsonMultiPointInput>())
                        .Resolver(ctx => ctx.ArgumentValue<MultiPoint>("arg").ToString()))
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ test(arg: { type: MULTI_POINT, coordinates:[[10, 40], [40, 30], [20, 20], [30, 10]] })}");

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
