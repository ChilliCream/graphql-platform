using System.Collections.Generic;
using HotChocolate.Language;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.WellKnownFields;
using static HotChocolate.Types.GeoJsonSerializers;
using static HotChocolate.Types.Spatial.ThrowHelper;
using static HotChocolate.Types.Spatial.WellKnownTypeNames;

namespace HotChocolate.Types.Spatial
{
    public sealed class GeometryType : ScalarType<Geometry>
    {
        public GeometryType() : base(GeometryTypeName)
        {
        }

        public GeometryType(
            NameString name,
            BindingBehavior bind = BindingBehavior.Explicit) : base(name, bind)
        {
        }

        public override object? Deserialize(object? resultValue)
        {
            if (resultValue is null)
            {
                return NullValueNode.Default;
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

        public override object? Serialize(object? runtimeValue)
        {
            if (runtimeValue is null)
            {
                return null;
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

        public override bool IsInstanceOfType(IValueNode valueSyntax)
        {
            if (valueSyntax is NullValueNode)
            {
                return true;
            }

            IGeoJsonSerializer geometryType = GetGeometrySerializer(valueSyntax);
            return geometryType.IsInstanceOfType(valueSyntax);
        }

        public override object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true)
        {
            if (valueSyntax is NullValueNode)
            {
                return null;
            }

            IGeoJsonSerializer geometryType = GetGeometrySerializer(valueSyntax);
            return geometryType.ParseLiteral(valueSyntax);
        }

        public override IValueNode ParseValue(object? runtimeValue)
        {
            if (runtimeValue is null)
            {
                return NullValueNode.Default;
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

        public override IValueNode ParseResult(object? resultValue)
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
    }
}
