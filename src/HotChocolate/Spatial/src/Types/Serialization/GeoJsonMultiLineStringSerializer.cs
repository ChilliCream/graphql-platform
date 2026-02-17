using System.Collections;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types.Spatial.Serialization;

internal class GeoJsonMultiLineStringSerializer
    : GeoJsonInputObjectSerializer<MultiLineString>
{
    private GeoJsonMultiLineStringSerializer()
        : base(GeoJsonGeometryType.MultiLineString)
    {
    }

    public override void CoerceOutputCoordinates(
        IType type,
        object runtimeValue,
        ResultElement resultElement)
    {
        if (runtimeValue is MultiLineString multiLineString)
        {
            resultElement.SetArrayValue(multiLineString.NumGeometries);

            var lineIndex = 0;
            foreach (var lineElement in resultElement.EnumerateArray())
            {
                var lineString = (LineString)multiLineString.GetGeometryN(lineIndex++);
                GeoJsonLineStringSerializer.Default.CoerceOutputCoordinates(type, lineString, lineElement);
            }

            return;
        }

        throw Serializer_CouldNotParseValue(type);
    }

    public override IValueNode CoordinateToLiteral(IType type, object? runtimeValue)
    {
        if (runtimeValue is MultiLineString multiLineString)
        {
            var result = new IValueNode[multiLineString.NumGeometries];

            for (var i = 0; i < multiLineString.NumGeometries; i++)
            {
                var lineString = (LineString)multiLineString.GetGeometryN(i);
                result[i] = GeoJsonLineStringSerializer.Default.CoordinateToLiteral(type, lineString);
            }

            return new ListValueNode(result);
        }

        throw Serializer_CouldNotParseValue(type);
    }

    public override MultiLineString CreateGeometry(
        IType type,
        object? coordinates,
        int? crs)
    {
        ArgumentNullException.ThrowIfNull(type);

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
                    if (list[index] is IList nestedCoords
                        && nestedCoords.TryConvertToCoordinates(out var coordinate))
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

        var factory = crs is null
            ? NtsGeometryServices.Instance.CreateGeometryFactory()
            : NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

        return factory.CreateMultiLineString(geometries);
    }

    public override object CreateInstance(IType type, object?[] fieldValues)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (fieldValues[0] is not GeoJsonGeometryType.MultiLineString)
        {
            throw Geometry_Parse_InvalidType(type);
        }

        return CreateGeometry(type, fieldValues[1], (int?)fieldValues[2]);
    }

    public override void GetFieldData(IType type, object runtimeValue, object?[] fieldValues)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (runtimeValue is not MultiLineString geometry)
        {
            throw Geometry_Parse_InvalidGeometryType(type, runtimeValue.GetType());
        }

        fieldValues[0] = GeoJsonGeometryType.MultiLineString;
        fieldValues[1] = geometry.Geometries.Select(t => t.Coordinates);
        fieldValues[2] = geometry.SRID;
    }

    public static readonly GeoJsonMultiLineStringSerializer Default = new();
}
