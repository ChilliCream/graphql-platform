using HotChocolate.Language;
using HotChocolate.Text.Json;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types.Spatial.Serialization;

internal class GeoJsonPointSerializer
    : GeoJsonInputObjectSerializer<Point>
{
    private GeoJsonPointSerializer()
        : base(GeoJsonGeometryType.Point)
    {
    }

    public override void CoerceOutputCoordinates(
        IType type,
        object runtimeValue,
        ResultElement resultElement)
    {
        if (runtimeValue is Point point)
        {
            GeoJsonPositionSerializer.Default.CoerceOutputCoordinates(type, point.Coordinate, resultElement);
            return;
        }

        throw Serializer_CouldNotParseValue(type);
    }

    public override IValueNode CoordinateToLiteral(IType type, object? runtimeValue)
    {
        if (runtimeValue is Point point)
        {
            return GeoJsonPositionSerializer.Default.ValueToLiteral(type, point.Coordinate);
        }

        throw Serializer_CouldNotParseValue(type);
    }

    public override Point CreateGeometry(
        IType type,
        object? coordinates,
        int? crs)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (coordinates is not Coordinate coordinate)
        {
            throw Serializer_Parse_CoordinatesIsInvalid(type);
        }

        var factory = crs is null
            ? NtsGeometryServices.Instance.CreateGeometryFactory()
            : NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

        return factory.CreatePoint(coordinate);
    }

    public override object CreateInstance(IType type, object?[] fieldValues)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (fieldValues[0] is not GeoJsonGeometryType.Point)
        {
            throw Geometry_Parse_InvalidType(type);
        }

        return CreateGeometry(type, fieldValues[1], (int?)fieldValues[2]);
    }

    public override void GetFieldData(IType type, object runtimeValue, object?[] fieldValues)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (runtimeValue is not Point geometry)
        {
            throw Geometry_Parse_InvalidGeometryType(type, runtimeValue.GetType());
        }

        fieldValues[0] = GeoJsonGeometryType.Point;
        fieldValues[1] = geometry.Coordinate;
        fieldValues[2] = geometry.SRID;
    }

    public static readonly GeoJsonPointSerializer Default = new();
}
