using System;
using System.Collections.Generic;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.ThrowHelper;

namespace HotChocolate.Types.Spatial.Serialization
{
    internal class GeoJsonMultiLineStringSerializer
        : GeoJsonInputObjectSerializer<MultiLineString>
    {
        private GeoJsonMultiLineStringSerializer()
            : base(GeoJsonGeometryType.MultiLineString)
        {
        }

        public override MultiLineString CreateGeometry(
            object? coordinates,
            int? crs)
        {
            if (coordinates is List<List<Coordinate>> list)
            {
                if (list.Count == 0)
                {
                    coordinates = Array.Empty<Coordinate[][]>();
                }
                else
                {
                    var temp = new Coordinate[list.Count][];

                    for (var index = 0; index < list.Count; index++)
                    {
                        temp[index] = list[index].ToArray();
                    }

                    coordinates = temp;
                }
            }

            if (!(coordinates is Coordinate[][] parts))
            {
                throw Serializer_Parse_CoordinatesIsInvalid();
            }

            var lineCount = parts.Length;
            var geometries = new LineString[lineCount];

            for (var i = 0; i < lineCount; i++)
            {
                geometries[i] = GeoJsonLineStringSerializer.Default
                    .CreateGeometry(parts[i], crs);
            }

            if (crs is not null)
            {
                GeometryFactory factory =
                    NtsGeometryServices.Instance.CreateGeometryFactory(crs.Value);

                return factory.CreateMultiLineString(geometries);
            }

            return new MultiLineString(geometries);
        }

        public override object CreateInstance(object?[] fieldValues)
        {
            if (fieldValues[0] is not GeoJsonGeometryType.MultiLineString)
            {
                throw Geometry_Parse_InvalidType();
            }

            return CreateGeometry(fieldValues[1], (int?)fieldValues[2]);
        }

        public override void GetFieldData(object runtimeValue, object?[] fieldValues)
        {
            var lineString = (LineString)runtimeValue;
            fieldValues[0] = GeoJsonGeometryType.MultiLineString;
            fieldValues[1] = lineString.Coordinates;
            fieldValues[2] = lineString.SRID;
        }

        public static readonly GeoJsonMultiLineStringSerializer Default = new();
    }
}
