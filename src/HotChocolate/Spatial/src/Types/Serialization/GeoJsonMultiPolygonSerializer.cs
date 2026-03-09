using System.Collections;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types.Spatial.Serialization;

internal class GeoJsonMultiPolygonSerializer
    : GeoJsonInputObjectSerializer<MultiPolygon>
{
    private GeoJsonMultiPolygonSerializer()
        : base(GeoJsonGeometryType.MultiPolygon)
    {
    }

    public override void CoerceOutputCoordinates(
        IType type,
        object runtimeValue,
        ResultElement resultElement)
    {
        if (runtimeValue is MultiPolygon multiPolygon)
        {
            resultElement.SetArrayValue(multiPolygon.NumGeometries);

            var polygonIndex = 0;
            foreach (var polygonElement in resultElement.EnumerateArray())
            {
                var polygon = (Polygon)multiPolygon.GetGeometryN(polygonIndex++);
                GeoJsonPolygonSerializer.Default.CoerceOutputCoordinates(type, polygon, polygonElement);
            }

            return;
        }

        throw Serializer_CouldNotParseValue(type);
    }

    public override IValueNode CoordinateToLiteral(IType type, object? runtimeValue)
    {
        if (runtimeValue is MultiPolygon multiPolygon)
        {
            var result = new IValueNode[multiPolygon.NumGeometries];

            for (var i = 0; i < multiPolygon.NumGeometries; i++)
            {
                var polygon = (Polygon)multiPolygon.GetGeometryN(i);
                result[i] = GeoJsonPolygonSerializer.Default.CoordinateToLiteral(type, polygon);
            }

            return new ListValueNode(result);
        }

        throw Serializer_CouldNotParseValue(type);
    }

    public override MultiPolygon CreateGeometry(
        IType type,
        object? coordinates,
        int? crs)
    {
        ArgumentNullException.ThrowIfNull(type);

        Polygon[]? geometries;

        if (coordinates is IList { Count: > 0 } list)
        {
            if (list.Count != 0)
            {
                geometries = new Polygon[list.Count];

                for (var index = 0; index < list.Count; index++)
                {
                    if (list[index] is IList nestedCoords)
                    {
                        geometries[index] =
                            GeoJsonPolygonSerializer.Default
                                .CreateGeometry(type, nestedCoords, crs);
                    }
                    else
                    {
                        throw Serializer_Parse_CoordinatesIsInvalid(type);
                    }
                }

                goto Success;
            }
        }

        goto Error;

Success:
        var factory = crs is null
            ? NtsGeometryServices.Instance.CreateGeometryFactory()
            : NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

        return factory.CreateMultiPolygon(geometries);

Error:
        throw Serializer_Parse_CoordinatesIsInvalid(type);
    }

    public override object CreateInstance(IType type, object?[] fieldValues)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (fieldValues[0] is not GeoJsonGeometryType.MultiPolygon)
        {
            throw Geometry_Parse_InvalidType(type);
        }

        return CreateGeometry(type, fieldValues[1], (int?)fieldValues[2]);
    }

    public override void GetFieldData(IType type, object runtimeValue, object?[] fieldValues)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (runtimeValue is not MultiPolygon geometry)
        {
            throw Geometry_Parse_InvalidGeometryType(type, runtimeValue.GetType());
        }

        // Build coordinate arrays for each polygon
        var polygonCoords = new object[geometry.NumGeometries];
        for (var i = 0; i < geometry.NumGeometries; i++)
        {
            var polygon = (Polygon)geometry.GetGeometryN(i);
            var rings = new Coordinate[polygon.NumInteriorRings + 1][];
            rings[0] = polygon.ExteriorRing.Coordinates;
            for (var j = 0; j < polygon.InteriorRings.Length; j++)
            {
                rings[j + 1] = polygon.InteriorRings[j].Coordinates;
            }
            polygonCoords[i] = rings;
        }

        fieldValues[0] = GeoJsonGeometryType.MultiPolygon;
        fieldValues[1] = polygonCoords;
        fieldValues[2] = geometry.SRID;
    }

    public static readonly GeoJsonMultiPolygonSerializer Default = new();
}
