using System;
using System.Collections.Generic;
using HotChocolate.Language;
using NetTopologySuite.Geometries;
using Xunit;

namespace HotChocolate.Types.Spatial.Tests
{
    public class GeoJsonTypeSerializerTests
    {
        [Fact]
        public void TrySerializer_Null()
        {
            // arrange
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            // assert
            Assert.True(serializer.TrySerialize(null, out object? resultValue));
            Assert.Null(resultValue);
        }

        [Fact]
        public void TrySerializer_DifferentObject()
        {
            // arrange
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            // assert
            Assert.False(serializer.TrySerialize("", out object? resultValue));
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
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            // assert
            Assert.True(serializer.TrySerialize(value, out object? resultValue));
            Assert.Equal(stringValue, resultValue);
        }

        [Fact]
        public void IsInstanceOfType_Should_Throw_When_Null()
        {
            // arrange
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            // assert
            Assert.Throws<ArgumentNullException>(() => serializer.IsInstanceOfType(null!));
        }

        [Fact]
        public void IsInstanceOfType_Should_Pass_When_NullValueNode()
        {
            // arrange
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            // assert
            Assert.True(serializer.IsInstanceOfType(NullValueNode.Default));
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
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            // assert
            Assert.True(serializer.IsInstanceOfType(new EnumValueNode(value)));
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
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            // assert
            Assert.True(serializer.IsInstanceOfType(new StringValueNode(value)));
        }

        [Fact]
        public void ParseLiteral_Should_Throw_When_Null()
        {
            // arrange
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            // assert
            Assert.Throws<ArgumentNullException>(() => serializer.ParseLiteral(null!));
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
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            object? resultValue = serializer.ParseLiteral(new EnumValueNode(stringValue));

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
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            object? resultValue = serializer.ParseLiteral(new StringValueNode(stringValue));

            // assert
            Assert.Equal(value, resultValue);
        }

        [Fact]
        public void ParseLiteral_Should_Parse_NullValueNode()
        {
            // arrange
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            object? resultValue = serializer.ParseLiteral(NullValueNode.Default);

            // assert
            Assert.Null(resultValue);
        }

        [Fact]
        public void ParseValue_Should_Parse_Null()
        {
            // arrange
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            IValueNode resultValue = serializer.ParseValue(null);

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
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            IValueNode resultValue = serializer.ParseValue(value);

            // assert
            var enumValue = Assert.IsType<EnumValueNode>(resultValue);
            Assert.Equal(stringValue, enumValue.Value);
        }


        [Fact]
        public void ParseValue_Should_Throw_OnInvalidValue()
        {
            // arrange
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act

            // assert
            Assert.Throws<GeoJsonSerializationException>(() => serializer.ParseValue(""));
        }

        [Fact]
        public void ParseResult_Should_Parse_Null()
        {
            // arrange
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            IValueNode resultValue = serializer.ParseResult(null);

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
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            IValueNode resultValue = serializer.ParseResult(value);

            // assert
            EnumValueNode enumValue = Assert.IsType<EnumValueNode>(resultValue);
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
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            IValueNode resultValue = serializer.ParseResult(new NameString(stringValue));

            // assert
            EnumValueNode enumValue = Assert.IsType<EnumValueNode>(resultValue);
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
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            IValueNode resultValue = serializer.ParseResult(stringValue);

            // assert
            EnumValueNode enumValue = Assert.IsType<EnumValueNode>(resultValue);
            Assert.Equal(stringValue, enumValue.Value);
        }


        [Fact]
        public void ParseResult_Should_Throw_OnInvalidValue()
        {
            // arrange
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            // assert
            Assert.Throws<GeoJsonSerializationException>(() => serializer.ParseResult(""));
        }

        [Fact]
        public void IsInstanceOfType_Null()
        {
            // arrange
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            // assert
            Assert.True(serializer.IsInstanceOfType((object?)null));
        }

        [Theory]
        [InlineData(GeoJsonGeometryType.Point)]
        [InlineData(GeoJsonGeometryType.MultiPoint)]
        [InlineData(GeoJsonGeometryType.LineString)]
        [InlineData(GeoJsonGeometryType.MultiLineString)]
        [InlineData(GeoJsonGeometryType.Polygon)]
        [InlineData(GeoJsonGeometryType.MultiPolygon)]
        public void IsInstanceOfType_GeometryType(GeoJsonGeometryType type)
        {
            // arrange
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            // assert
            Assert.True(serializer.IsInstanceOfType(type));
        }

        [Fact]
        public void IsInstanceOfType_Should_BeFalse_When_Other_Object()
        {
            // arrange
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            // assert
            Assert.False(serializer.IsInstanceOfType(""));
        }

        [Fact]
        public void TryDeserialize_Null()
        {
            // arrange
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            // assert
            Assert.True(serializer.TryDeserialize(null, out object? resultValue));
            Assert.Null(resultValue);
        }

        [Fact]
        public void TryDeserialize_DifferentObject()
        {
            // arrange
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            // assert
            Assert.False(serializer.TryDeserialize("", out object? resultValue));
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
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            // assert
            Assert.True(serializer.TryDeserialize(value, out object? resultValue));
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
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            // assert
            Assert.True(serializer.TryDeserialize(stringValue, out object? resultValue));
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
            string stringValue)
        {
            // arrange
            GeoJsonTypeSerializer serializer = GeoJsonTypeSerializer.Default;

            // act
            // assert
            Assert.True(
                serializer.TryDeserialize(new NameString(stringValue), out object? resultValue));
            Assert.Equal(value, resultValue);
        }
    }
}
