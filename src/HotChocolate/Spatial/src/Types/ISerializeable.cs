using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Spatial;
using HotChocolate.Utilities;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;
using static HotChocolate.Types.Spatial.WellKnownFields;

namespace HotChocolate.Types
{
    /// <summary>
    /// A serializable type can serialize its runtime value to the result value
    /// format and deserialize the result value format back to its runtime value.
    /// </summary>
    public interface IGeoJsonSerializer
    {
        /// <summary>
        /// Serializes a runtime value of this type to the result value format.
        /// </summary>
        /// <param name="runtimeValue">
        /// A runtime value representation of this type.
        /// </param>
        /// <returns>
        /// Returns a result value representation of this type.
        /// </returns>
        /// <exception cref="SerializationException">
        /// Unable to serialize the given <paramref name="runtimeValue"/>.
        /// </exception>
        object? Serialize(object? runtimeValue);

        /// <summary>
        /// Deserializes a result value of this type to the runtime value format.
        /// </summary>
        /// <param name="resultValue">
        /// A result value representation of this type.
        /// </param>
        /// <returns>
        /// Returns a runtime value representation of this type.
        /// </returns>
        object? Deserialize(object? resultValue);

        bool TryDeserialize(object? resultValue, out object? runtimeValue);

        /// <summary>
        /// Defines if the given <paramref name="valueSyntax"/> is possibly of this type.
        /// </summary>
        /// <param name="valueSyntax">
        /// The GraphQL value syntax which shall be validated.
        /// </param>
        /// <returns>
        /// <c>true</c> if the given <paramref name="valueSyntax"/> is possibly of this type.
        /// </returns>
        bool IsInstanceOfType(IValueNode valueSyntax);

        /// <summary>
        /// Defines if the given <paramref name="runtimeValue"/> is possibly of this type.
        /// </summary>
        /// <param name="runtimeValue">
        /// The runtime value which shall be validated.
        /// </param>
        /// <returns>
        /// <c>true</c> if the given <paramref name="runtimeValue"/> is possibly of this type.
        /// </returns>
        bool IsInstanceOfType(object? runtimeValue);

        /// <summary>
        /// Parses the GraphQL value syntax of this type into a runtime value representation.
        /// </summary>
        /// <param name="valueSyntax">
        /// A GraphQL value syntax representation of this type.
        /// </param>
        /// <param name="withDefaults">
        /// Specifies if default values shall be used if a field value us not provided.
        /// </param>
        /// <returns>
        /// Returns a runtime value representation of this type.
        /// </returns>
        object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true);

        /// <summary>
        /// Parses a runtime value of this type into a GraphQL value syntax representation.
        /// </summary>
        /// <param name="runtimeValue">
        /// A result value representation of this type.
        /// </param>
        /// <returns>
        /// Returns a GraphQL value syntax representation of the <paramref name="runtimeValue"/>.
        /// </returns>
        /// <exception cref="SerializationException">
        /// Unable to parse the given <paramref name="runtimeValue"/>
        /// into a GraphQL value syntax representation of this type.
        /// </exception>
        IValueNode ParseValue(object? runtimeValue);

        /// <summary>
        /// Parses a result value of this into a GraphQL value syntax representation.
        /// </summary>
        /// <param name="resultValue">
        /// A result value representation of this type.
        /// </param>
        /// <returns>
        /// Returns a GraphQL value syntax representation of the <paramref name="resultValue"/>.
        /// </returns>
        /// <exception cref="SerializationException">
        /// Unable to parse the given <paramref name="resultValue"/>
        /// into a GraphQL value syntax representation of this type.
        /// </exception>
        IValueNode ParseResult(object? resultValue);
    }

    public abstract class GeoJsonSerializerBase<T> : IGeoJsonSerializer
    {
        public abstract object? Deserialize(object? resultValue);
        public abstract bool IsInstanceOfType(IValueNode valueSyntax);
        public abstract bool IsInstanceOfType(object? runtimeValue);
        public abstract object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true);
        public abstract IValueNode ParseResult(object? resultValue);
        public abstract IValueNode ParseValue(object? runtimeValue);
        public abstract object? Serialize(object? runtimeValue);
        public abstract bool TryDeserialize(object? resultValue, out object? runtimeValue);

        protected GeoJsonGeometryType ParseGeometryKind(
            ObjectValueNode valueNode,
            int fieldIndex)
        {
            if (fieldIndex == -1)
            {
                throw InvalidStructure_TypeIsMissing(null!);
            }

            IValueNode typeValue = valueNode.Fields[fieldIndex].Value;

            if (!(_typeField.Type.ParseLiteral(typeValue) is GeoJsonGeometryType type))
            {
                throw InvalidStructure_TypeCannotBeNull(null!);
            }

            if (type != GeometryType)
            {
                throw InvalidStructure_IsOfWrongGeometryType(type, null!);
            }

            return type;
        }

        protected Coordinate ParsePoint(
            ObjectValueNode valueNode,
            int fieldIndex)
        {
            if (fieldIndex == -1)
            {
                throw InvalidStructure_CoordinatesIsMissing(null!);
            }

            IValueNode coordinatesValue = valueNode.Fields[fieldIndex].Value;

            if (!(_coordinatesField.Type.ParseLiteral(coordinatesValue) is Coordinate
                coordinates))
            {
                throw InvalidStructure_CoordinatesCannotBeNull(null!);
            }

            return coordinates;
        }

        protected IList<Coordinate> ParseCoordinateValues(
            ObjectValueNode valueNode,
            int fieldIndex,
            int coordinateCount)
        {
            if (fieldIndex == -1)
            {
                throw InvalidStructure_CoordinatesIsMissing(null!);
            }

            IValueNode coordinatesValue = valueNode.Fields[fieldIndex].Value;

            if (!(_coordinatesField.Type.ParseLiteral(coordinatesValue) is IList<Coordinate>
                coordinates))
            {
                throw InvalidStructure_CoordinatesCannotBeNull(null!);
            }

            if (coordinates.Count < coordinateCount)
            {
                throw InvalidStructure_CoordinatesOfWrongFormat(null!);
            }

            return coordinates;
        }

        protected IList<List<Coordinate>> ParseCoordinateParts(
            ObjectValueNode valueNode,
            int fieldIndex,
            int partCount)
        {
            if (fieldIndex == -1)
            {
                throw InvalidStructure_CoordinatesIsMissing(null!);
            }

            IValueNode coordinatesValue = valueNode.Fields[fieldIndex].Value;

            if (!(_coordinatesField.Type.ParseLiteral(coordinatesValue) is List<List<Coordinate>>
                coordinates))
            {
                throw InvalidStructure_CoordinatesCannotBeNull(null!);
            }

            if (coordinates.Count < partCount)
            {
                throw InvalidStructure_CoordinatesOfWrongFormat(null!);
            }

            return coordinates;
        }

        protected bool TryParseCrs(
            ObjectValueNode valueNode,
            int fieldIndex,
            out int srid)
        {
            if (fieldIndex > 0)
            {
                IValueNode crsField = valueNode.Fields[fieldIndex].Value;

                if (_crsField.Type.ParseLiteral(crsField) is int parsedSrid)
                {
                    srid = parsedSrid;
                    return true;
                }
            }

            srid = 0;
            return false;
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
                    coordinates = GeoJsonPositionSerializer.Default.ParseLiteral(syntaxNode);
                }
                else if (CrsFieldName.EqualsInvariantIgnoreCase(fieldName) &&
                    syntaxNode is IntValueNode node &&
                    !node.IsNull())
                {
                    crs = node.ToInt32();
                }
            }

            if (type == null)
            {
                throw new Exception();
            }

            if (coordinates == null)
            {
                throw new Exception();
            }

            return (type.Value, coordinates, crs);
        }
    }

    public class GeoJsonTypeSerializer : IGeoJsonSerializer
    {
        private static readonly IDictionary<string, GeoJsonGeometryType> _nameLookup =
            new Dictionary<string, GeoJsonGeometryType>{
            {"Point", GeoJsonGeometryType.Point},
            {"MultiPoint", GeoJsonGeometryType.MultiPoint},
            {"LineString", GeoJsonGeometryType.LineString},
            {"MultiLineString", GeoJsonGeometryType.MultiLineString},
            {"Polygon", GeoJsonGeometryType.Polygon},
            {"MultiPolygon", GeoJsonGeometryType.MultiPolygon},
            {"GeometryCollection", GeoJsonGeometryType.GeometryCollection},
            };

        private static readonly IDictionary<GeoJsonGeometryType, string> _valueLookup =
            new Dictionary<GeoJsonGeometryType, string>{
            {GeoJsonGeometryType.Point, "Point"},
            {GeoJsonGeometryType.MultiPoint, "MultiPoint"},
            {GeoJsonGeometryType.LineString, "LineString"},
            {GeoJsonGeometryType.MultiLineString, "MultiLineString"},
            {GeoJsonGeometryType.Polygon, "Polygon"},
            {GeoJsonGeometryType.MultiPolygon, "MultiPolygon"},
            {GeoJsonGeometryType.GeometryCollection, "GeometryCollection"},
            };

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

        public object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true)
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

            throw new Exception();
        }

        public IValueNode ParseValue(object? runtimeValue)
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

            throw new Exception();
        }

        public IValueNode ParseResult(object? resultValue)
        {
            if (resultValue is null)
            {
                return NullValueNode.Default;
            }

            if (resultValue is string s &&
                _nameLookup.TryGetValue(s, out GeoJsonGeometryType enumValue))
            {
                return new EnumValueNode(enumValue);
            }

            if (resultValue is NameString n &&
                _nameLookup.TryGetValue(n.Value, out enumValue))
            {
                return new EnumValueNode(enumValue);
            }

            if (resultValue is GeoJsonGeometryType value &&
                _valueLookup.TryGetValue(value, out var name))
            {
                return new EnumValueNode(name);
            }

            throw new Exception();
        }

        public object? Serialize(object? runtimeValue)
        {
            if (runtimeValue is null)
            {
                return null;
            }

            if (runtimeValue is GeoJsonGeometryType type &&
                _valueLookup.TryGetValue(type, out var enumValue))
            {
                return enumValue;
            }

            throw new Exception();
        }

        public object? Deserialize(object? resultValue)
        {
            if (TryDeserialize(resultValue, out object? runtimeValue))
            {
                return runtimeValue;
            }

            throw new Exception();
        }

        public bool IsInstanceOfType(object? runtimeValue)
        {
            return runtimeValue is null || runtimeValue is GeoJsonGeometryType;
        }

        public bool TryDeserialize(object? resultValue, out object? runtimeValue)
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

            runtimeValue = false;
            return false;
        }

        public static readonly GeoJsonPointSerializer Default = new GeoJsonPointSerializer();
    }
    public class GeoJsonPositionSerializer : IGeoJsonSerializer
    {
        public bool IsInstanceOfType(IValueNode valueSyntax)
        {
            if (valueSyntax is NullValueNode)
            {
                return true;
            }

            if (valueSyntax is ListValueNode listValueNode)
            {
                int numberOfItems = listValueNode.Items.Count;

                if (numberOfItems != 2 && numberOfItems != 3)
                {
                    return false;
                }

                if (listValueNode.Items[0] is IFloatValueLiteral &&
                    listValueNode.Items[1] is IFloatValueLiteral)
                {
                    if (numberOfItems == 2)
                    {
                        return true;
                    }
                    else if (numberOfItems == 3 &&
                        listValueNode.Items[2] is IFloatValueLiteral)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true)
        {
            if (valueSyntax == null)
            {
                throw PositionScalar_CoordinatesCannotBeNull(null!);
            }

            if (valueSyntax is NullValueNode)
            {
                return null;
            }

            if (valueSyntax is ListValueNode list)
            {
                if (list.Items.Count != 2 && list.Items.Count != 3)
                {
                    throw PositionScalar_InvalidPositionObject(null!);
                }

                if (list.Items[0] is IFloatValueLiteral x &&
                    list.Items[1] is IFloatValueLiteral y)
                {
                    if (list.Items.Count == 2)
                    {
                        return new Coordinate(x.ToDouble(), y.ToDouble());
                    }
                    else if (list.Items.Count == 3 &&
                        list.Items[2] is IFloatValueLiteral z)
                    {
                        return new CoordinateZ(x.ToDouble(), y.ToDouble(), z.ToDouble());
                    }
                }

                throw PositionScalar_InvalidPositionObject(null!);
            }

            throw PositionScalar_InvalidPositionObject(null!);
        }

        public IValueNode ParseValue(object? value)
        {
            // parse Coordinate into valueSyntax
            if (value is null)
            {
                return NullValueNode.Default;
            }

            if (value is not Coordinate coordinate)
            {
                throw PositionScalar_CoordinatesCannotBeNull(null!);
            }

            var xNode = new FloatValueNode(coordinate.X);
            var yNode = new FloatValueNode(coordinate.Y);

            // third optional element (z/elevation)
            if (!double.IsNaN(coordinate.Z))
            {
                var zNode = new FloatValueNode(coordinate.Z);
                return new ListValueNode(xNode, yNode, zNode);
            }

            return new ListValueNode(xNode, yNode);
        }

        public IValueNode ParseResult(object? resultValue)
        {
            if (resultValue is null)
            {
                return NullValueNode.Default;
            }

            if (resultValue is not double[] coordinate)
            {
                throw PositionScalar_CoordinatesCannotBeNull(null!);
            }

            if (coordinate.Length != 2 && coordinate.Length != 3)
            {
                throw PositionScalar_InvalidPositionObject(null!);
            }

            var xNode = new FloatValueNode(coordinate[0]);
            var yNode = new FloatValueNode(coordinate[1]);

            // third optional element (z/elevation)
            if (coordinate.Length > 2)
            {
                var zNode = new FloatValueNode(coordinate[2]);
                return new ListValueNode(xNode, yNode, zNode);
            }

            return new ListValueNode(xNode, yNode);
        }

        public bool TryDeserialize(object? serialized, out object? value)
        {
            // Deserialize from graphql variable
            // List<object> (long) = List<long>

            if (serialized is null)
            {
                value = null;
                return true;
            }

            if (serialized is not IList list)
            {
                value = null;
                return false;
            }

            if (list.Count < 2 || list.Count > 3)
            {
                value = null;
                return false;
            }

            double x;
            double y;
            try
            {
                x = Convert.ToDouble(list[0]);
                y = Convert.ToDouble(list[1]);
            }
            catch (Exception ex) when (ex is FormatException ||
                ex is InvalidCastException ||
                ex is OverflowException)
            {
                value = null;
                return false;
            }

            if (double.IsInfinity(x) || double.IsInfinity(y))
            {
                value = null;
                return false;
            }

            if (list.Count == 2)
            {
                value = new Coordinate(x, y);
                return true;
            }

            try
            {
                double z = Convert.ToDouble(list[2]);
                if (double.IsInfinity(z))
                {
                    value = null;
                    return false;
                }

                value = new CoordinateZ(x, y, z);
                return true;
            }
            catch (Exception ex) when (ex is FormatException ||
                ex is InvalidCastException ||
                ex is OverflowException)
            {
                value = null;
                return false;
            }
        }

        public bool TrySerialize(object? value, out object? serialized)
        {
            if (value is not Coordinate coordinate)
            {
                serialized = null;
                return false;
            }

            if (!double.IsNaN(coordinate.Z))
            {
                serialized = new double[] { coordinate.X, coordinate.Y, coordinate.Z };
                return true;
            }

            serialized = new double[] { coordinate.X, coordinate.Y };
            return true;
        }

        public object? Serialize(object? runtimeValue)
        {
            throw new System.NotImplementedException();
        }

        public object? Deserialize(object? resultValue)
        {
            throw new System.NotImplementedException();
        }

        public bool IsInstanceOfType(object? runtimeValue)
        {
            throw new System.NotImplementedException();
        }

        public static readonly GeoJsonPointSerializer Default = new GeoJsonPointSerializer();
    }

    public class GeoJsonPointSerializer : GeoJsonSerializerBase<Point>
    {
        public static readonly GeoJsonGeometryType Type =
            GeoJsonGeometryType.Point;

        public override object? Deserialize(object? resultValue)
        {
            throw new System.NotImplementedException();
        }

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal is null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is ObjectValueNode
                || literal is NullValueNode;
        }

        public override bool IsInstanceOfType(object? runtimeValue)
        {
            return runtimeValue is GeoJsonGeometryType t && t == Type;
        }

        public override object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true)
        {
            if (valueSyntax is NullValueNode)
            {
                return null;
            }

            valueSyntax.EnsureObjectValueNode(out ObjectValueNode obj);

            (GeoJsonGeometryType type, var coordinates, var crs) = ParseFields(obj);

            if (type != Type)
            {
                throw new Exception();
            }

            if (coordinates is not Coordinate coordinate)
            {
                throw new Exception();
            }

            if (crs is { })
            {
                GeometryFactory factory =
                    NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);
                return factory.CreatePoint(coordinate);
            }

            return new Point(coordinate);
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            if (resultValue is null)
            {
                return NullValueNode.Default;
            }

            if (resultValue is IReadOnlyDictionary<string, object> dict)
            {
                var list = new List<ObjectFieldNode>();


                if (dict.TryGetValue(TypeFieldName, out object? value))
                {
                    list.Add(new ObjectFieldNode(
                        TypeFieldName,
                        GeoJsonTypeSerializer.Default.ParseResult(value)));
                }
               
                if (dict.TryGetValue(CoordinatesFieldName, out value))
                {
                    list.Add(new ObjectFieldNode(
                        CoordinatesFieldName,
                        GeoJsonPositionSerializer.Default.ParseResult(value)));
                }
                
                if (dict.TryGetValue(CrsFieldName, out value) && value is int crs)
                {
                    list.Add(new ObjectFieldNode(
                        CrsFieldName,
                        new IntValueNode(crs)));
                }

                return new ObjectValueNode(list);
            }

            if (resultValue is Point)
            {
                return ParseValue(resultValue);
            }

            throw new Exception();
        }

        public override IValueNode ParseValue(object? runtimeValue)
        {
            if (runtimeValue is null)
            {
                return NullValueNode.Default;
            }

            if (runtimeValue is IReadOnlyDictionary<string, object> dict)
            {
                var list = new List<ObjectFieldNode>();


                if (dict.TryGetValue(TypeFieldName, out object? value))
                {
                    list.Add(new ObjectFieldNode(
                        TypeFieldName,
                        GeoJsonTypeSerializer.Default.ParseResult(value)));
                }
               
                if (dict.TryGetValue(CoordinatesFieldName, out value))
                {
                    list.Add(new ObjectFieldNode(
                        CoordinatesFieldName,
                        GeoJsonPositionSerializer.Default.ParseResult(value)));
                }
                
                if (dict.TryGetValue(CrsFieldName, out value) && value is int crs)
                {
                    list.Add(new ObjectFieldNode(
                        CrsFieldName,
                        new IntValueNode(crs)));
                }

                return new ObjectValueNode(list);
            }


            throw new System.NotImplementedException();
        }

        public override object? Serialize(object? runtimeValue)
        {
            throw new System.NotImplementedException();
        }

        public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            throw new System.NotImplementedException();
        }

        public static readonly GeoJsonPointSerializer Default = new GeoJsonPointSerializer();
    }
}