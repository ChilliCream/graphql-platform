using HotChocolate.Language;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial;

public class GeoJsonPositionScalarTest
{
    [Fact]
    public void IsValueCompatible_Valid2ElementCoordinate_True()
    {
        // arrange
        var type = new GeoJsonPositionType();
        var coordinate = new ListValueNode(
            new IntValueNode(1),
            new FloatValueNode(1.2));

        // act
        bool? result = type.IsValueCompatible(coordinate);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsValueCompatible_Valid3ElementCoordinate_True()
    {
        // arrange
        var type = new GeoJsonPositionType();
        var coordinate = new ListValueNode(
            new IntValueNode(1),
            new FloatValueNode(1.2),
            new FloatValueNode(3.2));

        // act
        bool? result = type.IsValueCompatible(coordinate);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void CoerceInputLiteral_Null_Throws()
    {
        var type = new GeoJsonPositionType();
        IValueNode? coordinate = null;

        Assert.Throws<LeafCoercionException>(() => type.CoerceInputLiteral(coordinate!));
    }

    [Fact]
    public void CoerceInputLiteral_With_2Valid_Coordinates()
    {
        var type = new GeoJsonPositionType();
        var coordinate = new ListValueNode(
            new FloatValueNode(1.0),
            new IntValueNode(2)
        );

        var result = type.CoerceInputLiteral(coordinate);

        Assert.Equal(1.0, Assert.IsType<Coordinate>(result).X);
        Assert.Equal(2, Assert.IsType<Coordinate>(result).Y);
    }

    [Fact]
    public void CoerceInputLiteral_With_3Valid_Coordinates()
    {
        var type = new GeoJsonPositionType();
        var coordinate = new ListValueNode(
            new FloatValueNode(1.0),
            new FloatValueNode(2.2),
            new IntValueNode(100)
        );

        var result = type.CoerceInputLiteral(coordinate);

        Assert.Equal(1.0, Assert.IsType<CoordinateZ>(result).X);
        Assert.Equal(2.2, Assert.IsType<CoordinateZ>(result).Y);
        Assert.Equal(100, Assert.IsType<CoordinateZ>(result).Z);
    }

    [Fact]
    public void CoerceInputLiteral_With_2Invalid_Coordinates_Throws()
    {
        var type = new GeoJsonPositionType();
        var coordinate = new ListValueNode(
            new FloatValueNode(1.0),
            new StringValueNode("2.2")
        );

        Assert.Throws<LeafCoercionException>(() => type.CoerceInputLiteral(coordinate));
    }

    [Fact]
    public void CoerceInputLiteral_With_3Invalid_Coordinates_Throws()
    {
        var type = new GeoJsonPositionType();
        var coordinate = new ListValueNode(
            new FloatValueNode(1.0),
            new IntValueNode(10),
            new StringValueNode("2.2")
        );

        Assert.Throws<LeafCoercionException>(() => type.CoerceInputLiteral(coordinate));
    }

    [Fact]
    public void CoerceInputLiteral_With_Invalid_Coordinates_Throws()
    {
        var type = new GeoJsonPositionType();
        var coordinate = new StringValueNode("2.2");

        Assert.Throws<LeafCoercionException>(() => type.CoerceInputLiteral(coordinate));
    }

    [Fact]
    public void ValueToLiteral_With_2Valid_Coordinates()
    {
        var type = new GeoJsonPositionType();
        var coordinate = new Coordinate(1.1, 2.2);

        var result = type.ValueToLiteral(coordinate);

        Assert.Equal("1.1", Assert.IsType<ListValueNode>(result).Items[0].Value);
        Assert.Equal("2.2", Assert.IsType<ListValueNode>(result).Items[1].Value);
    }

    [Fact]
    public void ValueToLiteral_With_3Valid_Coordinates()
    {
        var type = new GeoJsonPositionType();
        var coordinate = new CoordinateZ(1.1, 2.2, 3.3);

        var result = type.ValueToLiteral(coordinate);

        Assert.Equal("1.1", Assert.IsType<ListValueNode>(result).Items[0].Value);
        Assert.Equal("2.2", Assert.IsType<ListValueNode>(result).Items[1].Value);
        Assert.Equal("3.3", Assert.IsType<ListValueNode>(result).Items[2].Value);
    }

    [Fact]
    public void ValueToLiteral_With_Noncoordinate_Throws()
    {
        var type = new GeoJsonPositionType();
        const string item = "this is not a coordinate";

        Assert.Throws<LeafCoercionException>(() => type.ValueToLiteral(item));
    }
}
