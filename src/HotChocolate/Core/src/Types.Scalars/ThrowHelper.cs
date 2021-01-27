namespace HotChocolate.Types.Scalars
{
    internal static class ThrowHelper
    {
        public static SerializationException NonEmptyStringType_ParseLiteral_IsEmpty(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.NonEmptyStringType_IsEmpty_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", "NonEmptyString")
                    .Build(),
                type);
        }

        public static SerializationException NonEmptyStringType_ParseValue_IsEmpty(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.NonEmptyStringType_IsEmpty_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", "NonEmptyString")
                    .Build(),
                type);
        }

        public static SerializationException NegativeIntType_ParseLiteral_IsNotNegative(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.NegativeIntType_IsNotNegative_ParseLiteral)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .SetExtension("actualType", "NegativeInt")
                    .Build(),
                type);
        }

        public static SerializationException NegativeIntType_ParseValue_IsNotNegative(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.NegativeIntType_IsNotNegative_ParseValue)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .SetExtension("actualType", "NegativeInt")
                    .Build(),
                type);
        }
    }
}
