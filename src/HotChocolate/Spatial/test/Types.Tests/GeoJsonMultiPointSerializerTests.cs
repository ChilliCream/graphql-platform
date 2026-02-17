using HotChocolate.Language;
using HotChocolate.Utilities;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.WellKnownTypeNames;

namespace HotChocolate.Types.Spatial;

public class GeoJsonMultiPointSerializerTests
{
    private readonly IValueNode _coordinatesSyntaxNode = new ListValueNode(
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

    private readonly Geometry _geometry = new MultiPoint(
    [
        new Point(new Coordinate(10, 40)),
        new Point(new Coordinate(40, 30)),
        new Point(new Coordinate(20, 20)),
        new Point(new Coordinate(30, 10))
    ]);

    private const string GeometryType = "MultiPoint";

    [Theory]
    [InlineData(MultiPointInputName)]
    [InlineData(GeometryTypeName)]
    public void ParseLiteral_Should_Pass_When_NullValueNode(string typeName)
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType(typeName);

        // act
        // assert
        Assert.Null(inputParser.ParseLiteral(NullValueNode.Default, type));
    }

    [Theory]
    [InlineData(MultiPointInputName)]
    [InlineData(GeometryTypeName)]
    public void ParseLiteral_Should_Throw_When_NotObjectValueNode(string typeName)
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType(typeName);

        // act
        // assert
        Assert.Throws<LeafCoercionException>(
            () => inputParser.ParseLiteral(new ListValueNode(), type));
    }

    [Theory]
    [InlineData(MultiPointInputName)]
    [InlineData(GeometryTypeName)]
    public void ParseLiteral_Should_Pass_When_CorrectGeometry(string typeName)
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType(typeName);
        var typeField = new ObjectFieldNode(WellKnownFields.TypeFieldName, new EnumValueNode(GeometryType));
        var coordField = new ObjectFieldNode(
            WellKnownFields.CoordinatesFieldName,
            _coordinatesSyntaxNode);
        var crsField = new ObjectFieldNode(WellKnownFields.CrsFieldName, 26912);
        var valueNode = new ObjectValueNode(typeField, coordField, crsField);

        // act
        var parsedResult = inputParser.ParseLiteral(valueNode, type);

        // assert
        AssertGeometry(parsedResult, 26912);
    }

    [Theory]
    [InlineData(MultiPointInputName)]
    [InlineData(GeometryTypeName)]
    public void ParseLiteral_Should_Throw_When_NoGeometryType(string typeName)
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType(typeName);
        var coordField = new ObjectFieldNode(
            WellKnownFields.CoordinatesFieldName,
            _coordinatesSyntaxNode);
        var crsField = new ObjectFieldNode(WellKnownFields.CrsFieldName, 0);
        var valueNode = new ObjectValueNode(coordField, crsField);

        // act
        // assert
        Assert.Throws<LeafCoercionException>(() => inputParser.ParseLiteral(valueNode, type));
    }

    [Theory]
    [InlineData(MultiPointInputName)]
    [InlineData(GeometryTypeName)]
    public void ParseLiteral_Should_Throw_When_NoCoordinates(string typeName)
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType(typeName);
        var typeField = new ObjectFieldNode(WellKnownFields.TypeFieldName, new EnumValueNode(GeometryType));
        var crsField = new ObjectFieldNode(WellKnownFields.CrsFieldName, 0);
        var valueNode = new ObjectValueNode(typeField, crsField);

        // act
        // assert
        Assert.Throws<LeafCoercionException>(() => inputParser.ParseLiteral(valueNode, type));
    }

    [Theory]
    [InlineData(MultiPointInputName)]
    [InlineData(GeometryTypeName)]
    public void ParseLiteral_Should_Pass_When_NoCrs(string typeName)
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType(typeName);
        var typeField = new ObjectFieldNode(WellKnownFields.TypeFieldName, new EnumValueNode(GeometryType));
        var coordField = new ObjectFieldNode(
            WellKnownFields.CoordinatesFieldName,
            _coordinatesSyntaxNode);
        var valueNode = new ObjectValueNode(typeField, coordField);

        // act
        var parsedResult = inputParser.ParseLiteral(valueNode, type);

        // assert
        AssertGeometry(parsedResult);
    }

    [Theory]
    [InlineData(MultiPointInputName)]
    [InlineData(GeometryTypeName)]
    public void FormatValue_Should_Pass_When_NullValue(string typeName)
    {
        // arrange
        var inputFormatter = new InputFormatter();
        var type = CreateInputType(typeName);

        // act
        // assert
        Assert.Equal(NullValueNode.Default, inputFormatter.FormatValue(null, type));
    }

    [Theory]
    [InlineData(MultiPointInputName)]
    [InlineData(GeometryTypeName)]
    public void FormatValue_Should_Pass_When_Value(string typeName)
    {
        // arrange
        var inputFormatter = new InputFormatter();
        var type = CreateInputType(typeName);

        // act
        var literal = inputFormatter.FormatValue(_geometry, type);

        // assert
        literal.MatchSnapshot();
    }

    [Theory]
    [InlineData(MultiPointInputName)]
    [InlineData(GeometryTypeName)]
    public void FormatValue_Should_Throw_When_InvalidType(string typeName)
    {
        // arrange
        var inputFormatter = new InputFormatter();
        var type = CreateInputType(typeName);

        // act
        // assert
        Assert.Throws<LeafCoercionException>(() => inputFormatter.FormatValue("", type));
    }

    [Theory]
    [InlineData(MultiPointInputName)]
    [InlineData(GeometryTypeName)]
    public void MultiPoint_IsCoordinateValid_Should_Fail_When_Point(string typeName)
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType(typeName);
        var coords = new ListValueNode(
            new IntValueNode(30),
            new IntValueNode(10));
        var typeField = new ObjectFieldNode(WellKnownFields.TypeFieldName, new EnumValueNode(GeometryType));
        var coordField = new ObjectFieldNode(WellKnownFields.CoordinatesFieldName, coords);
        var valueNode = new ObjectValueNode(typeField, coordField);

        // act
        // assert
        Assert.Throws<LeafCoercionException>(() => inputParser.ParseLiteral(valueNode, type));
    }

    private Schema CreateSchema() => SchemaBuilder.New()
        .AddSpatialTypes()
        .AddQueryType(d => d
            .Name("Query")
            .Field("test")
            .Argument("arg", a => a.Type<StringType>())
            .Resolve("ghi"))
        .Create();

    private static void AssertGeometry(object? obj, int? crs = null)
    {
        Assert.Equal(4, Assert.IsType<MultiPoint>(obj).NumPoints);
        Assert.Equal(10, Assert.IsType<MultiPoint>(obj).Coordinates[0].X);
        Assert.Equal(40, Assert.IsType<MultiPoint>(obj).Coordinates[0].Y);
        Assert.Equal(40, Assert.IsType<MultiPoint>(obj).Coordinates[1].X);
        Assert.Equal(30, Assert.IsType<MultiPoint>(obj).Coordinates[1].Y);
        Assert.Equal(20, Assert.IsType<MultiPoint>(obj).Coordinates[2].X);
        Assert.Equal(20, Assert.IsType<MultiPoint>(obj).Coordinates[2].Y);
        Assert.Equal(30, Assert.IsType<MultiPoint>(obj).Coordinates[3].X);
        Assert.Equal(10, Assert.IsType<MultiPoint>(obj).Coordinates[3].Y);

        if (crs is not null)
        {
            Assert.Equal(crs, Assert.IsType<MultiPoint>(obj).SRID);
        }
    }

    private IInputTypeDefinition CreateInputType(string typeName)
    {
        return CreateSchema().Types.GetType<IInputTypeDefinition>(typeName);
    }
}
