using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Utilities;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;
using static HotChocolate.Types.Spatial.WellKnownFields;

namespace HotChocolate.Types.Spatial.Serialization;

internal abstract class GeoJsonSerializerBase : IGeoJsonSerializer
{
    public abstract bool IsInstanceOfType(IType type, IValueNode valueSyntax);

    public virtual bool IsInstanceOfType(IType type, object? runtimeValue)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (runtimeValue is null)
        {
            return true;
        }

        return false;
    }

    public virtual object? Deserialize(IType type, object? resultValue)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (TryDeserialize(type, resultValue, out var deserialized))
        {
            return deserialized;
        }

        throw Serializer_CouldNotSerialize(type);
    }

    public virtual object? Serialize(IType type, object? runtimeValue)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (TrySerialize(type, runtimeValue, out var serialized))
        {
            return serialized;
        }

        throw Serializer_CouldNotDeserialize(type);
    }

    public abstract bool TryDeserialize(
        IType type,
        object? resultValue,
        out object? runtimeValue);

    public abstract bool TrySerialize(
        IType type,
        object? runtimeValue,
        out object? resultValue);

    public abstract object? ParseLiteral(IType type, IValueNode valueSyntax);

    public abstract IValueNode ParseValue(IType type, object? runtimeValue);

    public abstract IValueNode ParseResult(IType type, object? resultValue);

    public abstract object CreateInstance(IType type, object?[] fieldValues);

    public abstract void GetFieldData(IType type, object runtimeValue, object?[] fieldValues);

    public virtual IValueNode ParseCoordinateResult(IType type, object? runtimeValue)
    {
        if (runtimeValue is IList { Count: > 0, } list && list[0] is IList)
        {
            var results = new IValueNode[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                results[i] = ParseCoordinateResult(type, list[i]);
            }

            return new ListValueNode(results);
        }

        if (runtimeValue is Coordinate)
        {
            return GeoJsonPositionSerializer.Default.ParseResult(type, runtimeValue);
        }

        if (runtimeValue is double[])
        {
            return GeoJsonPositionSerializer.Default.ParseResult(type, runtimeValue);
        }

        throw Serializer_CouldNotParseValue(type);
    }

    protected (GeoJsonGeometryType type, object coordinates, int? crs) ParseFields(
        IType type,
        IReadOnlyDictionary<string, object> obj)
    {
        GeoJsonGeometryType? geometryType = null;
        object? coordinates = null;
        int? crs = null;

        if (obj.TryGetValue(TypeFieldName, out var typeObject))
        {
            geometryType = GeoJsonTypeSerializer.Default.Deserialize(type, typeObject)
                as GeoJsonGeometryType?;
        }

        if (obj.TryGetValue(CoordinatesFieldName, out var coordinateObject))
        {
            coordinates = DeserializeCoordinate(type, coordinateObject);
        }

        if (obj.TryGetValue(CrsFieldName, out var crsObject) &&
            crsObject is int crsInt)
        {
            crs = crsInt;
        }

        if (geometryType is null)
        {
            throw Serializer_CoordinatesIsMissing(type);
        }

        if (coordinates is null)
        {
            throw Serializer_CoordinatesIsMissing(type);
        }

        return (geometryType.Value, coordinates, crs);
    }

    protected (GeoJsonGeometryType type, object coordinates, int? crs) ParseFields(
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
                    GeoJsonTypeSerializer.Default.ParseLiteral(type, syntaxNode)
                        as GeoJsonGeometryType?;
            }
            else if (CoordinatesFieldName.EqualsInvariantIgnoreCase(fieldName))
            {
                coordinates = ParseCoordinateLiteral(type, syntaxNode);
            }
            else if (CrsFieldName.EqualsInvariantIgnoreCase(fieldName) &&
                syntaxNode is IntValueNode node &&
                !node.IsNull())
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

    private object DeserializeCoordinate(IType type, object? runtimeValue)
    {
        return GeoJsonCoordinatesSerializer.Default.DeserializeCoordinate(type, runtimeValue);
    }

    private object ParseCoordinateLiteral(IType type, IValueNode syntaxNode)
    {
        return GeoJsonCoordinatesSerializer.Default.ParseCoordinateLiteral(type, syntaxNode);
    }

    public virtual bool TrySerializeCoordinates(
        IType type,
        object runtimeValue,
        out object? serialized)
    {
        serialized = null;
        if (runtimeValue is GeometryCollection collection)
        {
            var geometryCoords = new object?[collection.Geometries.Length];
            for (var i = 0; i < collection.Geometries.Length; i++)
            {
                if (GeoJsonSerializers.SerializersByTypeName.TryGetValue(
                    collection.Geometries[i].GeometryType,
                    out var serializer))
                {
                    if (serializer.TrySerializeCoordinates(type,
                        collection.Geometries[i],
                        out var elementCoords))
                    {
                        geometryCoords[i] = elementCoords;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (TrySerializeCoordinates(type,
                        collection.Geometries[i],
                        out var elementCoords))
                    {
                        geometryCoords[i] = elementCoords;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            serialized = geometryCoords;
            return true;
        }

        if (runtimeValue is Geometry g)
        {
            var result = new double[g.Coordinates.Length][];
            for (var i = 0; i < result.Length; i++)
            {
                if (GeoJsonPositionSerializer.Default.TrySerialize(type,
                        g.Coordinates[i],
                        out var serializedPoints) &&
                    serializedPoints is double[] points)
                {
                    result[i] = points;
                }
                else
                {
                    serialized = null;
                    return false;
                }
            }

            serialized = result;
            return true;
        }

        serialized = null;
        return false;
    }

    public virtual IValueNode ParseCoordinateValue(IType type, object? runtimeValue)
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
                        serializer.ParseCoordinateValue(type, collection.Geometries[i]);
                }
                else
                {
                    geometryCoords[i] = ParseCoordinateValue(type, collection.Geometries[i]);
                }
            }

            return new ListValueNode(geometryCoords);
        }

        if (runtimeValue is Geometry g)
        {
            var result = new IValueNode[g.Coordinates.Length];

            for (var i = 0; i < result.Length; i++)
            {
                if (GeoJsonPositionSerializer.Default
                    .ParseResult(type, g.Coordinates[i]) is { } parsed)
                {
                    result[i] = parsed;
                }
                else
                {
                    throw Serializer_Parse_CoordinatesIsInvalid(type);
                }
            }

            return new ListValueNode(result);
        }

        throw Serializer_Parse_CoordinatesIsInvalid(type);
    }
}

internal abstract class GeoJsonSerializerBase<T> : GeoJsonSerializerBase
{
    public override bool IsInstanceOfType(IType type, object? runtimeValue)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (runtimeValue is null)
        {
            return true;
        }

        if (runtimeValue is T)
        {
            return true;
        }

        return false;
    }
}
