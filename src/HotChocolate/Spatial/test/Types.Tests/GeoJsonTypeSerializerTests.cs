using HotChocolate.Language;
using HotChocolate.Types.Spatial.Serialization;
using Moq;

namespace HotChocolate.Types.Spatial;

public class GeoJsonTypeSerializerTests
{
    [Fact]
    public void IsValueCompatible_Should_Throw_When_Null()
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        // assert
        Assert.Throws<ArgumentNullException>(
            () => serializer.IsValueCompatible(type.Object, null!));
    }

    [Fact]
    public void IsValueCompatible_Should_Pass_When_NullValueNode()
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        // assert
        Assert.True(serializer.IsValueCompatible(type.Object, NullValueNode.Default));
    }

    [Theory]
    [InlineData("Point")]
    [InlineData("MultiPoint")]
    [InlineData("LineString")]
    [InlineData("MultiLineString")]
    [InlineData("Polygon")]
    [InlineData("MultiPolygon")]
    public void IsValueCompatible_Should_Pass_When_EnumValueNode(string value)
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        // assert
        Assert.True(serializer.IsValueCompatible(type.Object, new EnumValueNode(value)));
    }

    [Theory]
    [InlineData("Point")]
    [InlineData("MultiPoint")]
    [InlineData("LineString")]
    [InlineData("MultiLineString")]
    [InlineData("Polygon")]
    [InlineData("MultiPolygon")]
    public void IsValueCompatible_Should_Pass_When_StringValueNode(string value)
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        // assert
        Assert.True(serializer.IsValueCompatible(type.Object, new StringValueNode(value)));
    }

    [Fact]
    public void CoerceInputLiteral_Should_Throw_When_Null()
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        // assert
        Assert.Throws<ArgumentNullException>(() => serializer.CoerceInputLiteral(type.Object, null!));
    }

    [Theory]
    [InlineData(GeoJsonGeometryType.Point, "Point")]
    [InlineData(GeoJsonGeometryType.MultiPoint, "MultiPoint")]
    [InlineData(GeoJsonGeometryType.LineString, "LineString")]
    [InlineData(GeoJsonGeometryType.MultiLineString, "MultiLineString")]
    [InlineData(GeoJsonGeometryType.Polygon, "Polygon")]
    [InlineData(GeoJsonGeometryType.MultiPolygon, "MultiPolygon")]
    public void CoerceInputLiteral_Should_Parse_EnumValueNode(
        GeoJsonGeometryType value,
        string stringValue)
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        var resultValue = serializer.CoerceInputLiteral(type.Object, new EnumValueNode(stringValue));

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
    public void CoerceInputLiteral_Should_Parse_StringValueNode(
        GeoJsonGeometryType value,
        string stringValue)
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        var resultValue = serializer.CoerceInputLiteral(
            type.Object,
            new StringValueNode(stringValue));

        // assert
        Assert.Equal(value, resultValue);
    }

    [Fact]
    public void CoerceInputLiteral_Should_Parse_NullValueNode()
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        var resultValue = serializer.CoerceInputLiteral(type.Object, NullValueNode.Default);

        // assert
        Assert.Null(resultValue);
    }

    [Fact]
    public void ValueToLiteral_Should_Parse_Null()
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        var resultValue = serializer.ValueToLiteral(type.Object, null);

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
    public void ValueToLiteral_Should_Parse_EnumValue(
        GeoJsonGeometryType value,
        string stringValue)
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        var resultValue = serializer.ValueToLiteral(type.Object, value);

        // assert
        var enumValue = Assert.IsType<EnumValueNode>(resultValue);
        Assert.Equal(stringValue, enumValue.Value);
    }

    [Fact]
    public void ValueToLiteral_Should_Throw_OnInvalidValue()
    {
        // arrange
        var type = new Mock<IType>();
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        // assert
        Assert.Throws<LeafCoercionException>(
            () => serializer.ValueToLiteral(type.Object, ""));
    }

    [Theory]
    [InlineData("Point")]
    [InlineData("MultiPoint")]
    [InlineData("LineString")]
    [InlineData("MultiLineString")]
    [InlineData("Polygon")]
    [InlineData("MultiPolygon")]
    public void TryParseString_Should_Parse_GeometryTypeName(string typeName)
    {
        // arrange
        var serializer = GeoJsonTypeSerializer.Default;

        // act
        var success = serializer.TryParseString(typeName, out var resultValue);

        // assert
        Assert.True(success);
    }
}
