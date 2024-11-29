using HotChocolate.Language;
using HotChocolate.Utilities;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.WellKnownTypeNames;

namespace HotChocolate.Types.Spatial;

public class GeoJsonPointSerializerTests
{
    private readonly IValueNode _coordinatesSyntaxNode = new ListValueNode(
        new IntValueNode(30),
        new IntValueNode(10));

    private readonly Geometry _geometry = new Point(new Coordinate(30, 10));

    private readonly string _geometryType = "Point";

    private readonly object _geometryParsed = new[] { 30.0, 10.0, };

    [Theory]
    [InlineData(GeometryTypeName)]
    public void Serialize_Should_Pass_When_SerializeNullValue(string typeName)
    {
        // arrange
        var type = CreateLeafType(typeName);

        // act
        // assert
        Assert.Null(type.Serialize(null));
    }

    [Theory]
    [InlineData(GeometryTypeName)]
    public void Serialize_Should_Pass_When_Dictionary(string typeName)
    {
        // arrange
        var type = CreateLeafType(typeName);
        var dictionary = new Dictionary<string, object>();

        // act
        var result = type.Serialize(dictionary);

        // assert
        Assert.Equal(dictionary, result);
    }

    [Theory]
    [InlineData(GeometryTypeName)]
    public void Serialize_Should_Pass_When_SerializeGeometry(string typeName)
    {
        // arrange
        var type = CreateLeafType(typeName);

        // act
        var result = type.Serialize(_geometry);

        // assert
        result.MatchSnapshot();
    }

    [Theory]
    [InlineData(GeometryTypeName)]
    public void Serialize_Should_Throw_When_InvalidObjectShouldThrow(string typeName)
    {
        // arrange
        var type = CreateLeafType(typeName);

        // act
        // assert
        Assert.Throws<SerializationException>(() => type.Serialize(""));
    }

    [Theory]
    [InlineData(PointInputName)]
    [InlineData(GeometryTypeName)]
    public void IsInstanceOfType_Should_Throw_When_Null(string typeName)
    {
        // arrange
        var type = CreateInputType(typeName);

        // act
        // assert
        Assert.Throws<ArgumentNullException>(() => type.IsInstanceOfType(null!));
    }

    [Theory]
    [InlineData(PointInputName)]
    public void IsInstanceOfType_Should_Pass_When_ObjectValueNode(string typeName)
    {
        // arrange
        var type = CreateInputType(typeName);

        // act
        // assert
        Assert.True(type.IsInstanceOfType(new ObjectValueNode()));
    }

    [Theory]
    [InlineData(PointInputName)]
    [InlineData(GeometryTypeName)]
    public void IsInstanceOfType_Should_Pass_When_NullValueNode(string typeName)
    {
        // arrange
        var type = CreateInputType(typeName);

        // act
        // assert
        Assert.True(type.IsInstanceOfType(NullValueNode.Default));
    }

    [Theory]
    [InlineData(GeometryTypeName)]
    public void IsInstanceOfType_Should_Fail_When_DifferentGeoJsonObject(string typeName)
    {
        // arrange
        var type = CreateLeafType(typeName);

        // act
        // assert
        Assert.False(
            type.IsInstanceOfType(
                GeometryFactory.Default.CreateGeometryCollection(
                    [new Point(1, 2),])));
    }

    [Theory]
    [InlineData(GeometryTypeName)]
    public void IsInstanceOfType_Should_Pass_When_GeometryOfType(string typeName)
    {
        // arrange
        var type = CreateLeafType(typeName);

        // act
        // assert
        Assert.True(type.IsInstanceOfType(_geometry));
    }

    [Theory]
    [InlineData(GeometryTypeName)]
    public void IsInstanceOfType_Should_Fail_When_NoGeometry(string typeName)
    {
        // arrange
        var type = CreateLeafType(typeName);

        // act
        // assert
        Assert.False(type.IsInstanceOfType("foo"));
    }

    [Theory]
    [InlineData(GeometryTypeName)]
    public void ParseLiteral_Should_Pass_When_NullValueNode(string typeName)
    {
        // arrange
        var type = CreateLeafType(typeName);

        // act
        // assert
        Assert.Null(type.ParseLiteral(NullValueNode.Default));
    }

    [Theory]
    [InlineData(PointInputName)]
    [InlineData(GeometryTypeName)]
    public void ParseLiteral_Should_Throw_When_NotObjectValueNode(string typeName)
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType(typeName);

        // act
        // assert
        Assert.Throws<SerializationException>(
            () => inputParser.ParseLiteral(new ListValueNode(), type));
    }

    [Theory]
    [InlineData(PointInputName)]
    [InlineData(GeometryTypeName)]
    public void ParseLiteral_Should_Pass_When_CorrectGeometry(string typeName)
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType(typeName);
        var typeField = new ObjectFieldNode(WellKnownFields.TypeFieldName, _geometryType);
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
    [InlineData(PointInputName, Skip = "TODO: Pascal needs to review this one")]
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
        Assert.Throws<SerializationException>(() => inputParser.ParseLiteral(valueNode, type));
    }

    [Theory]
    [InlineData(PointInputName)]
    [InlineData(GeometryTypeName)]
    public void ParseLiteral_Should_Throw_When_NoCoordinates(string typeName)
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType(typeName);
        var typeField = new ObjectFieldNode(WellKnownFields.TypeFieldName, _geometryType);
        var crsField = new ObjectFieldNode(WellKnownFields.CrsFieldName, 0);
        var valueNode = new ObjectValueNode(typeField, crsField);

        // act
        // assert
        Assert.Throws<SerializationException>(() => inputParser.ParseLiteral(valueNode, type));
    }

    [Theory]
    [InlineData(PointInputName)]
    [InlineData(GeometryTypeName)]
    public void ParseLiteral_Should_Pass_When_NoCrs(string typeName)
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType(typeName);
        var typeField = new ObjectFieldNode(WellKnownFields.TypeFieldName, _geometryType);
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
    [InlineData(PointInputName)]
    [InlineData(GeometryTypeName)]
    public void ParseResult_Should_Pass_When_NullValue(string typeName)
    {
        // arrange
        var inputFormatter = new InputFormatter();
        var type = CreateInputType(typeName);

        // act
        // assert
        Assert.Equal(NullValueNode.Default, inputFormatter.FormatValue(null, type));
    }

    [Theory]
    [InlineData(GeometryTypeName)]
    public void ParseResult_Should_Pass_When_Serialized(string typeName)
    {
        // arrange
        var type = CreateLeafType(typeName);
        var serialized = type.Serialize(_geometry);

        // act
        var literal = type.ParseResult(serialized);

        // assert
        literal.MatchSnapshot();
    }

    [Theory]
    [InlineData(PointInputName)]
    [InlineData(GeometryTypeName)]
    public void ParseResult_Should_Pass_When_Value(string typeName)
    {
        // arrange
        var inputFormatter = new InputFormatter();
        var type = CreateInputType(typeName);

        // act
        var literal = inputFormatter.FormatResult(_geometry, type);

        // assert
        literal.MatchSnapshot();
    }

    [Theory]
    [InlineData(PointInputName)]
    [InlineData(GeometryTypeName)]
    public void ParseResult_Should_Throw_When_InvalidType(string typeName)
    {
        // arrange
        var inputFormatter = new InputFormatter();
        var type = CreateInputType(typeName);

        // act
        // assert
        Assert.Throws<SerializationException>(() => inputFormatter.FormatResult("", type));
    }

    [Theory]
    [InlineData(PointInputName)]
    [InlineData(GeometryTypeName)]
    public void ParseValue_Should_Pass_When_NullValue(string typeName)
    {
        // arrange
        var inputFormatter = new InputFormatter();
        var type = CreateInputType(typeName);

        // act
        // assert
        Assert.Equal(NullValueNode.Default, inputFormatter.FormatValue(null, type));
    }

    [Theory]
    [InlineData(GeometryTypeName)]
    public void ParseValue_Should_Pass_When_Serialized(string typeName)
    {
        // arrange
        var type = CreateLeafType(typeName);
        var serialized = type.Serialize(_geometry);

        // act
        var literal = type.ParseValue(serialized);

        // assert
        literal.MatchSnapshot();
    }

    [Theory]
    [InlineData(PointInputName)]
    [InlineData(GeometryTypeName)]
    public void ParseValue_Should_Pass_When_Value(string typeName)
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
    [InlineData(PointInputName)]
    [InlineData(GeometryTypeName)]
    public void ParseValue_Should_Throw_When_InvalidType(string typeName)
    {
        // arrange
        var inputFormatter = new InputFormatter();
        var type = CreateInputType(typeName);

        // act
        // assert
        Assert.Throws<SerializationException>(() => inputFormatter.FormatValue("", type));
    }

    [Theory]
    [InlineData(PointInputName)]
    [InlineData(GeometryTypeName)]
    public void Deserialize_Should_Pass_When_SerializeNullValue(string typeName)
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType(typeName);

        // act
        // assert
        Assert.Null(inputParser.ParseResult(null, type));
    }

    [Theory]
    [InlineData(GeometryTypeName)]
    public void Deserialize_Should_Pass_When_PassedSerializedResult(string typeName)
    {
        // arrange
        var type = CreateLeafType(typeName);
        var serialized = type.Serialize(_geometry);

        // act
        var result = type.Deserialize(serialized);

        // assert
        Assert.True(Assert.IsAssignableFrom<Geometry>(result).Equals(_geometry));
    }

    [Theory]
    [InlineData(PointInputName)]
    [InlineData(GeometryTypeName)]
    public void Deserialize_Should_Pass_When_SerializeGeometry(string typeName)
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType(typeName);

        // act
        var result = inputParser.ParseResult(_geometry, type);

        // assert
        Assert.Equal(result, _geometry);
    }

    [Theory]
    [InlineData(PointInputName)]
    [InlineData(GeometryTypeName)]
    public void Deserialize_Should_Throw_When_InvalidType(string typeName)
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType(typeName);

        // act
        // assert
        Assert.Throws<SerializationException>(() => inputParser.ParseResult("", type));
    }

    [Theory]
    [InlineData(PointInputName)]
    [InlineData(GeometryTypeName)]
    public void Deserialize_Should_Pass_When_AllFieldsInDictionary(string typeName)
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType(typeName);
        var serialized = new Dictionary<string, object>
            {
                { WellKnownFields.TypeFieldName, _geometryType },
                { WellKnownFields.CoordinatesFieldName, _geometryParsed },
                { WellKnownFields.CrsFieldName, 26912 },
            };

        // act
        var result = inputParser.ParseResult(serialized, type);

        // assert
        AssertGeometry(result, 26912);
    }

    [Theory]
    [InlineData(PointInputName)]
    [InlineData(GeometryTypeName)]
    public void Deserialize_Should_Pass_When_CrsIsMissing(string typeName)
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType(typeName);
        var serialized = new Dictionary<string, object>
            {
                { WellKnownFields.TypeFieldName, _geometryType },
                { WellKnownFields.CoordinatesFieldName, _geometryParsed },
            };

        // act
        var result = inputParser.ParseResult(serialized, type);

        // assert
        AssertGeometry(result);
    }

    [Theory]
    [InlineData(PointInputName, Skip = "TODO: Pascal needs to review this one")]
    [InlineData(GeometryTypeName)]
    public void Deserialize_Should_Fail_WhenTypeNameIsMissing(string typeName)
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType(typeName);
        var serialized = new Dictionary<string, object>
            {
                { WellKnownFields.CoordinatesFieldName, _geometryParsed },
                { WellKnownFields.CrsFieldName, new IntValueNode(0) },
            };

        // act
        // assert
        Assert.Throws<SerializationException>(() => inputParser.ParseResult(serialized, type));
    }

    [Theory]
    [InlineData(PointInputName, Skip = "TODO: Pascal needs to review this one")]
    [InlineData(GeometryTypeName)]
    public void Deserialize_Should_When_CoordinatesAreMissing(string typeName)
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType(typeName);
        var serialized = new Dictionary<string, object>
            {
                { WellKnownFields.TypeFieldName, _geometryType },
                { WellKnownFields.CrsFieldName, new IntValueNode(0) },
            };

        // act
        // assert
        Assert.Throws<SerializationException>(() => inputParser.ParseResult(serialized, type));
    }

    [Theory]
    [InlineData(PointInputName)]
    [InlineData(GeometryTypeName)]
    public void Point_IsCoordinateValid_Should_Fail_When_MultiArray(string typeName)
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = CreateInputType(typeName);
        var coords = new ListValueNode(
            new ListValueNode(
                new IntValueNode(30),
                new IntValueNode(10)));
        var typeField = new ObjectFieldNode(WellKnownFields.TypeFieldName, _geometryType);
        var coordField = new ObjectFieldNode(WellKnownFields.CoordinatesFieldName, coords);
        var valueNode = new ObjectValueNode(typeField, coordField);

        // act
        // assert
        Assert.Throws<SerializationException>(() => inputParser.ParseLiteral(valueNode, type));
    }

    private ISchema CreateSchema() => SchemaBuilder.New()
        .AddSpatialTypes()
        .AddQueryType(d => d
            .Name("Query")
            .Field("test")
            .Argument("arg", a => a.Type<StringType>())
            .Resolve("ghi"))
        .Create();

    private static void AssertGeometry(object? obj, int? crs = null)
    {
        Assert.Equal(30, Assert.IsType<Point>(obj).X);
        Assert.Equal(10, Assert.IsType<Point>(obj).Y);

        if (crs is not null)
        {
            Assert.Equal(crs, Assert.IsType<Point>(obj).SRID);
        }
    }

    private INamedInputType CreateInputType(string typeName)
    {
        return CreateSchema().GetType<INamedInputType>(typeName);
    }

    private ILeafType CreateLeafType(string typeName)
    {
        return CreateSchema().GetType<ILeafType>(typeName);
    }
}
