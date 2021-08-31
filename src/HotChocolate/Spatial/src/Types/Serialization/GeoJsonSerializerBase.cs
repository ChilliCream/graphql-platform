using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Utilities;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;
using static HotChocolate.Types.Spatial.WellKnownFields;

namespace HotChocolate.Types.Spatial.Serialization
{
    internal abstract class GeoJsonSerializerBase<T> : IGeoJsonSerializer
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

            if (runtimeValue is T)
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
                coordinates = ParseCoordinates(type, coordinateObject);
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
                IValueNode syntaxNode = field.Value;

                if (TypeFieldName.EqualsInvariantIgnoreCase(fieldName))
                {
                    geometryType =
                        GeoJsonTypeSerializer.Default.ParseLiteral(type, syntaxNode)
                            as GeoJsonGeometryType?;
                }
                else if (CoordinatesFieldName.EqualsInvariantIgnoreCase(fieldName))
                {
                    coordinates = ParseCoordinates(type, syntaxNode);
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

        private object ParseCoordinates(IType type, object? runtimeValue)
        {
            if (runtimeValue is IList { Count: > 0 } top)
            {
                if (top[0] is IList { Count: > 0 } second)
                {
                    if (second[0] is IList)
                    {
                        var result = new Coordinate[top.Count][];
                        for (var y = 0; y < result.Length; y++)
                        {
                            if (ParseCoordinates(type, top[y]) is Coordinate[] multi)
                            {
                                result[y] = multi;
                            }
                            else
                            {
                                throw Serializer_Parse_CoordinatesIsInvalid(type);
                            }
                        }

                        return result;
                    }
                    else
                    {
                        var result = new Coordinate[top.Count];
                        for (var y = 0; y < result.Length; y++)
                        {
                            if (ParseCoordinates(type, top[y]) is Coordinate coordinate)
                            {
                                result[y] = coordinate;
                            }
                            else
                            {
                                throw Serializer_Parse_CoordinatesIsInvalid(type);
                            }
                        }

                        return result;
                    }
                }
                else if (GeoJsonPositionSerializer.Default.TryDeserialize(
                    type, runtimeValue, out var result) &&
                    result is not null)
                {
                    return result;
                }
            }

            throw Serializer_Parse_CoordinatesIsInvalid(type);
        }

        private object ParseCoordinates(IType type, IValueNode syntaxNode)
        {
            if (syntaxNode is ListValueNode top && top.Items.Count > 0)
            {
                if (top.Items[0] is ListValueNode second && second.Items.Count > 0)
                {
                    if (second.Items[0] is ListValueNode)
                    {
                        var result = new Coordinate[top.Items.Count][];
                        for (var y = 0; y < result.Length; y++)
                        {
                            if (ParseCoordinates(type, top.Items[y]) is Coordinate[] multi)
                            {
                                result[y] = multi;
                            }
                            else
                            {
                                throw Serializer_Parse_CoordinatesIsInvalid(type);
                            }
                        }

                        return result;
                    }
                    else
                    {
                        var result = new Coordinate[top.Items.Count];
                        for (var y = 0; y < result.Length; y++)
                        {
                            if (ParseCoordinates(type, top.Items[y]) is Coordinate coordinate)
                            {
                                result[y] = coordinate;
                            }
                            else
                            {
                                throw Serializer_Parse_CoordinatesIsInvalid(type);
                            }
                        }

                        return result;
                    }
                }

                if (GeoJsonPositionSerializer.Default.ParseLiteral(type, top) is Coordinate coord)
                {
                    return coord;
                }
            }

            throw Serializer_Parse_CoordinatesIsInvalid(type);
        }
    }
}
