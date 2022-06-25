using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;
using static HotChocolate.Types.Spatial.WellKnownFields;

namespace HotChocolate.Types.Spatial.Serialization;

internal class GeoJsonMultiPolygonSerializer
    : GeoJsonInputObjectSerializer<MultiPolygon>
{
    private GeoJsonMultiPolygonSerializer()
        : base(GeoJsonGeometryType.MultiPolygon)
    {
    }

    public override MultiPolygon CreateGeometry(
        IType type,
        object? coordinates,
        int? crs)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

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
        GeometryFactory factory = crs is null
            ? NtsGeometryServices.Instance.CreateGeometryFactory()
            : NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

        return factory.CreateMultiPolygon(geometries);

Error:
        throw Serializer_Parse_CoordinatesIsInvalid(type);
    }

    public override object CreateInstance(IType type, object?[] fieldValues)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (fieldValues[0] is not GeoJsonGeometryType.MultiPolygon)
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

        if (runtimeValue is not MultiPolygon geometry ||
            !TrySerializeCoordinates(type, runtimeValue, out var serialized))
        {
            throw Geometry_Parse_InvalidGeometryType(type, runtimeValue.GetType());
        }

        fieldValues[0] = GeoJsonGeometryType.MultiPolygon;
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

        if (runtimeValue is MultiPolygon geometry)
        {
            var list = new List<ObjectFieldNode>
                {
                    new ObjectFieldNode(
                        TypeFieldName,
                        GeoJsonTypeSerializer.Default.ParseResult(
                            type,
                            GeoJsonGeometryType.MultiPolygon)),
                    new ObjectFieldNode(
                        CoordinatesFieldName,
                        ParseCoordinateValue(type, geometry)),
                    new ObjectFieldNode(
                        CrsFieldName,
                        new IntValueNode(geometry.SRID))
                };

            return new ObjectValueNode(list);
        }

        throw Serializer_CouldNotParseValue(type);
    }

    public static readonly GeoJsonMultiPolygonSerializer Default = new();
}
