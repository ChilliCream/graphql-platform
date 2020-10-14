using System;
using System.Collections.Generic;
using HotChocolate.Language;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;
using static HotChocolate.Types.Spatial.WellKnownFields;
using static HotChocolate.Types.Spatial.Serialization.GeoJsonSerializers;

namespace HotChocolate.Types.Spatial.Serialization
{
    internal class GeoJsonGeometrySerializer : IGeoJsonSerializer
    {
        public bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            try
            {
                resultValue = Serialize(runtimeValue);
                return true;
            }
            catch
            {
                resultValue = null;
                return false;
            }
        }

        public bool IsInstanceOfType(IValueNode valueSyntax)
        {
            if (valueSyntax is null)
            {
                throw new ArgumentNullException(nameof(valueSyntax));
            }

            if (valueSyntax is NullValueNode)
            {
                return true;
            }

            IGeoJsonSerializer geometryType = GetGeometrySerializer(valueSyntax);
            return geometryType.IsInstanceOfType(valueSyntax);
        }

        public bool IsInstanceOfType(object? runtimeValue)
        {
            return runtimeValue is Geometry &&
                SerializersByType.TryGetValue(runtimeValue.GetType(), out var serializer) &&
                serializer.IsInstanceOfType(runtimeValue);
        }

        public object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true)
        {
            if (valueSyntax is NullValueNode)
            {
                return null;
            }

            IGeoJsonSerializer geometryType = GetGeometrySerializer(valueSyntax);
            return geometryType.ParseLiteral(valueSyntax);
        }

        public IValueNode ParseResult(object? resultValue)
        {
            if (resultValue is null)
            {
                return NullValueNode.Default;
            }

            if (resultValue is Geometry)
            {
                return ParseValue(resultValue);
            }

            if (resultValue is IReadOnlyDictionary<string, object> dict)
            {
                if (!dict.TryGetValue(TypeFieldName, out var typeObject) ||
                    !(typeObject is string type))
                {
                    throw Serializer_TypeIsMissing();
                }

                if (GeoJsonTypeSerializer.Default.TryParseString(
                    type,
                    out GeoJsonGeometryType kind))
                {
                    return Serializers[kind].ParseResult(resultValue);
                }

                throw Geometry_Parse_InvalidGeometryKind(type);
            }

            throw Serializer_CouldNotParseValue();
        }


        public IValueNode ParseValue(object? runtimeValue)
        {
            if (runtimeValue is null)
            {
                return NullValueNode.Default;
            }

            if (runtimeValue is IReadOnlyDictionary<string, object> ||
                runtimeValue is IDictionary<string, object>)
            {
                return ParseResult(runtimeValue);
            }

            if (!(runtimeValue is Geometry geometry))
            {
                throw Geometry_Parse_InvalidGeometryType(runtimeValue.GetType());
            }

            if (!TryGetGeometryKind(geometry, out GeoJsonGeometryType kind))
            {
                throw Geometry_Parse_TypeIsUnknown(geometry.GeometryType);
            }

            if (!Serializers.TryGetValue(kind, out var geometryType))
            {
                throw Geometry_Serializer_NotFound(kind);
            }

            return geometryType.ParseValue(runtimeValue);
        }

        public object? Serialize(object? runtimeValue)
        {
            if (runtimeValue is null)
            {
                return null;
            }

            if (runtimeValue is IReadOnlyDictionary<string, object> ||
                runtimeValue is IDictionary<string, object>)
            {
                return runtimeValue;
            }

            if (!(runtimeValue is Geometry geometry))
            {
                throw Geometry_Serialize_InvalidGeometryType(runtimeValue.GetType());
            }

            if (!TryGetGeometryKind(geometry, out GeoJsonGeometryType kind))
            {
                throw Geometry_Serialize_TypeIsUnknown(geometry.GeometryType);
            }

            return Serializers[kind].Serialize(runtimeValue);
        }

        public object? Deserialize(object? resultValue)
        {
            if (resultValue is null)
            {
                return null;
            }

            if (resultValue is Geometry geometry)
            {
                if (!TryGetGeometryKind(geometry, out GeoJsonGeometryType kind))
                {
                    throw Geometry_Deserialize_TypeIsUnknown(geometry.GeometryType);
                }

                return Serializers[kind].Deserialize(resultValue);
            }

            if (resultValue is IReadOnlyDictionary<string, object> dict)
            {
                if (!dict.TryGetValue(TypeFieldName, out var typeObject) ||
                    !(typeObject is string type))
                {
                    throw Geometry_Deserialize_TypeIsMissing();
                }

                if (GeoJsonTypeSerializer.Default.TryParseString(
                    type,
                    out GeoJsonGeometryType kind))
                {
                    return Serializers[kind].Deserialize(resultValue);
                }

                throw Geometry_Deserialize_TypeIsUnknown(type);
            }

            throw Serializer_CouldNotDeserialize();
        }

        public bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            try
            {
                runtimeValue = Deserialize(resultValue);
                return false;
            }
            catch
            {
                runtimeValue = null;
                return false;
            }
        }

        private IGeoJsonSerializer GetGeometrySerializer(IValueNode valueSyntax)
        {
            valueSyntax.EnsureObjectValueNode(out var obj);

            if (!TryGetGeometryKind(obj, out GeoJsonGeometryType geometryType))
            {
                throw Geometry_Parse_InvalidType();
            }

            if (!Serializers.TryGetValue(geometryType, out var serializer))
            {
                throw Geometry_Serializer_NotFound(geometryType);
            }

            return serializer;
        }

        private bool TryGetGeometryKind(
            Geometry geometry,
            out GeoJsonGeometryType geometryType) =>
            GeoJsonTypeSerializer.Default.TryParseString(geometry.GeometryType, out geometryType);

        private bool TryGetGeometryKind(
            ObjectValueNode valueSyntax,
            out GeoJsonGeometryType geometryType)
        {
            IReadOnlyList<ObjectFieldNode> fields = valueSyntax.Fields;
            for (var i = 0; i < fields.Count; i++)
            {
                if (fields[i].Name.Value == TypeFieldName &&
                    GeoJsonTypeSerializer.Default.ParseLiteral(fields[i].Value) is
                        GeoJsonGeometryType type)
                {
                    geometryType = type;
                    return true;
                }
            }

            geometryType = default;
            return false;
        }

        public static readonly GeoJsonGeometrySerializer Default =
            new GeoJsonGeometrySerializer();
    }
}
