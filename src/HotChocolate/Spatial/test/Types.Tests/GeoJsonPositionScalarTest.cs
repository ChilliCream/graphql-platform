using System;
using System.Collections.Generic;
using HotChocolate.Language;
using NetTopologySuite.Geometries;
using Xunit;

namespace HotChocolate.Types.Spatial
{
    public class GeoJsonPositionScalarTest
    {
        [Fact]
        public void IsInstanceOfType_Valid2ElementCoordinate_True()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var coordinate = new ListValueNode(new IntValueNode(1), new FloatValueNode(1.2));

            // act
            bool? result = type.IsInstanceOfType(coordinate);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_Valid3ElementCoordinate_True()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var coordinate = new ListValueNode(
                new IntValueNode(1),
                new FloatValueNode(1.2),
                new FloatValueNode(3.2));

            // act
            bool? result = type.IsInstanceOfType(coordinate);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_NullType_True()
        {
            // arrange
            var type = new GeoJsonPositionType();
            NullValueNode coordinate = NullValueNode.Default;

            // act
            bool? result = type.IsInstanceOfType(coordinate);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_Invalid2ElementCoordinate_False()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var coordinate = new ListValueNode(new StringValueNode("1"), new FloatValueNode(1.2));

            // act
            bool? result = type.IsInstanceOfType(coordinate);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsInstanceOfType_Invalid3ElementCoordinate_False()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var coordinate = new ListValueNode(
                new IntValueNode(1),
                new FloatValueNode(1.2),
                new StringValueNode("2"));

            // act
            bool? result = type.IsInstanceOfType(coordinate);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsInstanceOfType_List2ElementCoordinate_False()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var coordinate = new ListValueNode(
                new ListValueNode(new FloatValueNode(1.1), new FloatValueNode(1.2)));

            // act
            var result = type.IsInstanceOfType(coordinate);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsInstanceOfType_2ListElementCoordinate_False()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var coordinate = new ListValueNode(
                new ListValueNode(new FloatValueNode(1.1), new FloatValueNode(1.2)),
                new ListValueNode(new FloatValueNode(1.1), new FloatValueNode(1.2)));

            // act
            var result = type.IsInstanceOfType(coordinate);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsInstanceOfType_Invalid4ElementCoordinate_False()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var coordinate = new ListValueNode(
                new IntValueNode(1),
                new IntValueNode(2),
                new IntValueNode(3),
                new IntValueNode(4));

            // act
            bool? result = type.IsInstanceOfType(coordinate);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void ParseLiteral_Null_Throws()
        {
            // arrange
            var type = new GeoJsonPositionType();
            IValueNode? coordinate = null;

            // act
            // assert
            Assert.Throws<SerializationException>(() => type.ParseLiteral(coordinate!));
        }

        [Fact]
        public void ParseLiteral_NullType_Null()
        {
            // arrange
            var type = new GeoJsonPositionType();
            NullValueNode coordinate = NullValueNode.Default;

            // act
            object? result = type.ParseLiteral(coordinate);

            // assert
            Assert.Null(result);
        }

        [Fact]
        public void ParseLiteral_With_2Valid_Coordinates()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var coordinate = new ListValueNode(
                new FloatValueNode(1.0),
                new IntValueNode(2));

            // act
            object? result = type.ParseLiteral(coordinate);

            // assert
            Assert.Equal(1.0, Assert.IsType<Coordinate>(result).X);
            Assert.Equal(2, Assert.IsType<Coordinate>(result).Y);
        }

        [Fact]
        public void ParseLiteral_With_3Valid_Coordinates()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var coordinate = new ListValueNode(
                new FloatValueNode(1.0),
                new FloatValueNode(2.2),
                new IntValueNode(100)
            );

            // act
            object? result = type.ParseLiteral(coordinate);

            // assert
            Assert.Equal(1.0, Assert.IsType<CoordinateZ>(result).X);
            Assert.Equal(2.2, Assert.IsType<CoordinateZ>(result).Y);
            Assert.Equal(100, Assert.IsType<CoordinateZ>(result).Z);
        }

        [Fact]
        public void ParseLiteral_With_2Invalid_Coordinates_Throws()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var coordinate = new ListValueNode(
                new FloatValueNode(1.0),
                new StringValueNode("2.2")
            );

            // act
            // assert
            Assert.Throws<SerializationException>(() => type.ParseLiteral(coordinate));
        }

        [Fact]
        public void ParseLiteral_With_3Invalid_Coordinates_Throws()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var coordinate = new ListValueNode(
                new FloatValueNode(1.0),
                new IntValueNode(10),
                new StringValueNode("2.2")
            );

            // act
            // assert
            Assert.Throws<SerializationException>(() => type.ParseLiteral(coordinate));
        }

        [Fact]
        public void ParseLiteral_With_Invalid_Coordinates_Throws()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var coordinate = new StringValueNode("2.2");

            // act
            // assert
            Assert.Throws<SerializationException>(() => type.ParseLiteral(coordinate));
        }

        [Fact]
        public void ParseValue_With_Noncoordinate_Throws()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var item = "this is not a coordinate";

            // act
            // assert
            Assert.Throws<SerializationException>(() => type.ParseValue(item));
        }

        [Fact]
        public void ParseValue_With_Null()
        {
            // arrange
            var type = new GeoJsonPositionType();

            // act
            IValueNode result = type.ParseValue(null);

            // assert
            Assert.Null(Assert.IsType<NullValueNode>(result).Value);
        }

        [Fact]
        public void ParseValue_With_2Valid_Coordinates()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var coordinate = new Coordinate(1.1, 2.2);

            // act
            IValueNode result = type.ParseValue(coordinate);

            // assert
            Assert.Equal("1.1", Assert.IsType<ListValueNode>(result).Items[0].Value);
            Assert.Equal("2.2", Assert.IsType<ListValueNode>(result).Items[1].Value);
        }

        [Fact]
        public void ParseValue_With_3Valid_Coordinates()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var coordinate = new CoordinateZ(1.1, 2.2, 3.3);

            // act
            IValueNode result = type.ParseValue(coordinate);

            // assert
            Assert.Equal("1.1", Assert.IsType<ListValueNode>(result).Items[0].Value);
            Assert.Equal("2.2", Assert.IsType<ListValueNode>(result).Items[1].Value);
            Assert.Equal("3.3", Assert.IsType<ListValueNode>(result).Items[2].Value);
        }

        [Fact]
        public void TryDeserialize_With_Null()
        {
            // arrange
            var type = new GeoJsonPositionType();
            object? input = null;

            // act
            var result = type.TrySerialize(input, out object? value);

            // assert
            Assert.True(result);
            Assert.Null(value);
        }

        [Fact]
        public void TryDeserialize_With_Non_List()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var input = "not null and not a list";

            // act
            var result = type.TryDeserialize(input, out object? value);

            // assert
            Assert.False(result);
            Assert.Null(value);
        }

        [Fact]
        public void TryDeserialize_With_Too_Many_Elements()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var input = new List<object> {"1", "2", "3", "4"};

            // act
            var result = type.TryDeserialize(input, out object? value);

            // assert
            Assert.False(result);
            Assert.Null(value);
        }

        [Fact]
        public void TryDeserialize_With_Too_Few_Elements()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var input = new List<object> {1};

            // act
            var result = type.TryDeserialize(input, out object? value);

            // assert
            Assert.False(result);
            Assert.Null(value);
        }

        [Fact]
        public void TryDeserialize_With_Invalid_Element_Type()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var input = new List<object> {1, "a"};

            // act
            var result = type.TryDeserialize(input, out object? value);

            // assert
            Assert.False(result);
            Assert.Null(value);
        }

        [Fact]
        public void TryDeserialize_With_Invalid_Element_Type2()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var input = new List<object> {1, DateTime.Now};

            // act
            var result = type.TryDeserialize(input, out object? value);

            // assert
            Assert.False(result);
            Assert.Null(value);
        }

        [Fact]
        public void TryDeserialize_With_Invalid_Element_Type3()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var input = new List<object> {1, double.PositiveInfinity};

            // act
            var result = type.TryDeserialize(input, out object? value);

            // assert
            Assert.False(result);
            Assert.Null(value);
        }

        [Fact]
        public void TryDeserialize_With_Invalid_3rdElement_Type()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var input = new List<object> {1, 2, "a"};

            // act
            var result = type.TryDeserialize(input, out object? value);

            // assert
            Assert.False(result);
            Assert.Null(value);
        }

        [Fact]
        public void TryDeserialize_With_Invalid_3rdElement_Type2()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var input = new List<object> {1, 2, 'a'};

            // act
            var result = type.TryDeserialize(input, out object? value);

            // assert
            Assert.False(result);
            Assert.Null(value);
        }

        [Fact]
        public void TryDeserialize_With_Invalid_3rdElement_Type3()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var input = new List<object> {1, 2, double.NegativeInfinity};

            // act
            var result = type.TryDeserialize(input, out object? value);

            // assert
            Assert.False(result);
            Assert.Null(value);
        }

        [Fact]
        public void TryDeserialize_With_Valid_2Elements()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var input = new List<object> {1, 2};

            // act
            var result = type.TryDeserialize(input, out object? value);

            // assert
            Assert.True(result);
            Assert.Equal(1, Assert.IsType<Coordinate>(value).X);
            Assert.Equal(2, Assert.IsType<Coordinate>(value).Y);
        }

        [Fact]
        public void TryDeserialize_With_Valid_3Elements()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var input = new List<object> {1, 2, 0};

            // act
            var result = type.TryDeserialize(input, out object? value);

            // assert
            Assert.True(result);
            Assert.Equal(1, Assert.IsType<CoordinateZ>(value).X);
            Assert.Equal(2, Assert.IsType<CoordinateZ>(value).Y);
            Assert.Equal(0, Assert.IsType<CoordinateZ>(value).Z);
        }

        [Fact]
        public void TrySerialize_With_Invalid_Object()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var input = "not a coordinate";

            // act
            var result = type.TrySerialize(input, out object? value);

            // assert
            Assert.False(result);
            Assert.Null(value);
        }

        [Fact]
        public void TrySerialize_With_Valid_2dCoordinate()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var input = new Coordinate(1, 2);

            // act
            var result = type.TrySerialize(input, out object? value);

            // assert
            Assert.True(result);
            Assert.Equal(2, Assert.IsType<double[]>(value).Length);
            Assert.Equal(new[] {1D, 2D}, Assert.IsType<double[]>(value));
        }

        [Fact]
        public void TrySerialize_With_Valid_3dCoordinate()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var input = new CoordinateZ(1, 2, 100);

            // act
            var result = type.TrySerialize(input, out object? value);

            // assert
            Assert.True(result);
            Assert.Equal(3, Assert.IsType<double[]>(value).Length);
            Assert.Equal(new[] {1D, 2D, 100D}, Assert.IsType<double[]>(value));
        }

        [Fact]
        public void TrySerialize_With_Nan_3dCoordinate()
        {
            // arrange
            var type = new GeoJsonPositionType();
            var input = new CoordinateZ(1, 2, double.NaN);

            // act
            var result = type.TrySerialize(input, out object? value);

            // assert
            Assert.True(result);
            Assert.Equal(2, Assert.IsType<double[]>(value).Length);
            Assert.Equal(new[] {1D, 2D}, Assert.IsType<double[]>(value));
        }
    }
}
