using System.Collections;
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

    public override LineString CreateGeometry(
        IType type,
        object? coordinates,
        int? crs)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (coordinates is List<Coordinate> list)
        {
            coordinates = list.Count == 0 ? [] : list.ToArray();
        }

        if (coordinates is not IList coordsObject ||
            coordsObject.Count < 2 ||
            !coordsObject.TryConvertToCoordinates(out var coords))
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
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (fieldValues[0] is not GeoJsonGeometryType.LineString)
        {
            throw Geometry_Parse_InvalidType(type);
        }

        return CreateGeometry(type, fieldValues[1], (int?)fieldValues[2]);
    }

    public override void GetFieldData(IType type, object runtimeValue, object?[] fieldValues)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

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
