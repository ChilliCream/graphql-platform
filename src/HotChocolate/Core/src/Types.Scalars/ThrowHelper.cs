namespace HotChocolate.Types;

internal static class ThrowHelper
{
    public static LeafCoercionException EmailAddressType_ParseLiteral_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.EmailAddressType_IsInvalid_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.EmailAddress)
                .Build(),
            type);
    }

    public static LeafCoercionException EmailAddressType_ParseValue_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.EmailAddressType_IsInvalid_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.EmailAddress)
                .Build(),
            type);
    }

    public static LeafCoercionException HexColorType_ParseValue_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.HexColorType_IsInvalid_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.HexColor)
                .Build(),
            type);
    }

    public static LeafCoercionException HexColorType_ParseLiteral_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.HexColorType_IsInvalid_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.HexColor)
                .Build(),
            type);
    }

    public static LeafCoercionException HslType_ParseValue_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.HslType_IsInvalid_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.Hsl)
                .Build(),
            type);
    }

    public static LeafCoercionException HslType_ParseLiteral_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.HslType_IsInvalid_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.Hsl)
                .Build(),
            type);
    }

    public static LeafCoercionException HslaType_ParseValue_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.HslaType_IsInvalid_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.Hsla)
                .Build(),
            type);
    }

    public static LeafCoercionException HslaType_ParseLiteral_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.HslaType_IsInvalid_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.Hsla)
                .Build(),
            type);
    }

    public static LeafCoercionException IPv4Type_ParseValue_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.IPv4Type_IsInvalid_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.IPv4)
                .Build(),
            type);
    }

    public static LeafCoercionException IPv4Type_ParseLiteral_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.IPv4Type_IsInvalid_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.IPv4)
                .Build(),
            type);
    }

    public static LeafCoercionException IPv6Type_ParseValue_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.IPv6Type_IsInvalid_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.IPv6)
                .Build(),
            type);
    }

    public static LeafCoercionException IPv6Type_ParseLiteral_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.IPv6Type_IsInvalid_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.IPv6)
                .Build(),
            type);
    }

    public static LeafCoercionException IsbnType_ParseValue_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.IsbnType_IsInvalid_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.Isbn)
                .Build(),
            type);
    }

    public static LeafCoercionException IsbnType_ParseLiteral_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.IsbnType_IsInvalid_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.Isbn)
                .Build(),
            type);
    }

    public static LeafCoercionException LatitudeType_ParseValue_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.LatitudeType_IsInvalid_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.Latitude)
                .Build(),
            type);
    }

    public static LeafCoercionException LatitudeType_ParseLiteral_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.LatitudeType_IsInvalid_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.Latitude)
                .Build(),
            type);
    }

    public static LeafCoercionException LocalCurrencyType_ParseValue_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.LocalCurrencyType_IsInvalid_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.LocalCurrency)
                .Build(),
            type);
    }

    public static LeafCoercionException LocalCurrencyType_ParseLiteral_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.LocalCurrencyType_IsInvalid_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.LocalCurrency)
                .Build(),
            type);
    }

    public static LeafCoercionException LongitudeType_ParseValue_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.LongitudeType_IsInvalid_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.Longitude)
                .Build(),
            type);
    }

    public static LeafCoercionException LongitudeType_ParseLiteral_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.LongitudeType_IsInvalid_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.Longitude)
                .Build(),
            type);
    }

    public static LeafCoercionException MacAddressType_ParseValue_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.MacAddressType_IsInvalid_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.MacAddress)
                .Build(),
            type);
    }

    public static LeafCoercionException MacAddressType_ParseLiteral_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.MacAddressType_IsInvalid_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.MacAddress)
                .Build(),
            type);
    }

    public static LeafCoercionException NegativeFloatType_ParseLiteral_IsNotNegative(
        IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.NegativeFloatType_IsNotNegative_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.NegativeFloat)
                .Build(),
            type);
    }

    public static LeafCoercionException NegativeFloatType_ParseValue_IsNotNegative(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.NegativeFloatType_IsNotNegative_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.NegativeFloat)
                .Build(),
            type);
    }

    public static LeafCoercionException NegativeIntType_ParseLiteral_IsNotNegative(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.NegativeIntType_IsNotNegative_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.NegativeInt)
                .Build(),
            type);
    }

    public static LeafCoercionException NegativeIntType_ParseValue_IsNotNegative(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.NegativeIntType_IsNotNegative_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.NegativeInt)
                .Build(),
            type);
    }

    public static LeafCoercionException NonEmptyStringType_ParseLiteral_IsEmpty(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.NonEmptyStringType_IsEmpty_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.NonEmptyString)
                .Build(),
            type);
    }

    public static LeafCoercionException NonEmptyStringType_ParseValue_IsEmpty(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.NonEmptyStringType_IsEmpty_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.NonEmptyString)
                .Build(),
            type);
    }

    public static LeafCoercionException NonNegativeIntType_ParseLiteral_IsNotNonNegative(
        IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.NonNegativeIntType_IsNotNonNegative_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.NonNegativeInt)
                .Build(),
            type);
    }

    public static LeafCoercionException NonNegativeIntType_ParseValue_IsNotNonNegative(
        IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.NonNegativeIntType_IsNotNonNegative_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.NonNegativeInt)
                .Build(),
            type);
    }

    public static LeafCoercionException NonPositiveIntType_ParseLiteral_IsNotNonPositive(
        IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.NonPositiveIntType_IsNotNonPositive_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.NonPositiveInt)
                .Build(),
            type);
    }

    public static LeafCoercionException NonPositiveFloatType_ParseLiteral_IsNotNonPositive(
        IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.NonPositiveFloatType_IsNotNonPositive_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.NonPositiveFloat)
                .Build(),
            type);
    }

    public static LeafCoercionException NonPositiveFloatType_ParseValue_IsNotNonPositive(
        IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.NonPositiveFloatType_IsNotNonPositive_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.NonPositiveFloat)
                .Build(),
            type);
    }

    public static LeafCoercionException NonPositiveIntType_ParseValue_IsNotNonPositive(
        IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.NonPositiveIntType_IsNotNonPositive_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.NonPositiveInt)
                .Build(),
            type);
    }

    public static LeafCoercionException NonNegativeFloatType_ParseLiteral_IsNotNonNegative(
        IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.NonNegativeFloatType_IsNotNonNegative_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.NonNegativeFloat)
                .Build(),
            type);
    }

    public static LeafCoercionException NonNegativeFloatType_ParseValue_IsNotNonNegative(
        IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.NonNegativeFloatType_IsNotNonNegative_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.NonNegativeFloat)
                .Build(),
            type);
    }

    public static LeafCoercionException PhoneNumber_ParseLiteral_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.PhoneNumberType_IsInvalid_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.PhoneNumber)
                .Build(),
            type);
    }

    public static LeafCoercionException PhoneNumber_ParseValue_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.PhoneNumberType_IsInvalid_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.PhoneNumber)
                .Build(),
            type);
    }

    public static LeafCoercionException PortType_ParseLiteral_OutOfRange(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.PortType_OutOfRange_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.Port)
                .Build(),
            type);
    }

    public static LeafCoercionException PortType_ParseValue_OutOfRange(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.PortType_OutOfRange_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.Port)
                .Build(),
            type);
    }

    public static LeafCoercionException PositiveIntType_ParseLiteral_ZeroOrLess(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.PositiveIntType_ZeroOrLess_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.PositiveInt)
                .Build(),
            type);
    }

    public static LeafCoercionException PositiveIntType_ParseValue_ZeroOrLess(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.PositiveIntType_ZeroOrLess_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.PositiveInt)
                .Build(),
            type);
    }

    public static LeafCoercionException PostalCodeType_ParseLiteral_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.PostalCodeType_IsInvalid_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.PostalCode)
                .Build(),
            type);
    }

    public static LeafCoercionException PostalCodeType_ParseValue_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.PostalCodeType_IsInvalid_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.PostalCode)
                .Build(),
            type);
    }

    public static LeafCoercionException RegexType_InvalidFormat(
        IType type,
        string name)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(
                    string.Format(
                        ScalarResources.RegexType_InvalidFormat,
                        name))
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .Build(),
            type);
    }

    public static LeafCoercionException RgbType_ParseLiteral_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.RgbType_IsInvalid_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.Rgb)
                .Build(),
            type);
    }

    public static LeafCoercionException RgbType_ParseValue_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.RgbType_IsInvalid_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.Rgb)
                .Build(),
            type);
    }

    public static LeafCoercionException RgbaType_ParseValue_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.RgbaType_IsInvalid_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.Rgba)
                .Build(),
            type);
    }

    public static LeafCoercionException RgbaType_ParseLiteral_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.RgbaType_IsInvalid_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.Rgba)
                .Build(),
            type);
    }

    public static LeafCoercionException UnsignedShortType_ParseValue_IsNotUnsigned(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.UnsignedShortType_IsNotUnsigned_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.UnsignedShort)
                .Build(),
            type);
    }

    public static LeafCoercionException UnsignedShortType_ParseLiteral_IsNotUnsigned(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.UnsignedShortType_IsNotUnsigned_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.UnsignedShort)
                .Build(),
            type);
    }

    public static LeafCoercionException SignedByteType_ParseValue_IsNotSigned(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.SignedByteType_IsNotSigned_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.SignedByte)
                .Build(),
            type);
    }

    public static LeafCoercionException SignedByteType_ParseLiteral_IsNotSigned(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.SignedByteType_IsNotSigned_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.SignedByte)
                .Build(),
            type);
    }

    public static LeafCoercionException UnsignedIntType_ParseValue_IsNotUnsigned(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.UnsignedIntType_IsNotUnsigned_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.UnsignedInt)
                .Build(),
            type);
    }

    public static LeafCoercionException UnsignedIntType_ParseLiteral_IsNotUnsigned(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.UnsignedIntType_IsNotUnsigned_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.UnsignedInt)
                .Build(),
            type);
    }

    public static LeafCoercionException UnsignedLongType_ParseValue_IsNotUnsigned(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.UnsignedLongType_IsNotUnsigned_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.UnsignedLong)
                .Build(),
            type);
    }

    public static LeafCoercionException UnsignedLongType_ParseLiteral_IsNotUnsigned(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.UnsignedLongType_IsNotUnsigned_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.UnsignedLong)
                .Build(),
            type);
    }

    public static LeafCoercionException UtcOffset_ParseValue_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.UtcOffsetType_IsInvalid_ParseValue)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .SetExtension("actualType", WellKnownScalarTypes.UtcOffset)
                .Build(),
            type);
    }

    public static LeafCoercionException UtcOffset_ParseLiteral_IsInvalid(IType type)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ScalarResources.UtcOffsetType_IsInvalid_ParseLiteral)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .SetExtension("actualType", WellKnownScalarTypes.UtcOffset)
                .Build(),
            type);
    }
}
