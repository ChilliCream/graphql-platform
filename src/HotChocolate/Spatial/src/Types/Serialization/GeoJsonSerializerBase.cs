using System.Collections;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types.Spatial;
using HotChocolate.Utilities;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;
using static HotChocolate.Types.Spatial.WellKnownFields;

namespace HotChocolate.Types
{
    internal abstract class GeoJsonSerializerBase<T> : IGeoJsonSerializer
    {
        public abstract bool TrySerialize(object? runtimeValue, out object? resultValue);

        public abstract bool TryDeserialize(object? resultValue, out object? runtimeValue);

        public abstract bool IsInstanceOfType(IValueNode valueSyntax);

        public abstract object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true);

        public abstract IValueNode ParseResult(object? resultValue);

        public abstract IValueNode ParseValue(object? runtimeValue);

        public virtual object? Serialize(object? runtimeValue)
        {
            if (TrySerialize(runtimeValue, out object? serialized))
            {
                return serialized;
            }

            throw Serializer_CouldNotDeserialize();
        }

        public virtual object? Deserialize(object? resultValue)
        {
            if (TryDeserialize(resultValue, out object? deserialized))
            {
                return deserialized;
            }

            throw Serializer_CouldNotSerialize();
        }


        public virtual bool IsInstanceOfType(object? runtimeValue)
        {
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

        protected (GeoJsonGeometryType type, object coordinates, int? crs)
            ParseFields(IReadOnlyDictionary<string, object> obj)
        {
            GeoJsonGeometryType? type = null;
            object? coordinates = null;
            int? crs = null;

            if (obj.TryGetValue(TypeFieldName, out var typeObject))
            {
                type = GeoJsonTypeSerializer.Default.Deserialize(typeObject)
                    as GeoJsonGeometryType?;
            }

            if (obj.TryGetValue(CoordinatesFieldName, out var coordinateObject))
            {
                coordinates = ParseCoordinates(coordinateObject);
            }

            if (obj.TryGetValue(CrsFieldName, out var crsObject) &&
                crsObject is int crsInt)
            {
                crs = crsInt;
            }

            if (type is null)
            {
                throw Serializer_CoordinatesIsMissing();
            }

            if (coordinates is null)
            {
                throw Serializer_CoordinatesIsMissing();
            }

            return (type.Value, coordinates, crs);
        }

        protected (GeoJsonGeometryType type, object coordinates, int? crs)
            ParseFields(ObjectValueNode obj)
        {
            GeoJsonGeometryType? type = null;
            object? coordinates = null;
            int? crs = null;

            for (var i = 0; i < obj.Fields.Count; i++)
            {
                var fieldName = obj.Fields[i].Name.Value;
                IValueNode syntaxNode = obj.Fields[i].Value;
                if (TypeFieldName.EqualsInvariantIgnoreCase(fieldName))
                {
                    type = GeoJsonTypeSerializer.Default.ParseLiteral(syntaxNode)
                        as GeoJsonGeometryType?;
                }
                else if (CoordinatesFieldName.EqualsInvariantIgnoreCase(fieldName))
                {
                    coordinates = ParseCoordinates(syntaxNode);
                }
                else if (CrsFieldName.EqualsInvariantIgnoreCase(fieldName) &&
                    syntaxNode is IntValueNode node &&
                    !node.IsNull())
                {
                    crs = node.ToInt32();
                }
            }

            if (type is null)
            {
                throw Serializer_TypeIsMissing();
            }

            if (coordinates is null)
            {
                throw Serializer_CoordinatesIsMissing();
            }

            return (type.Value, coordinates, crs);
        }

        private object ParseCoordinates(object? runtimeValue)
        {
            if (runtimeValue is IList top &&
                top.Count > 0)
            {
                if (top[0] is IList second &&
                    second.Count > 0)
                {
                    if (second[0] is IList)
                    {
                        var result = new Coordinate[top.Count][];
                        for (var y = 0; y < result.Length; y++)
                        {
                            if (ParseCoordinates(top[y]) is Coordinate[] multi)
                            {
                                result[y] = multi;
                            }
                            else
                            {
                                throw Serializer_Parse_CoordinatesIsInvalid();
                            }
                        }

                        return result;
                    }
                    else
                    {
                        var result = new Coordinate[top.Count];
                        for (var y = 0; y < result.Length; y++)
                        {
                            if (ParseCoordinates(top[y]) is Coordinate coordinate)
                            {
                                result[y] = coordinate;
                            }
                            else
                            {
                                throw Serializer_Parse_CoordinatesIsInvalid();
                            }
                        }

                        return result;
                    }
                }
                else if (
                    GeoJsonPositionSerializer.Default.TryDeserialize(
                        runtimeValue,
                        out object? result) &&
                    result is { })
                {
                    return result;
                }
            }

            throw Serializer_Parse_CoordinatesIsInvalid();
        }

        private object ParseCoordinates(IValueNode syntaxNode)
        {
            if (syntaxNode is ListValueNode top &&
                top.Items.Count > 0)
            {
                if (top.Items[0] is ListValueNode second &&
                    second.Items.Count > 0)
                {
                    if (second.Items[0] is ListValueNode)
                    {
                        var result = new Coordinate[top.Items.Count][];
                        for (var y = 0; y < result.Length; y++)
                        {
                            if (ParseCoordinates(top.Items[y]) is Coordinate[] multi)
                            {
                                result[y] = multi;
                            }
                            else
                            {
                                throw Serializer_Parse_CoordinatesIsInvalid();
                            }
                        }

                        return result;
                    }
                    else
                    {
                        var result = new Coordinate[top.Items.Count];
                        for (var y = 0; y < result.Length; y++)
                        {
                            if (ParseCoordinates(top.Items[y]) is Coordinate coordinate)
                            {
                                result[y] = coordinate;
                            }
                            else
                            {
                                throw Serializer_Parse_CoordinatesIsInvalid();
                            }
                        }

                        return result;
                    }
                }

                if (GeoJsonPositionSerializer.Default.ParseLiteral(top) is Coordinate coord)
                {
                    return coord;
                }
            }

            throw Serializer_Parse_CoordinatesIsInvalid();
        }
    }
}
