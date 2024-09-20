using System.Collections;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types.Spatial.Serialization;

internal class GeoJsonMultiPointSerializer
    : GeoJsonInputObjectSerializer<MultiPoint>
{
    private GeoJsonMultiPointSerializer()
        : base(GeoJsonGeometryType.MultiPoint)
    {
    }

    public override MultiPoint CreateGeometry(
        IType type,
        object? coordinates,
        int? crs)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        Point[]? geometries;

        if (coordinates is IList { Count: > 0, } listObjects &&
            listObjects.TryConvertToCoordinates(out var list))
        {
            geometries = new Point[list.Length];

            for (var i = 0; i < list.Length; i++)
            {
                geometries[i] =
                    GeoJsonPointSerializer.Default.CreateGeometry(type, list[i], crs);
            }

            goto Success;
        }

        goto Error;

Success:
        var factory = crs is null
            ? NtsGeometryServices.Instance.CreateGeometryFactory()
            : NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

        return factory.CreateMultiPoint(geometries);

Error:
        throw Serializer_Parse_CoordinatesIsInvalid(type);
    }

    public override object CreateInstance(IType type, object?[] fieldValues)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (fieldValues[0] is not GeoJsonGeometryType.MultiPoint)
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

        fieldValues[0] = GeoJsonGeometryType.MultiPoint;
        fieldValues[1] = geometry.Coordinates;
        fieldValues[2] = geometry.SRID;
    }

    public static readonly GeoJsonMultiPointSerializer Default = new();
}
