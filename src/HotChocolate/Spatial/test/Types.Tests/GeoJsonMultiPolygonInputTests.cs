using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial;

public class GeoJsonMultiPolygonInputTests
{
    private readonly ListValueNode _multiPolygon = new(
        new ListValueNode(
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
                    new IntValueNode(20)))),
        new ListValueNode(
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
                    new IntValueNode(5)))));

    [Fact]
    public void ParseLiteral_MultiPolygon_With_Single_Ring()
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType();

        // act
        var result = inputParser.ParseLiteral(
            new ObjectValueNode(
                new ObjectFieldNode("type", new EnumValueNode("MultiPolygon")),
                new ObjectFieldNode("coordinates", _multiPolygon)),
            type);

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
    public void ParseLiteral_MultiPolygon_With_Single_Ring_And_CRS()
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType();

        // act
        var result = inputParser.ParseLiteral(
            new ObjectValueNode(
                new ObjectFieldNode("type", new EnumValueNode("MultiPolygon")),
                new ObjectFieldNode("coordinates", _multiPolygon),
                new ObjectFieldNode("crs", 26912)),
            type);

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
        Assert.Equal(26912, Assert.IsType<MultiPolygon>(result).SRID);
    }

    [Fact]
    public void ParseLiteral_MultiPolygon_Is_Null()
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType();

        // act
        var result = inputParser.ParseLiteral(NullValueNode.Default, type);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseLiteral_MultiPolygon_Is_Not_ObjectType_Throws()
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType();

        // act
        // assert
        Assert.Throws<SerializationException>(
            () => inputParser.ParseLiteral(new ListValueNode(), type));
    }

    [Fact]
    public void ParseLiteral_MultiPolygon_With_Missing_Fields_Throws()
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType();

        // act
        // assert
        Assert.Throws<SerializationException>(
            () => inputParser.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("coordinates", _multiPolygon),
                    new ObjectFieldNode("missingType", new StringValueNode("ignored"))),
                type));
    }

    [Fact]
    public void ParseLiteral_MultiPolygon_With_Empty_Coordinates_Throws()
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType();

        // act
        // assert
        Assert.Throws<SerializationException>(
            () => inputParser.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("type", new EnumValueNode("MultiPolygon")),
                    new ObjectFieldNode("coordinates", new ListValueNode())),
                type));
    }

    [Fact]
    public void ParseLiteral_MultiPolygon_With_Wrong_Geometry_Type_Throws()
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType();

        // act
        // assert
        Assert.Throws<SerializationException>(
            () => inputParser.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("type", new EnumValueNode(GeoJsonGeometryType.Point)),
                    new ObjectFieldNode("coordinates", _multiPolygon)),
                type));
    }

    [Fact]
    public async Task Execution_Tests()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("test")
                    .Argument("arg", a => a.Type<GeoJsonMultiPolygonInputType>())
                    .Resolve(ctx => ctx.ArgumentValue<MultiPolygon>("arg").ToString()))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                  test(
                    arg: {
                      type: MultiPolygon
                      coordinates: [
                        [[[30, 20], [45, 40], [10, 40], [30, 20]]]
                        [[[15, 5], [40, 10], [10, 20], [5, 10], [15, 5]]]
                      ]
                    }
                  )
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Schema_Tests()
    {
        // arrange
        // act
        var schema = CreateSchema();

        // assert
        schema.MatchSnapshot();
    }

    private ISchema CreateSchema() => SchemaBuilder.New()
        .AddConvention<INamingConventions, MockNamingConvention>()
        .AddQueryType(
            d => d
                .Name("Query")
                .Field("test")
                .Argument("arg", a => a.Type<GeoJsonMultiPolygonInputType>())
                .Resolve("ghi"))
        .Create();

    private InputObjectType CreateInputType()
    {
        var schema = CreateSchema();
        return schema.GetType<InputObjectType>("GeoJSONMultiPolygonInput");
    }
}
