using StrawberryShake.CodeGeneration;

namespace StrawberryShake
{
    public static class TestHelper
    {
        public static NamedTypeReferenceDescriptor GetNamedNonNullStringTypeReference(string referenceName)
        {
            return new NamedTypeReferenceDescriptor(
                referenceName,
                new TypeDescriptor(
                    "string",
                    "System"
                )
            );
        }

        public static NamedTypeReferenceDescriptor GetNamedNonNullIntTypeReference(string referenceName)
        {
            return new NamedTypeReferenceDescriptor(
                referenceName,
                new TypeDescriptor(
                    "int",
                    "System"
                )
            );
        }

        public static TypeDescriptor GetNonNullStringTypeReference()
        {
            return new TypeDescriptor(
                "string",
                "System"
            );
        }
    }
}
