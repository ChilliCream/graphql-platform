using System;
using HotChocolate.Types.Spatial.Properties;

namespace HotChocolate.Types.Spatial
{
    public static class ThrowHelper
    {
        public static Exception InvalidStructure_CoordinatesOfWrongFormat(
            IGeometryType type)
        {
            var message = type.GeometryType switch
            {
                GeoJSONGeometryType.Point => Resources.InvalidInputObjectStructure_Point,
                GeoJSONGeometryType.MultiPoint => Resources.InvalidInputObjectStructure_MultiPoint,
                GeoJSONGeometryType.LineString => Resources.InvalidInputObjectStructure_LineString,
                GeoJSONGeometryType.MultiLineString => Resources
                    .InvalidInputObjectStructure_MultiLineString,
                GeoJSONGeometryType.Polygon => Resources.InvalidInputObjectStructure_Polygon,
                GeoJSONGeometryType.MultiPolygon => Resources
                    .InvalidInputObjectStructure_MultiPolygon,
                _ => throw new NotImplementedException()
            };

            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(
                        Resources.InvalidInputObjectStructure_CoordinatesOfWrongFormat +
                        message,
                        type.GeometryType)
                    .Build(),
                type);
        }

        public static Exception InvalidStructure_IsOfWrongGeometryType(
            GeoJSONGeometryType wrongType,
            IGeometryType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(
                        Resources.InvalidInputObjectStructure_IsOfWrongGeometryType,
                        wrongType,
                        type.GeometryType)
                    .Build(),
                type);
        }

        public static SerializationException InvalidStructure_TypeIsMissing(IType type) =>
            new SerializationException(
                Resources.InvalidInputObjectStructure_TypeIsMissing,
                type);

        public static SerializationException InvalidStructure_CoordinatesIsMissing(IType type) =>
            new SerializationException(
                Resources.InvalidInputObjectStructure_CoordinatesIsMissing,
                type);

        public static SerializationException InvalidStructure_TypeCannotBeNull(IType type) =>
            new SerializationException(
                Resources.InvalidInputObjectStructure_TypeCannotBeNull,
                type);

        public static SerializationException InvalidStructure_CoordinatesCannotBeNull(
            IType type) =>
            new SerializationException(Resources.PositionScalar_CoordinatesCannotBeNull, type);

        public static SerializationException PositionScalar_InvalidPositionObject(IType type) =>
            new SerializationException(Resources.PositionScalar_InvalidPositionObject, type);

        public static SerializationException PositionScalar_CoordinatesCannotBeNull(IType type) =>
            new SerializationException(Resources.PositionScalar_CoordinatesCannotBeNull, type);

        public static ArgumentException Resolver_Type_InvalidGeometryType() =>
            new ArgumentException(Resources.Resolver_Type_InvalidGeometryType);
    }
}
