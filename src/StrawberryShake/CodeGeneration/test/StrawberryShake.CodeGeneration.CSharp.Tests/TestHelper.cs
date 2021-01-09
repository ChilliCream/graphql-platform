using StrawberryShake.CodeGeneration;

namespace StrawberryShake
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

        public static NamedTypeReferenceDescriptor GetNamedNonNullIntTypeReference(string referenceName)
        {
            return new NamedTypeReferenceDescriptor(
                new TypeDescriptor(
                    "int",
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
