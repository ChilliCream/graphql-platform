using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;
using static HotChocolate.Types.Spatial.WellKnownFields;

namespace HotChocolate.Types.Spatial.Serialization
{
    internal class GeoJsonMultiPolygonSerializer
        : GeoJsonInputObjectSerializer<MultiPolygon>
    {
        private GeoJsonMultiPolygonSerializer()
            : base(GeoJsonGeometryType.MultiPolygon)
        {
        }

        public override MultiPolygon CreateGeometry(
            IType type,
            object? coordinates,
            int? crs)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Polygon[]? geometries;

            if (coordinates is List<List<Coordinate>> { Count: > 0 } list)
            {
                geometries = new Polygon[list.Count];

                for (var i = 0; i < list.Count; i++)
                {
                    geometries[i] =
                        GeoJsonPolygonSerializer.Default
                            .CreateGeometry(type, list[i].ToArray(), crs);
                }

                goto Success;
            }

            if (coordinates is Coordinate[][] { Length: > 0 } parts)
            {
                geometries = new Polygon[parts.Length];

                for (var i = 0; i < parts.Length; i++)
                {
                    geometries[i] =
                        GeoJsonPolygonSerializer.Default
                            .CreateGeometry(type, parts[i], crs);
                }

                goto Success;
            }

            goto Error;

            Success:
            if (crs is not null)
            {
                GeometryFactory factory =
                    NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

                return factory.CreateMultiPolygon(geometries);
            }

            return new MultiPolygon(geometries);

            Error:
            throw Serializer_Parse_CoordinatesIsInvalid(type);
        }

        public override object CreateInstance(IType type, object?[] fieldValues)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (fieldValues[0] is not GeoJsonGeometryType.MultiPolygon)
            {
                throw Geometry_Parse_InvalidType(type);
            }

            return CreateGeometry(type, fieldValues[1], (int?)fieldValues[2]);
        }

        public override void GetFieldData(IType type, object runtimeValue, object?[] fieldValues)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (runtimeValue is not MultiPolygon geometry)
            {
                throw Geometry_Parse_InvalidGeometryType(type, runtimeValue.GetType());
            }

            fieldValues[0] = GeoJsonGeometryType.MultiPolygon;
            fieldValues[1] = geometry.Geometries.Select(t => t.Coordinates);
            fieldValues[2] = geometry.SRID;
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

            if (runtimeValue is MultiPolygon geometry)
            {
                var list = new List<ObjectFieldNode>
                {
                    new ObjectFieldNode(
                        TypeFieldName,
                        GeoJsonTypeSerializer.Default.ParseResult(
                            type,
                            GeoJsonGeometryType.MultiPolygon)),
                    new ObjectFieldNode(
                        CoordinatesFieldName,
                        ParseCoordinates(
                            type,
                            geometry.Geometries.Select(t => t.Coordinates).ToArray())),
                    new ObjectFieldNode(
                        CrsFieldName,
                        new IntValueNode(geometry.SRID))
                };

                return new ObjectValueNode(list);
            }

            throw Serializer_CouldNotParseValue(type);
        }

        public static readonly GeoJsonMultiPolygonSerializer Default = new();
    }
}
