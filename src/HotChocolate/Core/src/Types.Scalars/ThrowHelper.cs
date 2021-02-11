namespace HotChocolate.Types.Scalars
{
    internal static class ThrowHelper
    {
        public static SerializationException EmailAddressType_ParseLiteral_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.EmailAddress_IsInvalid_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.EmailAddress)
                    .Build(),
                type);
        }

        public static SerializationException EmailAddressType_ParseValue_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.EmailAddress_IsInvalid_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.EmailAddress)
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

        public static SerializationException NonPositiveFloatType_ParseLiteral_IsNotNonPositive(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.NonPositiveFloatType_IsNotNonPositive_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.NonPositiveFloat)
                    .Build(),
                type);
        }

        public static SerializationException NonPositiveFloatType_ParseValue_IsNotNonPositive(IType type)
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
                    .SetMessage(ScalarResources.PhoneNumber_IsInvalid_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", WellKnownScalarTypes.PhoneNumber)
                    .Build(),
                type);
        }

        public static SerializationException PhoneNumber_ParseValue_IsInvalid(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.PhoneNumber_IsInvalid_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", WellKnownScalarTypes.PhoneNumber)
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
    }
}
