using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;
using static HotChocolate.Types.Spatial.WellKnownFields;

namespace HotChocolate.Types.Spatial.Serialization;

internal abstract class GeoJsonInputObjectSerializer<T>
    : GeoJsonSerializerBase<T>
    where T : Geometry
{
    private readonly GeoJsonGeometryType _geometryType;

    protected GeoJsonInputObjectSerializer(GeoJsonGeometryType geometryType)
    {
        _geometryType = geometryType;
    }

    public abstract T CreateGeometry(IType type, object? coordinates, int? crs);

    public override bool IsValueCompatible(IType type, IValueNode valueLiteral)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(valueLiteral);

        return valueLiteral is ObjectValueNode or NullValueNode;
    }

    public override bool IsValueCompatible(IType type, JsonElement inputValue)
    {
        ArgumentNullException.ThrowIfNull(type);

        return inputValue.ValueKind is JsonValueKind.Object or JsonValueKind.Null;
    }

    public override object? CoerceInputLiteral(IType type, IValueNode valueLiteral)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (valueLiteral is NullValueNode)
        {
            return null;
        }

        if (valueLiteral is ObjectValueNode obj)
        {
            var (geometryType, coordinates, crs) = ParseFields(type, obj);

            if (geometryType != _geometryType)
            {
                throw Serializer_Parse_TypeIsInvalid(type);
            }

            return CreateGeometry(type, coordinates, crs);
        }

        throw Serializer_Parse_ValueKindInvalid(type, valueLiteral.Kind);
    }

    public override object? CoerceInputValue(IType type, JsonElement inputValue, IFeatureProvider context)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (inputValue.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (inputValue.ValueKind == JsonValueKind.Object)
        {
            var (geometryType, coordinates, crs) = ParseFieldsFromJson(type, inputValue);

            if (geometryType != _geometryType)
            {
                throw Serializer_Parse_TypeIsInvalid(type);
            }

            return CreateGeometry(type, coordinates, crs);
        }

        throw Serializer_Parse_ValueKindInvalid(type, SyntaxKind.ObjectValue);
    }

    public override void CoerceOutputValue(IType type, object runtimeValue, ResultElement resultValue)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (runtimeValue is not T geometry)
        {
            throw Serializer_CouldNotParseValue(type);
        }

        resultValue.SetObjectValue(3);

        var propertyIndex = 0;
        foreach (var property in resultValue.EnumerateObject())
        {
            switch (propertyIndex++)
            {
                case 0:
                    property.Value.SetPropertyName(TypeFieldNameBytes);
                    GeoJsonTypeSerializer.Default.CoerceOutputValue(type, _geometryType, property.Value);
                    break;
                case 1:
                    property.Value.SetPropertyName(CoordinatesFieldNameBytes);
                    CoerceOutputCoordinates(type, geometry, property.Value);
                    break;
                case 2:
                    property.Value.SetPropertyName(CrsFieldNameBytes);
                    property.Value.SetNumberValue(geometry.SRID);
                    break;
            }
        }
    }

    public override IValueNode ValueToLiteral(IType type, object? runtimeValue)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (runtimeValue is null)
        {
            return NullValueNode.Default;
        }

        if (runtimeValue is T geometry)
        {
            var list = new List<ObjectFieldNode>
            {
                new ObjectFieldNode(
                    TypeFieldName,
                    GeoJsonTypeSerializer.Default.ValueToLiteral(type, _geometryType)),
                new ObjectFieldNode(
                    CoordinatesFieldName,
                    CoordinateToLiteral(type, geometry)),
                new ObjectFieldNode(
                    CrsFieldName,
                    new IntValueNode(geometry.SRID))
            };

            return new ObjectValueNode(list);
        }

        throw Serializer_CouldNotParseValue(type);
    }

    public override object CreateInstance(IType type, object?[] fieldValues)
    {
        throw Serializer_OperationIsNotSupported(type, this, nameof(CreateInstance));
    }

    public override void GetFieldData(IType type, object runtimeValue, object?[] fieldValues)
    {
        throw Serializer_OperationIsNotSupported(type, this, nameof(GetFieldData));
    }
}
