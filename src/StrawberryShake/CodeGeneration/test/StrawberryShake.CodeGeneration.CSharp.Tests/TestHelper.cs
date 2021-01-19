using StrawberryShake.CodeGeneration;

namespace StrawberryShake
{
    public static class TestHelper
    {
        public static PropertyDescriptor GetNamedNonNullStringTypeReference(
            string referenceName) =>
            new(
                referenceName,
                new NamedTypeDescriptor("string", "System"));

        public static PropertyDescriptor GetNamedNonNullIntTypeReference(
            string referenceName) =>
            new(
                referenceName,
                new NamedTypeDescriptor("int", "System"));

        public static NamedTypeDescriptor GetNonNullStringTypeReference() =>
            new("string", "System");
    }
}
