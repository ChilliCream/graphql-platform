using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial;

public class GeoJsonPointInputTests
{
    private readonly ListValueNode _point = new(
        new IntValueNode(30),
        new IntValueNode(10));

    private ISchema CreateSchema() => SchemaBuilder.New()
        .AddConvention<INamingConventions, MockNamingConvention>()
        .AddType<MockObjectType>()
        .AddQueryType(
            d => d
                .Name("Query")
                .Field("test")
                .Argument("arg", a => a.Type<GeoJsonPointInputType>())
                .Resolve("ghi"))
        .Create();

    private InputObjectType CreateInputType()
    {
        var schema = CreateSchema();
        return schema.GetType<InputObjectType>("GeoJSONPointInput");
    }

    private GeometryType CreateScalarType()
    {
        var schema = CreateSchema();
        return schema.GetType<GeometryType>("Geometry");
    }

    [Fact]
    public void ParseLiteral_Point_With_Valid_Coordinates_Scalar()
    {
        // arrange
        var type = CreateScalarType();

        // act
        var result = type.ParseLiteral(
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
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType();

        // act
        var result = inputParser.ParseLiteral(
            new ObjectValueNode(
                new ObjectFieldNode(
                    "type",
                    new EnumValueNode(nameof(GeoJsonGeometryType.Point))),
                new ObjectFieldNode("coordinates", _point)),
            type);

        // assert
        Assert.Equal(30, Assert.IsType<Point>(result).X);
        Assert.Equal(10, Assert.IsType<Point>(result).Y);
    }

    [Fact]
    public void ParseLiteral_Point_With_Valid_Coordinates_With_CRS()
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType();

        // act
        var result = inputParser.ParseLiteral(
            new ObjectValueNode(
                new ObjectFieldNode(
                    "type",
                    new EnumValueNode(nameof(GeoJsonGeometryType.Point))),
                new ObjectFieldNode("coordinates", _point),
                new ObjectFieldNode("crs", 26912)),
            type);

        // assert
        Assert.Equal(30, Assert.IsType<Point>(result).X);
        Assert.Equal(10, Assert.IsType<Point>(result).Y);
        Assert.Equal(26912, Assert.IsType<Point>(result).SRID);
    }

    [Fact]
    public void ParseLiteral_Point_Is_Null()
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
    public void ParseLiteral_Point_Is_Not_ObjectType_Throws()
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
    public void ParseLiteral_Point_With_Missing_Fields_Throws()
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType();

        // act
        // assert
        Assert.Throws<SerializationException>(
            () => inputParser.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("missingType", new StringValueNode("ignored")),
                    new ObjectFieldNode("coordinates", _point)),
                type));
    }

    [Fact]
    public void ParseLiteral_Point_With_Empty_Coordinates_Throws()
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
                    new ObjectFieldNode("coordinates", new ListValueNode())),
                type));
    }

    [Fact]
    public void ParseLiteral_Point_With_Wrong_Geometry_Type_Throws()
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType();

        // act
        // assert
        Assert.Throws<SerializationException>(
            () => inputParser.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("type", new EnumValueNode(GeoJsonGeometryType.Polygon)),
                    new ObjectFieldNode("coordinates", _point)),
                type));
    }

    [Fact]
    public async Task Execution_Tests()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("test")
                    .Argument("arg", a => a.Type<GeoJsonPointInputType>())
                    .Resolve(ctx => ctx.ArgumentValue<Point>("arg").ToString()))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            "{ test(arg: { type: Point, coordinates:[9,10] })}");

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
}
