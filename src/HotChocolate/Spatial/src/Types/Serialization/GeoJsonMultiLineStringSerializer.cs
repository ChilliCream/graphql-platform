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

internal class GeoJsonMultiLineStringSerializer
    : GeoJsonInputObjectSerializer<MultiLineString>
{
    private GeoJsonMultiLineStringSerializer()
        : base(GeoJsonGeometryType.MultiLineString)
    {
    }

    public override MultiLineString CreateGeometry(
        IType type,
        object? coordinates,
        int? crs)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (coordinates is IList { Count: > 0 } list)
        {
            if (list.Count == 0)
            {
                coordinates = Array.Empty<Coordinate[][]>();
            }
            else
            {
                var temp = new Coordinate[list.Count][];

                for (var index = 0; index < list.Count; index++)
                {
                    if (list[index] is IList nestedCoords &&
                        nestedCoords.TryConvertToCoordinates(out var coordinate))
                    {
                        temp[index] = coordinate;
                    }
                    else
                    {
                        throw Serializer_Parse_CoordinatesIsInvalid(type);
                    }
                }

                coordinates = temp;
            }
        }

        if (coordinates is not Coordinate[][] { Length: > 0 } parts)
        {
            throw Serializer_Parse_CoordinatesIsInvalid(type);
        }

        var lineCount = parts.Length;
        var geometries = new LineString[lineCount];

        for (var i = 0; i < lineCount; i++)
        {
            geometries[i] = GeoJsonLineStringSerializer.Default
                .CreateGeometry(type, parts[i], crs);
        }

        GeometryFactory factory = crs is null
            ? NtsGeometryServices.Instance.CreateGeometryFactory()
            : NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

        return factory.CreateMultiLineString(geometries);
    }

    public override object CreateInstance(IType type, object?[] fieldValues)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (fieldValues[0] is not GeoJsonGeometryType.MultiLineString)
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

        if (runtimeValue is not MultiLineString geometry)
        {
            throw Geometry_Parse_InvalidGeometryType(type, runtimeValue.GetType());
        }

        fieldValues[0] = GeoJsonGeometryType.MultiLineString;
        fieldValues[1] = geometry.Geometries.Select(t => t.Coordinates);
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

        if (runtimeValue is MultiLineString geometry)
        {
            var list = new List<ObjectFieldNode>
                {
                    new ObjectFieldNode(
                        TypeFieldName,
                        GeoJsonTypeSerializer.Default.ParseResult(
                            type,
                            GeoJsonGeometryType.MultiLineString)),
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

    public static readonly GeoJsonMultiLineStringSerializer Default = new();
}
