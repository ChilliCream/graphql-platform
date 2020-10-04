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

        public abstract T CreateGeometry(object? coordinates, int? crs);

        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
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
                                collection.Geometries[i].Coordinates,
                                out object? coords))
                            {
                                geometryCoords[i] = coords;
                            }
                            else
                            {
                                throw Serializer_CouldNotSerialize();
                            }
                        }

                        coordinate = geometryCoords;
                    }
                    else
                    {
                        if (!TrySerializeCoordinates(p.Coordinates, out coordinate))
                        {
                            throw Serializer_CouldNotSerialize();
                        }
                    }


                    resultValue = new Dictionary<string, object>
                    {
                        { CoordinatesFieldName, coordinate },
                        {
                            TypeFieldName, GeoJsonTypeSerializer.Default.Serialize(_geometryType) ??
                            throw Serializer_CouldNotSerialize()
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

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal is null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is ObjectValueNode ||
                literal is NullValueNode;
        }

        public override bool IsInstanceOfType(object? runtimeValue)
        {
            return runtimeValue is T t &&
                GeoJsonTypeSerializer.Default.TryParseString(
                    t.GeometryType,
                    out GeoJsonGeometryType g) &&
                g == _geometryType;
        }

        public override object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true)
        {
            if (valueSyntax is NullValueNode)
            {
                return null;
            }

            valueSyntax.EnsureObjectValueNode(out ObjectValueNode obj);

            (GeoJsonGeometryType type, var coordinates, int? crs) = ParseFields(obj);

            if (type != _geometryType)
            {
                throw Serializer_Parse_TypeIsInvalid();
            }

            return CreateGeometry(coordinates, crs);
        }

        public override IValueNode ParseResult(object? resultValue)
        {
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
                        GeoJsonTypeSerializer.Default.ParseResult(_geometryType))
                };


                if (dict.TryGetValue(CoordinatesFieldName, out var value) &&
                    value is IList coordinates)
                {
                    list.Add(
                        new ObjectFieldNode(
                            CoordinatesFieldName,
                            ParseCoordinates(coordinates)));
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
                return ParseValue(resultValue);
            }

            throw Serializer_CouldNotParseValue();
        }


        public override IValueNode ParseValue(object? runtimeValue)
        {
            if (runtimeValue is null)
            {
                return NullValueNode.Default;
            }

            if (runtimeValue is IReadOnlyDictionary<string, object> dict)
            {
                return ParseResult(dict);
            }

            if (runtimeValue is T geometry)
            {
                var list = new List<ObjectFieldNode>
                {
                    new ObjectFieldNode(
                        TypeFieldName,
                        GeoJsonTypeSerializer.Default.ParseResult(_geometryType)),
                    new ObjectFieldNode(
                        CrsFieldName,
                        new IntValueNode(geometry.SRID)),
                    new ObjectFieldNode(
                        CoordinatesFieldName,
                        ParseCoordinates(geometry.Coordinates))
                };

                return new ObjectValueNode(list);
            }

            throw Serializer_CouldNotParseValue();
        }

        public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            try
            {
                if (resultValue is null)
                {
                    runtimeValue = null;
                    return true;
                }

                if (resultValue is IReadOnlyDictionary<string, object> dict)
                {
                    (GeoJsonGeometryType type, var coordinates, var crs) = ParseFields(dict);

                    if (type != _geometryType)
                    {
                        runtimeValue = null;
                        return false;
                    }

                    runtimeValue = CreateGeometry(coordinates, crs);
                    return true;
                }

                if (resultValue is T)
                {
                    runtimeValue = resultValue;
                    return true;
                }

                runtimeValue = null;
                return false;
            }
            catch
            {
                runtimeValue = null;
                return false;
            }
        }

        protected virtual bool TrySerializeCoordinates(
            Coordinate[] runtimeValue,
            [NotNullWhen(true)] out object? resultValue)
        {
            var result = new double[runtimeValue.Length][];
            for (var i = 0; i < result.Length; i++)
            {
                if (GeoJsonPositionSerializer.Default.TrySerialize(
                        runtimeValue[i],
                        out object? serialized) &&
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

        protected virtual IValueNode ParseCoordinates(IList runtimeValue)
        {
            var result = new IValueNode[runtimeValue.Count];
            for (var i = 0; i < result.Length; i++)
            {
                if (runtimeValue[i] is IList nested &&
                    nested.Count > 0 &&
                    nested[0] is IList)
                {
                    result[i] = ParseCoordinates(nested);
                }
                else if (GeoJsonPositionSerializer.Default.ParseResult(runtimeValue[i]) is {} parsed
                )
                {
                    result[i] = parsed;
                }
                else
                {
                    throw Serializer_Parse_CoordinatesIsInvalid();
                }
            }

            return new ListValueNode(result);
        }
    }
}
