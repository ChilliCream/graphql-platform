using System;
using System.Globalization;
using HotChocolate.Types.Spatial.Properties;

namespace HotChocolate.Types.Spatial
{
    public static class ThrowHelper
    {
        public static Exception InvalidInputObjectStructure(GeoJSONGeometryType geometryType)
        {
            var errorMessageTemplate =  string.Format(Resources.ThrowHelper_InvalidInputObjectStructure_Base,
                geometryType, CultureInfo.InvariantCulture);

            switch (geometryType)
            {
                case GeoJSONGeometryType.Point:
                    return new InputObjectSerializationException(errorMessageTemplate +
                        Resources.ThrowHelper_InvalidInputObjectStructure_Point);
                case GeoJSONGeometryType.MultiPoint:
                    return new InputObjectSerializationException(errorMessageTemplate +
                        Resources.ThrowHelper_InvalidInputObjectStructure_MultiPoint);
                case GeoJSONGeometryType.LineString:
                    return new InputObjectSerializationException(errorMessageTemplate +
                        Resources.ThrowHelper_InvalidInputObjectStructure_LineString);
                case GeoJSONGeometryType.MultiLineString:
                    return new InputObjectSerializationException(errorMessageTemplate +
                        Resources.ThrowHelper_InvalidInputObjectStructure_MultiLineString);
                case GeoJSONGeometryType.Polygon:
                    return new InputObjectSerializationException(errorMessageTemplate +
                        Resources.ThrowHelper_InvalidInputObjectStructure_Polygon);
                case GeoJSONGeometryType.MultiPolygon:
                    return new InputObjectSerializationException(errorMessageTemplate +
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
    }
}
