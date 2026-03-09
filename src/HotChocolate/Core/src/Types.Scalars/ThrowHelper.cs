namespace HotChocolate.Types;

internal static class ThrowHelper
{
    public static LeafCoercionException EmailAddressType_InvalidFormat(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.EmailAddressType_InvalidFormat)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.EmailAddress)
                .Build(),
            type);
    }

    public static LeafCoercionException HexColorType_InvalidFormat(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.HexColorType_InvalidFormat)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.HexColor)
                .Build(),
            type);
    }

    public static LeafCoercionException HslType_InvalidFormat(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.HslType_InvalidFormat)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.Hsl)
                .Build(),
            type);
    }

    public static LeafCoercionException HslaType_InvalidFormat(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.HslaType_InvalidFormat)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.Hsla)
                .Build(),
            type);
    }

    public static LeafCoercionException IPv4Type_InvalidFormat(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.IPv4Type_InvalidFormat)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.IPv4)
                .Build(),
            type);
    }

    public static LeafCoercionException IPv6Type_InvalidFormat(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.IPv6Type_InvalidFormat)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.IPv6)
                .Build(),
            type);
    }

    public static LeafCoercionException IsbnType_InvalidFormat(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.IsbnType_InvalidFormat)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.Isbn)
                .Build(),
            type);
    }

    public static LeafCoercionException LatitudeType_InvalidFormat(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.LatitudeType_InvalidFormat)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.Latitude)
                .Build(),
            type);
    }

    public static LeafCoercionException LongitudeType_InvalidFormat(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.LongitudeType_InvalidFormat)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.Longitude)
                .Build(),
            type);
    }

    public static LeafCoercionException MacAddressType_InvalidFormat(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.MacAddressType_InvalidFormat)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.MacAddress)
                .Build(),
            type);
    }

    public static LeafCoercionException PhoneNumberType_InvalidFormat(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.PhoneNumberType_InvalidFormat)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.PhoneNumber)
                .Build(),
            type);
    }

    public static LeafCoercionException RgbType_InvalidFormat(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.RgbType_InvalidFormat)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.Rgb)
                .Build(),
            type);
    }

    public static LeafCoercionException RgbaType_InvalidFormat(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.RgbaType_InvalidFormat)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.Rgba)
                .Build(),
            type);
    }

    public static LeafCoercionException UtcOffsetType_InvalidFormat(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.UtcOffsetType_InvalidFormat)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.UtcOffset)
                .Build(),
            type);
    }
}
