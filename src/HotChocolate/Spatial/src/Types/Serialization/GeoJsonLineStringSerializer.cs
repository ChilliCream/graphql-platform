using System.Collections;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types.Spatial.Serialization;

internal class GeoJsonLineStringSerializer : GeoJsonInputObjectSerializer<LineString>
{
    private GeoJsonLineStringSerializer()
        : base(GeoJsonGeometryType.LineString)
    {
    }

    public override void CoerceOutputCoordinates(
        IType type,
        object runtimeValue,
        ResultElement resultElement)
    {
        if (runtimeValue is LineString lineString)
        {
            var coords = lineString.Coordinates;
            resultElement.SetArrayValue(coords.Length);

            var index = 0;
            foreach (var element in resultElement.EnumerateArray())
            {
                GeoJsonPositionSerializer.Default.CoerceOutputCoordinates(type, coords[index++], element);
            }

            return;
        }

        throw Serializer_CouldNotParseValue(type);
    }

    public override IValueNode CoordinateToLiteral(IType type, object? runtimeValue)
    {
        if (runtimeValue is LineString lineString)
        {
            var coords = lineString.Coordinates;
            var result = new IValueNode[coords.Length];

            for (var i = 0; i < coords.Length; i++)
            {
                result[i] = GeoJsonPositionSerializer.Default.ValueToLiteral(type, coords[i]);
            }

            return new ListValueNode(result);
        }

        throw Serializer_CouldNotParseValue(type);
    }

    public override LineString CreateGeometry(
        IType type,
        object? coordinates,
        int? crs)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (coordinates is List<Coordinate> list)
        {
            coordinates = list.Count == 0 ? [] : list.ToArray();
        }

        if (coordinates is not IList coordsObject
            || coordsObject.Count < 2
            || !coordsObject.TryConvertToCoordinates(out var coords))
        {
            throw Serializer_Parse_CoordinatesIsInvalid(type);
        }

        var factory = crs is null
            ? NtsGeometryServices.Instance.CreateGeometryFactory()
            : NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

        return factory.CreateLineString(coords);
    }

    public override object CreateInstance(IType type, object?[] fieldValues)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (fieldValues[0] is not GeoJsonGeometryType.LineString)
        {
            throw Geometry_Parse_InvalidType(type);
        }

        return CreateGeometry(type, fieldValues[1], (int?)fieldValues[2]);
    }

    public override void GetFieldData(IType type, object runtimeValue, object?[] fieldValues)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (runtimeValue is not Geometry geometry)
        {
            throw Geometry_Parse_InvalidGeometryType(type, runtimeValue.GetType());
        }

        fieldValues[0] = GeoJsonGeometryType.LineString;
        fieldValues[1] = geometry.Coordinates;
        fieldValues[2] = geometry.SRID;
    }

    public static readonly GeoJsonLineStringSerializer Default = new();
}
