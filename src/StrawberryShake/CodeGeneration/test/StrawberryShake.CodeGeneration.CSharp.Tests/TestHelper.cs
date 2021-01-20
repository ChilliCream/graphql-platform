using StrawberryShake.CodeGeneration;

namespace StrawberryShake
{
    public static class TestHelper
    {
        public static PropertyDescriptor GetNamedNonNullStringTypeReference(
            string referenceName) =>
            new(
                referenceName,
                new NonNullTypeDescriptor(new NamedTypeDescriptor("string", "System")));

        public static PropertyDescriptor GetNamedNonNullIntTypeReference(
            string referenceName) =>
            new(
                referenceName,
                new NonNullTypeDescriptor(new NamedTypeDescriptor("int", "System")));
    }
}
