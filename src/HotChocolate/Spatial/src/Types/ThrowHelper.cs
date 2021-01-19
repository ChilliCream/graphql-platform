using System;
using System.Net.Security;
using HotChocolate.Types.Spatial.Properties;

namespace HotChocolate.Types.Spatial
{
    internal static class ThrowHelper
    {
        public static SerializationException PositionScalar_InvalidPositionObject(IType type) =>
            new SerializationException(Resources.PositionScalar_InvalidPositionObject, type);

        public static SerializationException PositionScalar_CoordinatesCannotBeNull(IType type) =>
            new SerializationException(Resources.PositionScalar_CoordinatesCannotBeNull, type);

        public static ArgumentException Resolver_Type_InvalidGeometryType() =>
            new ArgumentException(Resources.Resolver_Type_InvalidGeometryType);

        public static GeoJsonSerializationException Serializer_CouldNotSerialize() =>
            new GeoJsonSerializationException(Resources.Serializer_CouldNotSerialize);

        public static GeoJsonSerializationException Serializer_CouldNotDeserialize() =>
            new GeoJsonSerializationException(Resources.Serializer_CouldNotDeserialize);

        public static GeoJsonSerializationException Serializer_Parse_TypeIsInvalid() =>
            new GeoJsonSerializationException(Resources.Serializer_Parse_TypeIsInvalid);

        public static GeoJsonSerializationException Serializer_CoordinatesIsMissing() =>
            new GeoJsonSerializationException(Resources.Serializer_Parse_CoordinatesIsMissing);

        public static GeoJsonSerializationException Serializer_TypeIsMissing() =>
            new GeoJsonSerializationException(Resources.Serializer_Parse_TypeIsMissing);

        public static GeoJsonSerializationException Serializer_Parse_CoordinatesIsInvalid() =>
            new GeoJsonSerializationException(Resources.Serializer_Parse_CoordinatesIsInvalid);

        public static GeoJsonSerializationException Serializer_CouldNotParseLiteral() =>
            new GeoJsonSerializationException(Resources.Serializer_CouldNotParseLiteral);

        public static GeoJsonSerializationException Serializer_CouldNotParseValue() =>
            new GeoJsonSerializationException(Resources.Serializer_CouldNotParseValue);

        public static GeoJsonSerializationException Geometry_Deserialize_TypeIsUnknown(
            string type) =>
            new GeoJsonSerializationException(Resources.Geometry_Deserialize_TypeIsUnknown, type);

        public static GeoJsonSerializationException Geometry_Deserialize_TypeIsMissing() =>
            new GeoJsonSerializationException(Resources.Geometry_Deserialize_TypeIsMissing);

        public static GeoJsonSerializationException Geometry_Serialize_InvalidGeometryType(
            Type type) =>
            new GeoJsonSerializationException(
                Resources.Geometry_Serialize_InvalidGeometryType,
                type.Name);

        public static GeoJsonSerializationException Geometry_Parse_InvalidGeometryType(
            Type type) =>
            new GeoJsonSerializationException(
                Resources.Geometry_Parse_InvalidGeometryType,
                type.Name);

        public static GeoJsonSerializationException Geometry_Serialize_TypeIsUnknown(string type) =>
            new GeoJsonSerializationException(Resources.Geometry_Serialize_TypeIsUnknown, type);

        public static GeoJsonSerializationException Geometry_Parse_TypeIsUnknown(string type) =>
            new GeoJsonSerializationException(Resources.Geometry_Parse_TypeIsUnknown, type);

        public static GeoJsonSerializationException Geometry_Serializer_NotFound(
            GeoJsonGeometryType type) =>
            new GeoJsonSerializationException(Resources.Geometry_Serializer_NotFound, type);

        public static GeoJsonSerializationException Geometry_Parse_InvalidGeometryKind(
            string type) =>
            new GeoJsonSerializationException(Resources.Geometry_Parse_InvalidGeometryKind, type);

        public static GeoJsonSerializationException Geometry_Parse_InvalidType() =>
            new GeoJsonSerializationException(Resources.Geometry_Parse_InvalidType);

        public static GraphQLException Transformation_UnknownCRS(int srid) =>
            new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(Resources.Transformation_DefaultCRSNotFound, srid)
                    .SetCode(ErrorCodes.Spatial.UnknowCrs)
                    .Build());

        public static SchemaException Transformation_DefaultCRSNotFound(int srid) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(Resources.Transformation_DefaultCRSNotFound, srid)
                    .Build());

        public static GraphQLException Transformation_Projection_CoodinateMNotSupported() =>
            new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(Resources.Transformation_Projection_CoodinateMNotSupported)
                    .SetCode(ErrorCodes.Spatial.CoordinateMNotSupported)
                    .Build());
    }
}
