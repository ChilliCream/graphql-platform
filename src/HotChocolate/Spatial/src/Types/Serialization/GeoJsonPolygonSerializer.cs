using System.Collections;
using HotChocolate.Language;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;
using static HotChocolate.Types.Spatial.WellKnownFields;

namespace HotChocolate.Types.Spatial.Serialization;

internal class GeoJsonPolygonSerializer
    : GeoJsonInputObjectSerializer<Polygon>
{
    private GeoJsonPolygonSerializer()
        : base(GeoJsonGeometryType.Polygon)
    {
    }

    public override Polygon CreateGeometry(
        IType type,
        object? coordinates,
        int? crs)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        object[]? ringsCoordinates = null;
        if (coordinates is IList listObj)
        {
            ringsCoordinates = new object[listObj.Count];
            for (var i = 0; i < listObj.Count; i++)
            {
                if (listObj[i] is IList ringCoordinateObject &&
                    ringCoordinateObject.TryConvertToCoordinates(out var ringCoordinate))
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
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (fieldValues[0] is not GeoJsonGeometryType.Polygon)
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

        if (runtimeValue is not Polygon geometry ||
            !TrySerializeCoordinates(type, geometry, out var serialized))
        {
            throw Geometry_Parse_InvalidGeometryType(type, runtimeValue.GetType());
        }

        fieldValues[0] = GeoJsonGeometryType.Polygon;
        fieldValues[1] = serialized;
        fieldValues[2] = geometry.SRID;
    }

    public override IValueNode ParseValue(IType type, object? runtimeValue)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (runtimeValue is null)
        {
            return NullValueNode.Default;
        }

        if (runtimeValue is IReadOnlyDictionary<string, object> dict)
        {
            return ParseResult(type, dict);
        }

        if (runtimeValue is Polygon geometry)
        {
            var list = new List<ObjectFieldNode>
                {
                    new(TypeFieldName,
                        GeoJsonTypeSerializer.Default.ParseResult(
                            type,
                            GeoJsonGeometryType.Polygon)),
                    new(CoordinatesFieldName, ParseCoordinateValue(type, geometry)),
                    new(CrsFieldName, new IntValueNode(geometry.SRID)),
                };

            return new ObjectValueNode(list);
        }

        throw Serializer_CouldNotParseValue(type);
    }

    public override IValueNode ParseCoordinateValue(IType type, object? runtimeValue)
    {
        if (runtimeValue is Polygon p)
        {
            var geometryCoords = new IValueNode[p!.NumInteriorRings + 1];
            geometryCoords[0] = base.ParseCoordinateValue(type, p.ExteriorRing);
            for (var i = 0; i < p.InteriorRings.Length; i++)
            {
                geometryCoords[i + 1] =
                    base.ParseCoordinateValue(type, p.InteriorRings[i]);
            }

            return new ListValueNode(geometryCoords);
        }

        throw Serializer_CouldNotParseValue(type);
    }

    public override bool TrySerializeCoordinates(
        IType type,
        object runtimeValue,
        out object? serialized)
    {
        if (runtimeValue is Polygon polygon)
        {
            var geometryCoords = new object?[polygon.NumInteriorRings + 1];

            if (base.TrySerializeCoordinates(type,
                polygon.ExteriorRing,
                out var serializedPolygonCoords))
            {
                geometryCoords[0] = serializedPolygonCoords;
            }
            else
            {
                throw Serializer_CouldNotSerialize(type);
            }

            for (var i = 0; i < polygon.InteriorRings.Length; i++)
            {
                if (base.TrySerializeCoordinates(
                    type,
                    polygon.InteriorRings[i],
                    out var coords))
                {
                    geometryCoords[i + 1] = coords;
                }
                else
                {
                    throw Serializer_CouldNotSerialize(type);
                }
            }

            serialized = geometryCoords;
            return true;
        }

        serialized = null;
        return false;
    }

    public static readonly GeoJsonPolygonSerializer Default = new();
}
