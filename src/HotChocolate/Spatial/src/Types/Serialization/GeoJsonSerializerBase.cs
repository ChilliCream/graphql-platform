using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Utilities;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;
using static HotChocolate.Types.Spatial.WellKnownFields;

namespace HotChocolate.Types.Spatial.Serialization;

internal abstract class GeoJsonSerializerBase : IGeoJsonSerializer
{
    public abstract bool IsValueCompatible(IType type, IValueNode valueLiteral);

    public abstract bool IsValueCompatible(IType type, JsonElement inputValue);

    public abstract object? CoerceInputLiteral(IType type, IValueNode valueLiteral);

    public abstract object? CoerceInputValue(IType type, JsonElement inputValue, IFeatureProvider context);

    public abstract void CoerceOutputValue(IType type, object runtimeValue, ResultElement resultValue);

    public abstract IValueNode ValueToLiteral(IType type, object? runtimeValue);

    public abstract object CreateInstance(IType type, object?[] fieldValues);

    public abstract void GetFieldData(IType type, object runtimeValue, object?[] fieldValues);

    public virtual IValueNode CoordinateToLiteral(IType type, object? runtimeValue)
    {
        if (runtimeValue is GeometryCollection collection)
        {
            var geometryCoords = new IValueNode[collection.Geometries.Length];
            for (var i = 0; i < collection.Geometries.Length; i++)
            {
                if (GeoJsonSerializers.SerializersByTypeName.TryGetValue(
                    collection.Geometries[i].GeometryType,
                    out var serializer))
                {
                    geometryCoords[i] =
                        serializer.CoordinateToLiteral(type, collection.Geometries[i]);
                }
                else
                {
                    geometryCoords[i] = CoordinateToLiteral(type, collection.Geometries[i]);
                }
            }

            return new ListValueNode(geometryCoords);
        }

        if (runtimeValue is Geometry g)
        {
            var result = new IValueNode[g.Coordinates.Length];

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = GeoJsonPositionSerializer.Default.ValueToLiteral(type, g.Coordinates[i]);
            }

            return new ListValueNode(result);
        }

        throw Serializer_Parse_CoordinatesIsInvalid(type);
    }

    public virtual void CoerceOutputCoordinates(IType type, object runtimeValue, ResultElement resultElement)
    {
        if (runtimeValue is GeometryCollection collection)
        {
            resultElement.SetArrayValue(collection.Geometries.Length);

            var geomIndex = 0;
            foreach (var element in resultElement.EnumerateArray())
            {
                var geom = collection.Geometries[geomIndex++];
                if (GeoJsonSerializers.SerializersByTypeName.TryGetValue(
                    geom.GeometryType,
                    out var serializer))
                {
                    serializer.CoerceOutputCoordinates(type, geom, element);
                }
                else
                {
                    CoerceOutputCoordinates(type, geom, element);
                }
            }

            return;
        }

        if (runtimeValue is Geometry g)
        {
            resultElement.SetArrayValue(g.Coordinates.Length);

            var coordIndex = 0;
            foreach (var element in resultElement.EnumerateArray())
            {
                GeoJsonPositionSerializer.Default.CoerceOutputCoordinates(type, g.Coordinates[coordIndex++], element);
            }

            return;
        }

        throw Serializer_CouldNotSerialize(type);
    }

    protected static (GeoJsonGeometryType type, object coordinates, int? crs) ParseFields(
        IType type,
        ObjectValueNode obj)
    {
        GeoJsonGeometryType? geometryType = null;
        object? coordinates = null;
        int? crs = null;

        foreach (var field in obj.Fields)
        {
            var fieldName = field.Name.Value;
            var syntaxNode = field.Value;

            if (TypeFieldName.EqualsInvariantIgnoreCase(fieldName))
            {
                geometryType =
                    GeoJsonTypeSerializer.Default.CoerceInputLiteral(type, syntaxNode)
                        as GeoJsonGeometryType?;
            }
            else if (CoordinatesFieldName.EqualsInvariantIgnoreCase(fieldName))
            {
                coordinates = ParseCoordinateLiteral(type, syntaxNode);
            }
            else if (CrsFieldName.EqualsInvariantIgnoreCase(fieldName)
                && syntaxNode is IntValueNode node
                && !node.IsNull())
            {
                crs = node.ToInt32();
            }
        }

        if (geometryType is null)
        {
            throw Serializer_TypeIsMissing(type);
        }

        if (coordinates is null)
        {
            throw Serializer_CoordinatesIsMissing(type);
        }

        return (geometryType.Value, coordinates, crs);
    }

    protected static (GeoJsonGeometryType type, object coordinates, int? crs) ParseFieldsFromJson(
        IType type,
        JsonElement obj)
    {
        GeoJsonGeometryType? geometryType = null;
        object? coordinates = null;
        int? crs = null;

        if (obj.TryGetProperty(TypeFieldName, out var typeElement))
        {
            geometryType = GeoJsonTypeSerializer.Default.CoerceInputValueFromJson(type, typeElement)
                as GeoJsonGeometryType?;
        }

        if (obj.TryGetProperty(CoordinatesFieldName, out var coordsElement))
        {
            coordinates = GeoJsonCoordinatesSerializer.Default.ParseCoordinateFromJson(type, coordsElement);
        }

        if (obj.TryGetProperty(CrsFieldName, out var crsElement)
            && crsElement.ValueKind == JsonValueKind.Number)
        {
            crs = crsElement.GetInt32();
        }

        if (geometryType is null)
        {
            throw Serializer_TypeIsMissing(type);
        }

        if (coordinates is null)
        {
            throw Serializer_CoordinatesIsMissing(type);
        }

        return (geometryType.Value, coordinates, crs);
    }

    protected static object ParseCoordinateLiteral(IType type, IValueNode syntaxNode)
    {
        return GeoJsonCoordinatesSerializer.Default.ParseCoordinateLiteral(type, syntaxNode);
    }
}

internal abstract class GeoJsonSerializerBase<T> : GeoJsonSerializerBase
{
    // No runtime type checking - the new scalar API handles this at the type level
}
