using System;
using System.Collections.Generic;
using HotChocolate.Language;
using static HotChocolate.Types.Spatial.GeoJsonGeometryType;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types.Spatial.Serialization;

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
                { nameof(GeometryCollection), GeometryCollection },
        };

    private static readonly IDictionary<GeoJsonGeometryType, string> _valueLookup =
        new Dictionary<GeoJsonGeometryType, string>
        {
                { Point, nameof(Point) },
                { MultiPoint, nameof(MultiPoint) },
                { LineString, nameof(LineString) },
                { MultiLineString, nameof(MultiLineString) },
                { Polygon, nameof(Polygon) },
                { MultiPolygon, nameof(MultiPolygon) },
                { GeometryCollection, nameof(GeometryCollection) },
        };

    public override bool TrySerialize(
        IType type,
        object? runtimeValue,
        out object? resultValue)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (runtimeValue is null)
        {
            resultValue = null;
            return true;
        }

        if (runtimeValue is GeoJsonGeometryType geometryType &&
            _valueLookup.TryGetValue(geometryType, out var enumValue))
        {
            resultValue = enumValue;
            return true;
        }

        resultValue = null;
        return false;
    }

    public override bool IsInstanceOfType(IType type, IValueNode valueSyntax)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

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

    public override object? ParseLiteral(IType type, IValueNode valueSyntax)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (valueSyntax is null)
        {
            throw new ArgumentNullException(nameof(valueSyntax));
        }

        if (valueSyntax is EnumValueNode evn &&
            _nameLookup.TryGetValue(evn.Value, out var ev))
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

        throw Serializer_CouldNotParseLiteral(type);
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

        if (runtimeValue is GeoJsonGeometryType value &&
            _valueLookup.TryGetValue(value, out var enumValue))
        {
            return new EnumValueNode(enumValue);
        }

        throw Serializer_CouldNotParseValue(type);
    }

    public override object CreateInstance(IType type, object?[] fieldValues)
    {
        throw new NotImplementedException();
    }

    public override void GetFieldData(IType type, object runtimeValue, object?[] fieldValues)
    {
        throw new NotImplementedException();
    }

    public override IValueNode ParseResult(IType type, object? resultValue)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (resultValue is null)
        {
            return NullValueNode.Default;
        }

        if (resultValue is string s &&
            _nameLookup.ContainsKey(s))
        {
            return new EnumValueNode(s);
        }

        if (resultValue is GeoJsonGeometryType value &&
            _valueLookup.TryGetValue(value, out var name))
        {
            return new EnumValueNode(name);
        }

        throw Serializer_CouldNotParseValue(type);
    }

    public override bool IsInstanceOfType(IType type, object? runtimeValue)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return runtimeValue is null or GeoJsonGeometryType;
    }

    public bool TryParseString(string type, out GeoJsonGeometryType geometryType) =>
        _nameLookup.TryGetValue(type, out geometryType);

    public override bool TryDeserialize(
        IType type,
        object? resultValue,
        out object? runtimeValue)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (resultValue is null)
        {
            runtimeValue = null;
            return true;
        }

        if (resultValue is string s &&
            _nameLookup.TryGetValue(s, out var enumValue))
        {
            runtimeValue = enumValue;
            return true;
        }

        if (resultValue is GeoJsonGeometryType geometryType &&
            _valueLookup.ContainsKey(geometryType))
        {
            runtimeValue = geometryType;
            return true;
        }

        runtimeValue = null;
        return false;
    }

    public static readonly GeoJsonTypeSerializer Default = new();
}
