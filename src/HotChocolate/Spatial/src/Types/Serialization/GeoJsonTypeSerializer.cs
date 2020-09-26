using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types.Spatial;
using static HotChocolate.Types.Spatial.GeoJsonGeometryType;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types
{
    internal class GeoJsonTypeSerializer : GeoJsonSerializerBase<GeoJsonGeometryType>
    {
        private static readonly IDictionary<string, GeoJsonGeometryType> _nameLookup =
            new Dictionary<string, GeoJsonGeometryType>
            {
                { nameof(Point), Point },
                { nameof(MultiPoint), MultiPoint },
                { nameof(LineString), LineString },
                { nameof(MultiLineString), MultiLineString },
                { nameof(Polygon), Polygon },
                { nameof(MultiPolygon), MultiPolygon },
                { nameof(GeometryCollection), GeometryCollection }
            };

        private static readonly IDictionary<GeoJsonGeometryType, string> _valueLookup =
            new Dictionary<GeoJsonGeometryType,
                string>
            {
                { Point, nameof(Point) },
                { MultiPoint, nameof(MultiPoint) },
                { LineString, nameof(LineString) },
                { MultiLineString, nameof(MultiLineString) },
                { Polygon, nameof(Polygon) },
                { MultiPolygon, nameof(MultiPolygon) },
                { GeometryCollection, nameof(GeometryCollection) }
            };

        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            if (runtimeValue is null)
            {
                resultValue = null;
                return true;
            }

            if (runtimeValue is GeoJsonGeometryType type &&
                _valueLookup.TryGetValue(type, out var enumValue))
            {
                resultValue = enumValue;
                return true;
            }

            resultValue = null;
            return false;
        }

        public override bool IsInstanceOfType(IValueNode valueSyntax)
        {
            if (valueSyntax is null)
            {
                throw new ArgumentNullException(nameof(valueSyntax));
            }

            if (valueSyntax is NullValueNode)
            {
                return true;
            }

            if (valueSyntax is EnumValueNode ev)
            {
                return _nameLookup.ContainsKey(ev.Value);
            }

            if (valueSyntax is StringValueNode sv)
            {
                return _nameLookup.ContainsKey(sv.Value);
            }

            return false;
        }

        public override object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true)
        {
            if (valueSyntax is null)
            {
                throw new ArgumentNullException(nameof(valueSyntax));
            }

            if (valueSyntax is EnumValueNode evn &&
                _nameLookup.TryGetValue(evn.Value, out GeoJsonGeometryType ev))
            {
                return ev;
            }

            if (valueSyntax is StringValueNode svn &&
                _nameLookup.TryGetValue(svn.Value, out ev))
            {
                return ev;
            }

            if (valueSyntax is NullValueNode)
            {
                return null;
            }

            throw Serializer_CouldNotParseLiteral();
        }

        public override IValueNode ParseValue(object? runtimeValue)
        {
            if (runtimeValue is null)
            {
                return NullValueNode.Default;
            }

            if (runtimeValue is GeoJsonGeometryType value &&
                _valueLookup.TryGetValue(value, out var enumValue))
            {
                return new EnumValueNode(enumValue);
            }

            throw Serializer_CouldNotParseValue();
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            if (resultValue is null)
            {
                return NullValueNode.Default;
            }

            if (resultValue is string s &&
                _nameLookup.ContainsKey(s))
            {
                return new EnumValueNode(s);
            }

            if (resultValue is NameString n &&
                _nameLookup.ContainsKey(n.Value))
            {
                return new EnumValueNode(n.Value);
            }

            if (resultValue is GeoJsonGeometryType value &&
                _valueLookup.TryGetValue(value, out var name))
            {
                return new EnumValueNode(name);
            }

            throw Serializer_CouldNotParseValue();
        }

        public override bool IsInstanceOfType(object? runtimeValue)
        {
            return runtimeValue is null || runtimeValue is GeoJsonGeometryType;
        }

        public bool TryParseString(string type, out GeoJsonGeometryType geometryType) =>
            _nameLookup.TryGetValue(type, out geometryType);

        public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            if (resultValue is null)
            {
                runtimeValue = null;
                return true;
            }

            if (resultValue is string s &&
                _nameLookup.TryGetValue(s, out GeoJsonGeometryType enumValue))
            {
                runtimeValue = enumValue;
                return true;
            }

            if (resultValue is NameString n &&
                n.HasValue &&
                _nameLookup.TryGetValue(n.Value, out enumValue))
            {
                runtimeValue = enumValue;
                return true;
            }

            if (resultValue is GeoJsonGeometryType type &&
                _valueLookup.ContainsKey(type))
            {
                runtimeValue = type;
                return true;
            }

            runtimeValue = null;
            return false;
        }

        public static readonly GeoJsonTypeSerializer Default = new GeoJsonTypeSerializer();
    }
}
