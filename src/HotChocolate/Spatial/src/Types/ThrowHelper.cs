using System;
using System.Globalization;
using HotChocolate.Spatial.Types.Properties;
using HotChocolate.Types;

namespace HotChocolate.Spatial.Types
{
    public static class ThrowHelper
    {
        public static Exception InvalidInputObjectStructure(GeoJSONGeometryType geometryType)
        {
            var errorMessageTemplate =  string.Format(
                CultureInfo.InvariantCulture,
                Resources.ThrowHelper_InvalidInputObjectStructure_Base,
                geometryType);

            switch (geometryType)
            {
                case GeoJSONGeometryType.Point:
                    return new SerializationException(errorMessageTemplate +
                        Resources.ThrowHelper_InvalidInputObjectStructure_Point);
                case GeoJSONGeometryType.MultiPoint:
                    return new SerializationException(errorMessageTemplate +
                        Resources.ThrowHelper_InvalidInputObjectStructure_MultiPoint);
                case GeoJSONGeometryType.LineString:
                    return new SerializationException(errorMessageTemplate +
                        Resources.ThrowHelper_InvalidInputObjectStructure_LineString);
                case GeoJSONGeometryType.MultiLineString:
                    return new SerializationException(errorMessageTemplate +
                        Resources.ThrowHelper_InvalidInputObjectStructure_MultiLineString);
                case GeoJSONGeometryType.Polygon:
                    return new SerializationException(errorMessageTemplate +
                        Resources.ThrowHelper_InvalidInputObjectStructure_Polygon);
                case GeoJSONGeometryType.MultiPolygon:
                    return new SerializationException(errorMessageTemplate +
                        Resources.ThrowHelper_InvalidInputObjectStructure_MultiPolygon);
                case GeoJSONGeometryType.GeometryCollection:
                    return new NotImplementedException();
                default:
                    return new NotImplementedException();
            }

        }

        public static ScalarSerializationException InvalidPositionScalar() =>
            new ScalarSerializationException(Resources.ThrowHelper_InvalidPositionScalar);

        public static ArgumentNullException NullPositionScalar() =>
            new ArgumentNullException(Resources.ThrowHelper_InvalidPositionScalar);

        // TODO : move to resource
        public static ArgumentException InvalidType() =>
            new ArgumentException("The specified value has to be a Coordinate Type");
    }
}
