using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;
using static HotChocolate.Types.Spatial.WellKnownFields;

namespace HotChocolate.Types.Spatial.Serialization
{
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

        public override bool TrySerialize(IType type, object? runtimeValue, out object? resultValue)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            try
            {
                if (runtimeValue is null)
                {
                    resultValue = null;
                    return true;
                }

                if (runtimeValue is IReadOnlyDictionary<string, object> ||
                    runtimeValue is IDictionary<string, object>)
                {
                    resultValue = runtimeValue;
                    return true;
                }

                if (runtimeValue is T p)
                {
                    object? coordinate;
                    if (p is GeometryCollection collection)
                    {
                        var geometryCoords = new object[collection.Geometries.Length];
                        for (var i = 0; i < collection.Geometries.Length; i++)
                        {
                            if (TrySerializeCoordinates(
                                type,
                                collection.Geometries[i].Coordinates,
                                out var coords))
                            {
                                geometryCoords[i] = coords;
                            }
                            else
                            {
                                throw Serializer_CouldNotSerialize(type);
                            }
                        }

                        coordinate = geometryCoords;
                    }
                    else
                    {
                        if (!TrySerializeCoordinates(type, p.Coordinates, out coordinate))
                        {
                            throw Serializer_CouldNotSerialize(type);
                        }
                    }

                    resultValue = new Dictionary<string, object>
                    {
                        { CoordinatesFieldName, coordinate },
                        {
                            TypeFieldName,
                            GeoJsonTypeSerializer.Default.Serialize(type, _geometryType) ??
                                throw Serializer_CouldNotSerialize(type)
                        },
                        { CrsFieldName, p.SRID }
                    };

                    return true;
                }

                resultValue = null;
                return false;
            }
            catch
            {
                resultValue = null;
                return false;
            }
        }

        public override bool IsInstanceOfType(IType type, IValueNode literal)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (literal is null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is ObjectValueNode ||
                literal is NullValueNode;
        }

        public override bool IsInstanceOfType(IType type, object? runtimeValue)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return runtimeValue is T t &&
                GeoJsonTypeSerializer.Default.TryParseString(
                    t.GeometryType,
                    out GeoJsonGeometryType g) &&
                g == _geometryType;
        }

        public override object? ParseLiteral(IType type, IValueNode valueSyntax)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (valueSyntax is NullValueNode)
            {
                return null;
            }

            if (valueSyntax is ObjectValueNode obj)
            {
                (GeoJsonGeometryType geometryType, var coordinates, var crs) =
                    ParseFields(type, obj);

                if (geometryType != _geometryType)
                {
                    throw Serializer_Parse_TypeIsInvalid(type);
                }

                return CreateGeometry(type, coordinates, crs);
            }

            throw Serializer_Parse_ValueKindInvalid(type, valueSyntax.Kind);
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

            if (resultValue is IReadOnlyDictionary<string, object> dict)
            {
                var list = new List<ObjectFieldNode>
                {
                    new ObjectFieldNode(
                        TypeFieldName,
                        GeoJsonTypeSerializer.Default.ParseResult(type, _geometryType))
                };

                if (dict.TryGetValue(CoordinatesFieldName, out var value) &&
                    value is IList coordinates)
                {
                    list.Add(
                        new ObjectFieldNode(
                            CoordinatesFieldName,
                            ParseCoordinates(type, coordinates)));
                }

                if (dict.TryGetValue(CrsFieldName, out value) && value is int crs)
                {
                    list.Add(
                        new ObjectFieldNode(
                            CrsFieldName,
                            new IntValueNode(crs)));
                }

                return new ObjectValueNode(list);
            }

            if (resultValue is T)
            {
                return ParseValue(type, resultValue);
            }

            throw Serializer_CouldNotParseValue(type);
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

            if (runtimeValue is IReadOnlyDictionary<string, object> dict)
            {
                return ParseResult(type, dict);
            }

            if (runtimeValue is T geometry)
            {
                var list = new List<ObjectFieldNode>
                {
                    new ObjectFieldNode(
                        TypeFieldName,
                        GeoJsonTypeSerializer.Default.ParseResult(type, _geometryType)),
                    new ObjectFieldNode(
                        CoordinatesFieldName,
                        ParseCoordinates(type, geometry.Coordinates)),
                    new ObjectFieldNode(
                        CrsFieldName,
                        new IntValueNode(geometry.SRID))
                };

                return new ObjectValueNode(list);
            }

            throw Serializer_CouldNotParseValue(type);
        }

        public override bool TryDeserialize(
            IType type,
            object? resultValue,
            out object? runtimeValue)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            try
            {
                switch (resultValue)
                {
                    case null:
                        runtimeValue = null;
                        return true;

                    case IReadOnlyDictionary<string, object> dict:
                    {
                        (GeoJsonGeometryType geometryType, var coordinates, var crs) =
                            ParseFields(type, dict);

                        if (geometryType != _geometryType)
                        {
                            runtimeValue = null;
                            return false;
                        }

                        runtimeValue = CreateGeometry(type, coordinates, crs);
                        return true;
                    }

                    case T:
                        runtimeValue = resultValue;
                        return true;

                    default:
                        runtimeValue = null;
                        return false;
                }
            }
            catch
            {
                runtimeValue = null;
                return false;
            }
        }

        protected virtual bool TrySerializeCoordinates(
            IType type,
            Coordinate[] runtimeValue,
            [NotNullWhen(true)] out object? resultValue)
        {
            var result = new double[runtimeValue.Length][];
            for (var i = 0; i < result.Length; i++)
            {
                if (GeoJsonPositionSerializer.Default.TrySerialize(type, runtimeValue[i],
                    out var serialized) &&
                    serialized is double[] points)
                {
                    result[i] = points;
                }
                else
                {
                    resultValue = null;
                    return false;
                }
            }

            resultValue = result;
            return true;
        }

        protected virtual IValueNode ParseCoordinates(IType type, IList runtimeValue)
        {
            var result = new IValueNode[runtimeValue.Count];

            for (var i = 0; i < result.Length; i++)
            {
                var element = runtimeValue[i];

                if (element is IList { Count: > 0 } nested && nested[0] is IList or Coordinate)
                {
                    result[i] = ParseCoordinates(type, nested);
                }
                else if (GeoJsonPositionSerializer.Default.ParseResult(type, element) is { } parsed)
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
    }
}
