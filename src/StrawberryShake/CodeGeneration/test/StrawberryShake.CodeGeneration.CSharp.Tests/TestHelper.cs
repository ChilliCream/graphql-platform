namespace StrawberryShake.CodeGeneration.CSharp.Tests
{
    public static class TestHelper
    {
        public static NamedTypeReferenceDescriptor GetNamedNonNullStringTypeReference(string referenceName)
        {
            return new NamedTypeReferenceDescriptor(
                new TypeDescriptor(
                    "string",
                    "System"
                ),
                false,
                ListType.NoList,
                referenceName
            );
        }

        public static TypeReferenceDescriptor GetNonNullStringTypeReference()
        {
            return new TypeReferenceDescriptor(
                new TypeDescriptor(
                    "string",
                    "System"
                ),
                false,
                ListType.NoList
            );
        }
    }
}
