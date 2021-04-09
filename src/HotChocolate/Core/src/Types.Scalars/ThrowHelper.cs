namespace HotChocolate.Types
{
    internal static class ThrowHelper
    {
        public static SerializationException EmailAddressType_ParseLiteral_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.EmailAddressType_IsInvalid_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.EmailAddress)
                    .Build(),
                type);
        }

        public static SerializationException EmailAddressType_ParseValue_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.EmailAddressType_IsInvalid_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.EmailAddress)
                    .Build(),
                type);
        }

        public static SerializationException HexColorType_ParseValue_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.HexColorType_IsInvalid_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.HexColor)
                    .Build(),
                type);
        }

        public static SerializationException HexColorType_ParseLiteral_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.HexColorType_IsInvalid_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.HexColor)
                    .Build(),
                type);
        }

        public static SerializationException HslType_ParseValue_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.HslType_IsInvalid_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.Hsl)
                    .Build(),
                type);
        }

        public static SerializationException HslType_ParseLiteral_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.HslType_IsInvalid_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.Hsl)
                    .Build(),
                type);
        }

        public static SerializationException HslaType_ParseValue_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.HslaType_IsInvalid_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.Hsla)
                    .Build(),
                type);
        }

        public static SerializationException HslaType_ParseLiteral_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.HslaType_IsInvalid_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.Hsla)
                    .Build(),
                type);
        }

        public static SerializationException IPv4Type_ParseValue_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.IPv4Type_IsInvalid_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.IPv4)
                    .Build(),
                type);
        }

        public static SerializationException IPv4Type_ParseLiteral_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.IPv4Type_IsInvalid_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.IPv4)
                    .Build(),
                type);
        }

        public static SerializationException IPv6Type_ParseValue_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.IPv6Type_IsInvalid_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.IPv6)
                    .Build(),
                type);
        }

        public static SerializationException IPv6Type_ParseLiteral_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.IPv6Type_IsInvalid_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.IPv6)
                    .Build(),
                type);
        }

        public static SerializationException IsbnType_ParseValue_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.IsbnType_IsInvalid_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.Isbn)
                    .Build(),
                type);
        }

        public static SerializationException IsbnType_ParseLiteral_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.IsbnType_IsInvalid_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.Isbn)
                    .Build(),
                type);
        }

        public static SerializationException LatitudeType_ParseValue_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.LatitudeType_IsInvalid_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.Latitude)
                    .Build(),
                type);
        }

        public static SerializationException LatitudeType_ParseLiteral_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.LatitudeType_IsInvalid_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.Latitude)
                    .Build(),
                type);
        }

        public static SerializationException LocalDateType_ParseValue_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.LocalDateType_IsInvalid_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.LocalDate)
                    .Build(),
                type);
        }

        public static SerializationException LocalDateType_ParseLiteral_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.LocalDateType_IsInvalid_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.LocalDate)
                    .Build(),
                type);
        }

        public static SerializationException LocalTimeType_ParseValue_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.LocalTimeType_IsInvalid_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.LocalTime)
                    .Build(),
                type);
        }

        public static SerializationException LocalTimeType_ParseLiteral_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.LocalTimeType_IsInvalid_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.LocalTime)
                    .Build(),
                type);
        }

        public static SerializationException MacAddressType_ParseValue_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.MacAddressType_IsInvalid_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.MacAddress)
                    .Build(),
                type);
        }

        public static SerializationException MacAddressType_ParseLiteral_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.MacAddressType_IsInvalid_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.MacAddress)
                    .Build(),
                type);
        }

        public static SerializationException NegativeFloatType_ParseLiteral_IsNotNegative(
            IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.NegativeFloatType_IsNotNegative_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.NegativeFloat)
                    .Build(),
                type);
        }

        public static SerializationException NegativeFloatType_ParseValue_IsNotNegative(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.NegativeFloatType_IsNotNegative_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.NegativeFloat)
                    .Build(),
                type);
        }

        public static SerializationException NegativeIntType_ParseLiteral_IsNotNegative(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.NegativeIntType_IsNotNegative_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.NegativeInt)
                    .Build(),
                type);
        }

        public static SerializationException NegativeIntType_ParseValue_IsNotNegative(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.NegativeIntType_IsNotNegative_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.NegativeInt)
                    .Build(),
                type);
        }

        public static SerializationException NonEmptyStringType_ParseLiteral_IsEmpty(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.NonEmptyStringType_IsEmpty_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.NonEmptyString)
                    .Build(),
                type);
        }

        public static SerializationException NonEmptyStringType_ParseValue_IsEmpty(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.NonEmptyStringType_IsEmpty_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.NonEmptyString)
                    .Build(),
                type);
        }

        public static SerializationException NonNegativeIntType_ParseLiteral_IsNotNonNegative(
            IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.NonNegativeIntType_IsNotNonNegative_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.NonNegativeInt)
                    .Build(),
                type);
        }

        public static SerializationException NonNegativeIntType_ParseValue_IsNotNonNegative(
            IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.NonNegativeIntType_IsNotNonNegative_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.NonNegativeInt)
                    .Build(),
                type);
        }

        public static SerializationException NonPositiveIntType_ParseLiteral_IsNotNonPositive(
            IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.NonPositiveIntType_IsNotNonPositive_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.NonPositiveInt)
                    .Build(),
                type);
        }

        public static SerializationException NonPositiveFloatType_ParseLiteral_IsNotNonPositive(
            IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.NonPositiveFloatType_IsNotNonPositive_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.NonPositiveFloat)
                    .Build(),
                type);
        }

        public static SerializationException NonPositiveFloatType_ParseValue_IsNotNonPositive(
            IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.NonPositiveFloatType_IsNotNonPositive_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.NonPositiveFloat)
                    .Build(),
                type);
        }

        public static SerializationException NonPositiveIntType_ParseValue_IsNotNonPositive(
            IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.NonPositiveIntType_IsNotNonPositive_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.NonPositiveInt)
                    .Build(),
                type);
        }

        public static SerializationException NonNegativeFloatType_ParseLiteral_IsNotNonNegative(
            IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.NonNegativeFloatType_IsNotNonNegative_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.NonNegativeFloat)
                    .Build(),
                type);
        }

        public static SerializationException NonNegativeFloatType_ParseValue_IsNotNonNegative(
            IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.NonNegativeFloatType_IsNotNonNegative_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.NonNegativeFloat)
                    .Build(),
                type);
        }

        public static SerializationException PhoneNumber_ParseLiteral_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.PhoneNumberType_IsInvalid_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.PhoneNumber)
                    .Build(),
                type);
        }

        public static SerializationException PhoneNumber_ParseValue_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.PhoneNumberType_IsInvalid_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.PhoneNumber)
                    .Build(),
                type);
        }

        public static SerializationException PortType_ParseLiteral_OutOfRange(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.PortType_OutOfRange_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.Port)
                    .Build(),
                type);
        }

        public static SerializationException PortType_ParseValue_OutOfRange(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.PortType_OutOfRange_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.Port)
                    .Build(),
                type);
        }

        public static SerializationException PositiveIntType_ParseLiteral_ZeroOrLess(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.PositiveIntType_ZeroOrLess_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.PositiveInt)
                    .Build(),
                type);
        }

        public static SerializationException PositiveIntType_ParseValue_ZeroOrLess(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.PositiveIntType_ZeroOrLess_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.PositiveInt)
                    .Build(),
                type);
        }

        public static SerializationException PostalCodeType_ParseLiteral_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.PostalCodeType_IsInvalid_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.PostalCode)
                    .Build(),
                type);
        }

        public static SerializationException PostalCodeType_ParseValue_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.PostalCodeType_IsInvalid_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.PostalCode)
                    .Build(),
                type);
        }

        public static SerializationException RegexType_ParseValue_IsInvalid(
            IType type,
            string name)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(
                        string.Format(
                            ScalarResources.RegexType_IsInvalid_ParseValue,
                            name))
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .Build(),
                type);
        }

        public static SerializationException RegexType_ParseLiteral_IsInvalid(
            IType type,
            string name)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(
                        string.Format(
                            ScalarResources.RegexType_IsInvalid_ParseLiteral,
                            name))
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .Build(),
                type);
        }

        public static SerializationException RgbType_ParseLiteral_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.RgbType_IsInvalid_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.Rgb)
                    .Build(),
                type);
        }

        public static SerializationException RgbType_ParseValue_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.RgbType_IsInvalid_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.Rgb)
                    .Build(),
                type);
        }

        public static SerializationException RgbaType_ParseValue_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.RgbaType_IsInvalid_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.Rgba)
                    .Build(),
                type);
        }

        public static SerializationException RgbaType_ParseLiteral_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.RgbaType_IsInvalid_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.Rgba)
                    .Build(),
                type);
        }

        public static SerializationException UnsignedIntType_ParseValue_IsNotUnsigned(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.UnsignedIntType_IsNotUnsigned_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.UnsignedInt)
                    .Build(),
                type);
        }

        public static SerializationException UnsignedIntType_ParseLiteral_IsNotUnsigned(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.UnsignedIntType_IsNotUnsigned_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.UnsignedInt)
                    .Build(),
                type);
        }

        public static SerializationException UnsignedLongType_ParseValue_IsNotUnsigned(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.UnsignedLongType_IsNotUnsigned_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.UnsignedLong)
                    .Build(),
                type);
        }

        public static SerializationException UnsignedLongType_ParseLiteral_IsNotUnsigned(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.UnsignedLongType_IsNotUnsigned_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.UnsignedLong)
                    .Build(),
                type);
        }

        public static SerializationException UtcOffsetType_ParseValue_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.UtcOffsetType_IsInvalid_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.UtcOffset)
                    .Build(),
                type);
        }

        public static SerializationException UtcOffsetType_ParseLiteral_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.UtcOffsetType_IsInvalid_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.UtcOffset)
                    .Build(),
                type);
        }
    }
}
