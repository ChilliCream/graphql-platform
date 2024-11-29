using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial;

public class GeoJsonMultiPointInputTests
{
    private readonly ListValueNode _multipoint = new(
        new ListValueNode(
            new IntValueNode(10),
            new IntValueNode(40)),
        new ListValueNode(
            new IntValueNode(40),
            new IntValueNode(30)),
        new ListValueNode(
            new IntValueNode(20),
            new IntValueNode(20)),
        new ListValueNode(
            new IntValueNode(30),
            new IntValueNode(10)));

    [Fact]
    public void ParseLiteral_MultiPoint_With_Valid_Coordinates()
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType();

        // act
        var result = inputParser.ParseLiteral(
            new ObjectValueNode(
                new ObjectFieldNode("type", new EnumValueNode("MultiPoint")),
                new ObjectFieldNode("coordinates", _multipoint)),
            type);

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
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType();

        // act
        var result = inputParser.ParseLiteral(
            new ObjectValueNode(
                new ObjectFieldNode("type", new EnumValueNode("MultiPoint")),
                new ObjectFieldNode("coordinates", _multipoint),
                new ObjectFieldNode("crs", 26912)),
            type);

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
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType();

        // act
        var result = inputParser.ParseLiteral(NullValueNode.Default, type);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseLiteral_MultiPoint_Is_Not_ObjectType_Throws()
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
    public void ParseLiteral_MultiPoint_With_Missing_Fields_Throws()
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
                    new ObjectFieldNode("coordinates", _multipoint)),
                type));
    }

    [Fact]
    public void ParseLiteral_MultiPoint_With_Empty_Coordinates_Throws()
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType();

        // act
        // assert
        Assert.Throws<SerializationException>(
            () => inputParser.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("type", new EnumValueNode("MultiPoint")),
                    new ObjectFieldNode("coordinates", new ListValueNode())),
                type));
    }

    [Fact]
    public void ParseLiteral_MultiPoint_With_Wrong_Geometry_Type_Throws()
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
                    new ObjectFieldNode("coordinates", _multipoint)),
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
                    .Argument("arg", a => a.Type<GeoJsonMultiPointInputType>())
                    .Resolve(ctx => ctx.ArgumentValue<MultiPoint>("arg").ToString()))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                    test(arg: {
                        type: MultiPoint,
                        coordinates:[[10, 40], [40, 30], [20, 20], [30, 10]]
                    })
                }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Schema_Tests()
    {
        // arrange
        var schema = CreateSchema();

        // act
        // assert
        schema.MatchSnapshot();
    }

    private ISchema CreateSchema() =>
        SchemaBuilder.New()
            .AddConvention<INamingConventions, MockNamingConvention>()
            .AddType<MockObjectType>()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("test")
                    .Argument("arg", a => a.Type<GeoJsonMultiPointInputType>())
                    .Resolve("ghi"))
            .Create();

    private InputObjectType CreateInputType()
    {
        var schema = CreateSchema();
        return schema.GetType<InputObjectType>("GeoJSONMultiPointInput");
    }
}
