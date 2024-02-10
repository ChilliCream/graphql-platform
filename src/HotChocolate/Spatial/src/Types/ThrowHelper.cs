using HotChocolate.Language;
using HotChocolate.Types.Spatial.Properties;
using HotChocolate.Types.Spatial.Serialization;

namespace HotChocolate.Types.Spatial;

internal static class ThrowHelper
{
    public static SerializationException CoordinatesScalar_InvalidCoordinatesObject(IType type)
        => new(Resources.CoordinatesScalar_InvalidCoordinatesObject, type);

    public static SerializationException CoordinatesScalar_CoordinatesCannotBeNull(IType type)
        => new(Resources.CoordinatesScalar_CoordinatesCannotBeNull, type);

    public static SerializationException PositionScalar_InvalidPositionObject(IType type)
        => new(Resources.PositionScalar_InvalidPositionObject, type);

    public static SerializationException PositionScalar_CoordinatesCannotBeNull(IType type)
        => new(Resources.PositionScalar_CoordinatesCannotBeNull, type);

    public static ArgumentException Resolver_Type_InvalidGeometryType()
        => new(Resources.Resolver_Type_InvalidGeometryType);

    public static SerializationException Serializer_CouldNotSerialize(IType type)
        => new(Resources.Serializer_CouldNotSerialize, type);

    public static SerializationException Serializer_CouldNotDeserialize(IType type)
        => new(Resources.Serializer_CouldNotDeserialize, type);

    public static SerializationException Serializer_Parse_TypeIsInvalid(IType type)
        => new(Resources.Serializer_Parse_TypeIsInvalid, type);

    public static SerializationException Serializer_Parse_ValueKindInvalid(
        IType type,
        SyntaxKind syntaxKind)
        => new("Resources.Serializer_Parse_TypeIsInvalid", type);

    public static SerializationException Serializer_CoordinatesIsMissing(IType type)
        => new(Resources.Serializer_Parse_CoordinatesIsMissing, type);

    public static SerializationException Serializer_TypeIsMissing(IType type)
        => new(Resources.Serializer_Parse_TypeIsMissing, type);

    public static SerializationException Serializer_Parse_CoordinatesIsInvalid(IType type)
        => new(Resources.Serializer_Parse_CoordinatesIsInvalid, type);

    public static SerializationException Serializer_CouldNotParseLiteral(IType type)
        => new(Resources.Serializer_CouldNotParseLiteral, type);

    public static SerializationException Serializer_CouldNotParseValue(IType type)
        => new(Resources.Serializer_CouldNotParseValue, type);

    public static SerializationException Geometry_Deserialize_TypeIsUnknown(
        IType type,
        string typeName)
        => new(string.Format(Resources.Geometry_Deserialize_TypeIsUnknown, typeName), type);

    public static SerializationException Geometry_Deserialize_TypeIsMissing(IType type)
        => new(Resources.Geometry_Deserialize_TypeIsMissing, type);

    public static SerializationException Geometry_Serialize_InvalidGeometryType(
        IType type,
        Type runtimeType)
        => new(
            string.Format(Resources.Geometry_Serialize_InvalidGeometryType, runtimeType.Name),
            type);

    public static SerializationException Geometry_Parse_InvalidGeometryType(
        IType type,
        Type runtimeType)
        => new(
            string.Format(Resources.Geometry_Parse_InvalidGeometryType, runtimeType.Name),
            type);

    public static SerializationException Geometry_Serialize_TypeIsUnknown(
        IType type,
        string typeName)
        => new(
            string.Format(Resources.Geometry_Serialize_TypeIsUnknown, typeName),
            type);

    public static SerializationException Geometry_Parse_TypeIsUnknown(
        IType type,
        string typeName)
        => new(
            string.Format(Resources.Geometry_Parse_TypeIsUnknown, typeName),
            type);

    public static SerializationException Geometry_Serializer_NotFound(
        IType type,
        GeoJsonGeometryType geometryType)
        => new(
            string.Format(Resources.Geometry_Serializer_NotFound, geometryType),
            type);

    public static SerializationException Geometry_Serializer_NotFound(
        IType type,
        string geometryType)
        => new(
            string.Format(Resources.Geometry_Serializer_NotFound, geometryType),
            type);

    public static SerializationException Geometry_Parse_InvalidGeometryKind(
        IType type,
        string typeName)
        => new(
            string.Format(Resources.Geometry_Parse_InvalidGeometryKind, typeName),
            type);

    public static SerializationException Geometry_Parse_InvalidType(IType type)
        => new(Resources.Geometry_Parse_InvalidType, type);

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

    public static GraphQLException Transformation_CoordinateMNotSupported() =>
        new GraphQLException(
            ErrorBuilder.New()
                .SetMessage(Resources.Transformation_CoordinateMNotSupported)
                .SetCode(ErrorCodes.Spatial.CoordinateMNotSupported)
                .Build());

    public static SerializationException Serializer_OperationIsNotSupported(
        IType type,
        IGeoJsonSerializer serializer,
        string method) =>
        new(
            ErrorBuilder
                .New()
                .SetMessage(Resources.Serializer_OperationIsNotSupported,
                    serializer.GetType().FullName ?? serializer.GetType().Name,
                    method)
                .Build(),
            type);
}
