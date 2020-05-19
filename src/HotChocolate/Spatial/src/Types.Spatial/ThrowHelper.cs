using System;

namespace HotChocolate.Types.Spatial
{
    public static class ThrowHelper
    {
        public static Exception InvalidInputObjectStructure(GeoJSONGeometryType geometryType)
        {
            var errorMessageTemplate = $"Failed to parse {geometryType}. `coordinates` and `type`" +
            " fields are required. `coordinates should be ";

            switch (geometryType)
            {
                case GeoJSONGeometryType.Point:
                    return new InputObjectSerializationException(errorMessageTemplate +
                    "a single position. e.g. `[1,1]`");
                case GeoJSONGeometryType.MultiPoint:
                    return new InputObjectSerializationException(errorMessageTemplate +
                    "an array of positions. e.g. `[[1,1], [2,2]]`");
                case GeoJSONGeometryType.LineString:
                    return new InputObjectSerializationException(errorMessageTemplate +
                    "an array of two or more positions. e.g. `[[1,1], [2,2]]`");
                case GeoJSONGeometryType.MultiLineString:
                    return new InputObjectSerializationException(errorMessageTemplate +
                    "an array of LineStrings. e.g. `[[[1,1], [2,2]], [[0,0], [3,3]]]");
                case GeoJSONGeometryType.Polygon:
                    return new InputObjectSerializationException(errorMessageTemplate +
                    "an closed array of four or more positions.");
                case GeoJSONGeometryType.MultiPolygon:
                    return new InputObjectSerializationException(errorMessageTemplate +
                    "an array of Polygons.");
                case GeoJSONGeometryType.GeometryCollection:
                    return new NotImplementedException();
                default:
                    return new NotImplementedException();
            }

        }

        public static ScalarSerializationException InvalidPositionScalar() => new ScalarSerializationException(
            "A valid position object must be a list of two or three int or float literals " +
            "representing a position. e.g. [1,1] or [2,2,0]"
        );

        public static ArgumentNullException NullPositionScalar() => new ArgumentNullException(
            "A valid position object must be a list of two or three int or float literals " +
            "representing a position. e.g. [1,1] or [2,2,0]"
        );
    }
}
