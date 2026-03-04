using System.Collections;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types.Spatial.Serialization;

internal class GeoJsonPolygonSerializer
    : GeoJsonInputObjectSerializer<Polygon>
{
    private GeoJsonPolygonSerializer()
        : base(GeoJsonGeometryType.Polygon)
    {
    }

    public override void CoerceOutputCoordinates(
        IType type,
        object runtimeValue,
        ResultElement resultElement)
    {
        if (runtimeValue is Polygon polygon)
        {
            var ringCount = polygon.NumInteriorRings + 1;
            resultElement.SetArrayValue(ringCount);

            var ringIndex = 0;
            foreach (var ringElement in resultElement.EnumerateArray())
            {
                var ring = ringIndex == 0 ? polygon.ExteriorRing : polygon.InteriorRings[ringIndex - 1];
                var coords = ring.Coordinates;
                ringElement.SetArrayValue(coords.Length);

                var coordIndex = 0;
                foreach (var coordElement in ringElement.EnumerateArray())
                {
                    GeoJsonPositionSerializer.Default.CoerceOutputCoordinates(type, coords[coordIndex++], coordElement);
                }

                ringIndex++;
            }

            return;
        }

        throw Serializer_CouldNotParseValue(type);
    }

    public override IValueNode CoordinateToLiteral(IType type, object? runtimeValue)
    {
        if (runtimeValue is Polygon p)
        {
            var geometryCoords = new IValueNode[p.NumInteriorRings + 1];
            geometryCoords[0] = RingToLiteral(type, p.ExteriorRing);

            for (var i = 0; i < p.InteriorRings.Length; i++)
            {
                geometryCoords[i + 1] = RingToLiteral(type, p.InteriorRings[i]);
            }

            return new ListValueNode(geometryCoords);
        }

        throw Serializer_CouldNotParseValue(type);
    }

    private static IValueNode RingToLiteral(IType type, LineString ring)
    {
        var coords = ring.Coordinates;
        var result = new IValueNode[coords.Length];

        for (var i = 0; i < coords.Length; i++)
        {
            result[i] = GeoJsonPositionSerializer.Default.ValueToLiteral(type, coords[i]);
        }

        return new ListValueNode(result);
    }

    public override Polygon CreateGeometry(
        IType type,
        object? coordinates,
        int? crs)
    {
        ArgumentNullException.ThrowIfNull(type);

        object[]? ringsCoordinates = null;
        if (coordinates is IList listObj)
        {
            ringsCoordinates = new object[listObj.Count];
            for (var i = 0; i < listObj.Count; i++)
            {
                if (listObj[i] is IList ringCoordinateObject
                    && ringCoordinateObject.TryConvertToCoordinates(out var ringCoordinate))
                {
                    ringsCoordinates[i] = ringCoordinate;
                }
                else
                {
                    throw Serializer_Parse_CoordinatesIsInvalid(type);
                }
            }
        }

        if (ringsCoordinates is not { })
        {
            throw Serializer_Parse_CoordinatesIsInvalid(type);
        }

        var factory = crs is null
            ? NtsGeometryServices.Instance.CreateGeometryFactory()
            : NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

        var ringSrid = factory.CreateLinearRing((Coordinate[])ringsCoordinates[0]);
        var holes = Array.Empty<LinearRing>();
        if (ringsCoordinates.Length > 1)
        {
            holes = new LinearRing[ringsCoordinates.Length - 1];
            for (var i = 0; i < ringsCoordinates.Length - 1; i++)
            {
                holes[i] = factory.CreateLinearRing((Coordinate[])ringsCoordinates[i + 1]);
            }
        }

        return factory.CreatePolygon(ringSrid, holes);
    }

    public override object CreateInstance(IType type, object?[] fieldValues)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (fieldValues[0] is not GeoJsonGeometryType.Polygon)
        {
            throw Geometry_Parse_InvalidType(type);
        }

        return CreateGeometry(type, fieldValues[1], (int?)fieldValues[2]);
    }

    public override void GetFieldData(IType type, object runtimeValue, object?[] fieldValues)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (runtimeValue is not Polygon geometry)
        {
            throw Geometry_Parse_InvalidGeometryType(type, runtimeValue.GetType());
        }

        // Build the coordinate arrays for polygon (rings)
        var rings = new Coordinate[geometry.NumInteriorRings + 1][];
        rings[0] = geometry.ExteriorRing.Coordinates;
        for (var i = 0; i < geometry.InteriorRings.Length; i++)
        {
            rings[i + 1] = geometry.InteriorRings[i].Coordinates;
        }

        fieldValues[0] = GeoJsonGeometryType.Polygon;
        fieldValues[1] = rings;
        fieldValues[2] = geometry.SRID;
    }

    public static readonly GeoJsonPolygonSerializer Default = new();
}
