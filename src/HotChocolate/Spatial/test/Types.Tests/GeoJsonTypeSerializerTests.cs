using HotChocolate.Language;
using HotChocolate.Types.Spatial.Serialization;
using Moq;

namespace HotChocolate.Types.Spatial;

public class GeoJsonTypeSerializerTests
{
    [Fact]
    public void TrySerializer_Null()
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        // assert
        Assert.True(serializer.TrySerialize(type.Object, null, out var resultValue));
        Assert.Null(resultValue);
    }

    [Fact]
    public void TrySerializer_DifferentObject()
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        // assert
        Assert.False(serializer.TrySerialize(type.Object, "", out var resultValue));
        Assert.Null(resultValue);
    }

    [Theory]
    [InlineData(GeoJsonGeometryType.Point, "Point")]
    [InlineData(GeoJsonGeometryType.MultiPoint, "MultiPoint")]
    [InlineData(GeoJsonGeometryType.LineString, "LineString")]
    [InlineData(GeoJsonGeometryType.MultiLineString, "MultiLineString")]
    [InlineData(GeoJsonGeometryType.Polygon, "Polygon")]
    [InlineData(GeoJsonGeometryType.MultiPolygon, "MultiPolygon")]
    public void TrySerializer_Should_Serialize_Enum(
        GeoJsonGeometryType value,
        string stringValue)
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        // assert
        Assert.True(serializer.TrySerialize(type.Object, value, out var resultValue));
        Assert.Equal(stringValue, resultValue);
    }

    [Fact]
    public void IsInstanceOfType_Should_Throw_When_Null()
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        // assert
        Assert.Throws<ArgumentNullException>(
            () => serializer.IsInstanceOfType(type.Object, null!));
    }

    [Fact]
    public void IsInstanceOfType_Should_Pass_When_NullValueNode()
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        // assert
        Assert.True(serializer.IsInstanceOfType(type.Object, NullValueNode.Default));
    }

    [Theory]
    [InlineData("Point")]
    [InlineData("MultiPoint")]
    [InlineData("LineString")]
    [InlineData("MultiLineString")]
    [InlineData("Polygon")]
    [InlineData("MultiPolygon")]
    public void IsInstanceOfType_Should_Pass_When_EnumValueNode(string value)
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        // assert
        Assert.True(serializer.IsInstanceOfType(type.Object, new EnumValueNode(value)));
    }

    [Theory]
    [InlineData("Point")]
    [InlineData("MultiPoint")]
    [InlineData("LineString")]
    [InlineData("MultiLineString")]
    [InlineData("Polygon")]
    [InlineData("MultiPolygon")]
    public void IsInstanceOfType_Should_Pass_When_StringValueNode(string value)
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        // assert
        Assert.True(serializer.IsInstanceOfType(type.Object, new StringValueNode(value)));
    }

    [Fact]
    public void ParseLiteral_Should_Throw_When_Null()
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        // assert
        Assert.Throws<ArgumentNullException>(() => serializer.ParseLiteral(type.Object, null!));
    }

    [Theory]
    [InlineData(GeoJsonGeometryType.Point, "Point")]
    [InlineData(GeoJsonGeometryType.MultiPoint, "MultiPoint")]
    [InlineData(GeoJsonGeometryType.LineString, "LineString")]
    [InlineData(GeoJsonGeometryType.MultiLineString, "MultiLineString")]
    [InlineData(GeoJsonGeometryType.Polygon, "Polygon")]
    [InlineData(GeoJsonGeometryType.MultiPolygon, "MultiPolygon")]
    public void ParseLiteral_Should_Parse_EnumValueNode(
        GeoJsonGeometryType value,
        string stringValue)
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        var resultValue = serializer.ParseLiteral(type.Object, new EnumValueNode(stringValue));

        // assert
        Assert.Equal(value, resultValue);
    }

    [Theory]
    [InlineData(GeoJsonGeometryType.Point, "Point")]
    [InlineData(GeoJsonGeometryType.MultiPoint, "MultiPoint")]
    [InlineData(GeoJsonGeometryType.LineString, "LineString")]
    [InlineData(GeoJsonGeometryType.MultiLineString, "MultiLineString")]
    [InlineData(GeoJsonGeometryType.Polygon, "Polygon")]
    [InlineData(GeoJsonGeometryType.MultiPolygon, "MultiPolygon")]
    public void ParseLiteral_Should_Parse_StringValueNode(
        GeoJsonGeometryType value,
        string stringValue)
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        var resultValue = serializer.ParseLiteral(
            type.Object,
            new StringValueNode(stringValue));

        // assert
        Assert.Equal(value, resultValue);
    }

    [Fact]
    public void ParseLiteral_Should_Parse_NullValueNode()
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        var resultValue = serializer.ParseLiteral(type.Object, NullValueNode.Default);

        // assert
        Assert.Null(resultValue);
    }

    [Fact]
    public void ParseValue_Should_Parse_Null()
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        var resultValue = serializer.ParseValue(type.Object, null);

        // assert
        Assert.Equal(NullValueNode.Default, resultValue);
    }

    [Theory]
    [InlineData(GeoJsonGeometryType.Point, "Point")]
    [InlineData(GeoJsonGeometryType.MultiPoint, "MultiPoint")]
    [InlineData(GeoJsonGeometryType.LineString, "LineString")]
    [InlineData(GeoJsonGeometryType.MultiLineString, "MultiLineString")]
    [InlineData(GeoJsonGeometryType.Polygon, "Polygon")]
    [InlineData(GeoJsonGeometryType.MultiPolygon, "MultiPolygon")]
    public void ParseValue_Should_Parse_EnumValue(
        GeoJsonGeometryType value,
        string stringValue)
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        var resultValue = serializer.ParseValue(type.Object, value);

        // assert
        var enumValue = Assert.IsType<EnumValueNode>(resultValue);
        Assert.Equal(stringValue, enumValue.Value);
    }

    [Fact]
    public void ParseValue_Should_Throw_OnInvalidValue()
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        // assert
        Assert.Throws<SerializationException>(
            () => serializer.ParseValue(type.Object, ""));
    }

    [Fact]
    public void ParseResult_Should_Parse_Null()
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        var resultValue = serializer.ParseResult(type.Object, null);

        // assert
        Assert.Equal(NullValueNode.Default, resultValue);
    }

    [Theory]
    [InlineData(GeoJsonGeometryType.Point, "Point")]
    [InlineData(GeoJsonGeometryType.MultiPoint, "MultiPoint")]
    [InlineData(GeoJsonGeometryType.LineString, "LineString")]
    [InlineData(GeoJsonGeometryType.MultiLineString, "MultiLineString")]
    [InlineData(GeoJsonGeometryType.Polygon, "Polygon")]
    [InlineData(GeoJsonGeometryType.MultiPolygon, "MultiPolygon")]
    public void ParseResult_Should_Parse_EnumValue(
        GeoJsonGeometryType value,
        string stringValue)
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        var resultValue = serializer.ParseResult(type.Object, value);

        // assert
        var enumValue = Assert.IsType<EnumValueNode>(resultValue);
        Assert.Equal(stringValue, enumValue.Value);
    }

    [Theory]
    [InlineData("Point")]
    [InlineData("MultiPoint")]
    [InlineData("LineString")]
    [InlineData("MultiLineString")]
    [InlineData("Polygon")]
    [InlineData("MultiPolygon")]
    public void ParseResult_Should_Parse_NameString(string stringValue)
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        var resultValue = serializer.ParseResult(type.Object, stringValue);

        // assert
        var enumValue = Assert.IsType<EnumValueNode>(resultValue);
        Assert.Equal(stringValue, enumValue.Value);
    }

    [Theory]
    [InlineData("Point")]
    [InlineData("MultiPoint")]
    [InlineData("LineString")]
    [InlineData("MultiLineString")]
    [InlineData("Polygon")]
    [InlineData("MultiPolygon")]
    public void ParseResult_Should_Parse_String(string stringValue)
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        var resultValue = serializer.ParseResult(type.Object, stringValue);

        // assert
        var enumValue = Assert.IsType<EnumValueNode>(resultValue);
        Assert.Equal(stringValue, enumValue.Value);
    }

    [Fact]
    public void ParseResult_Should_Throw_OnInvalidValue()
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        // assert
        Assert.Throws<SerializationException>(
            () => serializer.ParseResult(type.Object, ""));
    }

    [Fact]
    public void IsInstanceOfType_Null()
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        // assert
        Assert.True(serializer.IsInstanceOfType(type.Object, (object?)null));
    }

    [Theory]
    [InlineData(GeoJsonGeometryType.Point)]
    [InlineData(GeoJsonGeometryType.MultiPoint)]
    [InlineData(GeoJsonGeometryType.LineString)]
    [InlineData(GeoJsonGeometryType.MultiLineString)]
    [InlineData(GeoJsonGeometryType.Polygon)]
    [InlineData(GeoJsonGeometryType.MultiPolygon)]
    public void IsInstanceOfType_GeometryType(GeoJsonGeometryType geometryType)
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        // assert
        Assert.True(serializer.IsInstanceOfType(type.Object, geometryType));
    }

    [Fact]
    public void IsInstanceOfType_Should_BeFalse_When_Other_Object()
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        // assert
        Assert.False(serializer.IsInstanceOfType(type.Object, ""));
    }

    [Fact]
    public void TryDeserialize_Null()
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        // assert
        Assert.True(serializer.TryDeserialize(type.Object, null, out var resultValue));
        Assert.Null(resultValue);
    }

    [Fact]
    public void TryDeserialize_DifferentObject()
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        // assert
        Assert.False(serializer.TryDeserialize(type.Object, "", out var resultValue));
        Assert.Null(resultValue);
    }

    [Theory]
    [InlineData(GeoJsonGeometryType.Point)]
    [InlineData(GeoJsonGeometryType.MultiPoint)]
    [InlineData(GeoJsonGeometryType.LineString)]
    [InlineData(GeoJsonGeometryType.MultiLineString)]
    [InlineData(GeoJsonGeometryType.Polygon)]
    [InlineData(GeoJsonGeometryType.MultiPolygon)]
    public void TryDeserialize_Should_Serialize_Enum(
        GeoJsonGeometryType value)
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        // assert
        Assert.True(serializer.TryDeserialize(type.Object, value, out var resultValue));
        Assert.Equal(value, resultValue);
    }

    [Theory]
    [InlineData(GeoJsonGeometryType.Point, "Point")]
    [InlineData(GeoJsonGeometryType.MultiPoint, "MultiPoint")]
    [InlineData(GeoJsonGeometryType.LineString, "LineString")]
    [InlineData(GeoJsonGeometryType.MultiLineString, "MultiLineString")]
    [InlineData(GeoJsonGeometryType.Polygon, "Polygon")]
    [InlineData(GeoJsonGeometryType.MultiPolygon, "MultiPolygon")]
    public void TryDeserialize_Should_Serialize_String(
        GeoJsonGeometryType value,
        string stringValue)
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        // assert
        Assert.True(serializer.TryDeserialize(type.Object, stringValue, out var resultValue));
        Assert.Equal(value, resultValue);
    }

    [Theory]
    [InlineData(GeoJsonGeometryType.Point, "Point")]
    [InlineData(GeoJsonGeometryType.MultiPoint, "MultiPoint")]
    [InlineData(GeoJsonGeometryType.LineString, "LineString")]
    [InlineData(GeoJsonGeometryType.MultiLineString, "MultiLineString")]
    [InlineData(GeoJsonGeometryType.Polygon, "Polygon")]
    [InlineData(GeoJsonGeometryType.MultiPolygon, "MultiPolygon")]
    public void TryDeserialize_Should_Serialize_NameString(
        GeoJsonGeometryType value,
        string typeName)
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        var success = serializer.TryDeserialize(type.Object, typeName, out var resultValue);

        // assert
        Assert.True(success);
        Assert.Equal(value, resultValue);
    }
}
