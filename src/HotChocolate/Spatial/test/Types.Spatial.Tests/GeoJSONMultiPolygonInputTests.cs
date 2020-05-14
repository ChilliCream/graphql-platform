using System;
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
    public class GeoJSONMultiPolygonInputTests
    {
        private readonly ListValueNode multiPolygon = new ListValueNode(
            new ListValueNode(
                new ListValueNode(
                    new IntValueNode(30),
                    new IntValueNode(20)),
                new ListValueNode(
                    new IntValueNode(45),
                    new IntValueNode(40)),
                new ListValueNode(
                    new IntValueNode(10),
                    new IntValueNode(40)),
                new ListValueNode(
                    new IntValueNode(30),
                    new IntValueNode(20))),
            new ListValueNode(
                new ListValueNode(
                    new IntValueNode(15),
                    new IntValueNode(5)),
                new ListValueNode(
                    new IntValueNode(40),
                    new IntValueNode(10)),
                new ListValueNode(
                    new IntValueNode(10),
                    new IntValueNode(20)),
                new ListValueNode(
                    new IntValueNode(5),
                    new IntValueNode(15)),
                new ListValueNode(
                    new IntValueNode(15),
                    new IntValueNode(5))));

        private ISchema CreateSchema() => SchemaBuilder.New()
            .AddQueryType(d => d
            .Name("Query")
            .Field("test")
            .Argument("arg", a => a.Type<GeoJSONMultiPolygonInput>())
            .Resolver("ghi"))
            .Create();

        private InputObjectType CreateInputType()
        {
            ISchema schema = CreateSchema();

            return schema.GetType<InputObjectType>("MultiPolygonInput");
        }

        [Fact]
        public void ParseLiteral_MultiPolygon_With_Single_Ring()
        {
            InputObjectType type = CreateInputType();

            // act
            object result = type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("type", new EnumValueNode(GeoJSONGeometryType.MultiPolygon)),
                    new ObjectFieldNode("coordinates", multiPolygon)));

            // assert
            Assert.Equal(2, Assert.IsType<MultiPolygon>(result).NumGeometries);
            Assert.Equal(4, Assert.IsType<MultiPolygon>(result).Geometries[0].NumPoints);
            Assert.Equal(5, Assert.IsType<MultiPolygon>(result).Geometries[1].NumPoints);

            Assert.Equal(30, Assert.IsType<MultiPolygon>(result).Coordinates[0].X);
            Assert.Equal(20, Assert.IsType<MultiPolygon>(result).Coordinates[0].Y);
            Assert.Equal(45, Assert.IsType<MultiPolygon>(result).Coordinates[1].X);
            Assert.Equal(40, Assert.IsType<MultiPolygon>(result).Coordinates[1].Y);
            Assert.Equal(10, Assert.IsType<MultiPolygon>(result).Coordinates[2].X);
            Assert.Equal(40, Assert.IsType<MultiPolygon>(result).Coordinates[2].Y);
            Assert.Equal(30, Assert.IsType<MultiPolygon>(result).Coordinates[3].X);
            Assert.Equal(20, Assert.IsType<MultiPolygon>(result).Coordinates[3].Y);
        }

        [Fact]
        public void ParseLiteral_MultiPolygon_Is_Null()
        {
            InputObjectType type = CreateInputType();

            object result = type.ParseLiteral(NullValueNode.Default);

            Assert.Null(result);
        }

        [Fact]
        public void ParseLiteral_MultiPolygon_Is_Not_ObjectType_Throws()
        {
            InputObjectType type = CreateInputType();

            Assert.Throws<InputObjectSerializationException>(
                () => type.ParseLiteral(new ListValueNode()));
        }

        [Fact]
        public void ParseLiteral_MultiPolygon_With_Missing_Fields_Throws()
        {
            InputObjectType type = CreateInputType();

            Assert.Throws<InputObjectSerializationException>(() => type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("coordinates", multiPolygon),
                    new ObjectFieldNode("missingType", new StringValueNode("ignored")))));
        }

        [Fact]
        public void ParseLiteral_MultiPolygon_With_Empty_Coordinates_Throws()
        {
            InputObjectType type = CreateInputType();

            Assert.Throws<InputObjectSerializationException>(() => type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("type", new EnumValueNode(GeoJSONGeometryType.MultiPolygon)),
                    new ObjectFieldNode("coordinates", new ListValueNode()))));
        }

        [Fact]
        public void ParseLiteral_MultiPolygon_With_Wrong_Geometry_Type_Throws()
        {
            InputObjectType type = CreateInputType();

            Assert.Throws<InputObjectSerializationException>(() => type.ParseLiteral(
               new ObjectValueNode(
                   new ObjectFieldNode("type", new EnumValueNode(GeoJSONGeometryType.Point)),
                   new ObjectFieldNode("coordinates", multiPolygon))));
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
                    .Argument("arg", a => a.Type<GeoJSONMultiPolygonInput>())
                    .Resolver(ctx => ctx.Argument<MultiPolygon>("arg").ToString()))
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ test(arg: { type: MULTIPOLYGON, coordinates:[ [" +
                "[[30, 20], [45, 40], [10, 40], [30, 20]] ], " +
                "[ [[15, 5], [40, 10], [10, 20], [5, 10], [15, 5]] ] ] })}");

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
