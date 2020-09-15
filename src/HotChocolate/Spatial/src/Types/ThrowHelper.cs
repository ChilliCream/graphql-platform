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
                GeoJsonGeometryType.Point => Resources.InvalidInputObjectStructure_Point,
                GeoJsonGeometryType.MultiPoint => Resources.InvalidInputObjectStructure_MultiPoint,
                GeoJsonGeometryType.LineString => Resources.InvalidInputObjectStructure_LineString,
                GeoJsonGeometryType.MultiLineString => Resources
                    .InvalidInputObjectStructure_MultiLineString,
                GeoJsonGeometryType.Polygon => Resources.InvalidInputObjectStructure_Polygon,
                GeoJsonGeometryType.MultiPolygon => Resources
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
            GeoJsonGeometryType wrongType,
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
