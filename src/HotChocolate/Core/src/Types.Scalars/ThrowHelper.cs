namespace HotChocolate.Types.Scalars
{
    internal static class ThrowHelper
    {
        public static SerializationException NonNullStringType_ParseLiteral_IsEmpty(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.NonNullStringType_IsEmpty_ParseLiteral)
                    .SetExtension("actualType", "NonEmptyString")
                    .Build(),
                type);
        }

        public static SerializationException NonNullStringType_ParseValue_IsEmpty(IType type)
        {
            return new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ScalarResources.NonNullStringType_IsEmpty_ParseValue)
                    .SetExtension("actualType", "NonEmptyString")
                    .Build(),
                type);
        }
    }
}
