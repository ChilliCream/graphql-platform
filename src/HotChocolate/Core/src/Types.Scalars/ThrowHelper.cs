namespace HotChocolate.Types.Scalars
{
    internal static class ThrowHelper
    {
        public static SerializationException NonEmptyStringType_ParseLiteral_IsEmpty(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.NonEmptyStringType_IsEmpty_ParseLiteral)
                    .SetExtension("actualType", "NonEmptyString")
                    .Build(),
                type);
        }

        public static SerializationException NonEmptyStringType_ParseValue_IsEmpty(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.NonEmptyStringType_IsEmpty_ParseValue)
                    .SetExtension("actualType", "NonEmptyString")
                    .Build(),
                type);
        }
    }
}
