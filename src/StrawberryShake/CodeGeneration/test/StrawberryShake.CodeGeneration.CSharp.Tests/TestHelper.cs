using StrawberryShake.CodeGeneration;

namespace StrawberryShake
{
    public static class TestHelper
    {
        public static TypeMemberDescriptor GetNamedNonNullStringTypeReference(
            string referenceName) =>
            new(
                referenceName,
                new TypeDescriptor("string", "System"));

        public static TypeMemberDescriptor GetNamedNonNullIntTypeReference(
            string referenceName) =>
            new(
                referenceName,
                new TypeDescriptor("int", "System"));

        public static TypeDescriptor GetNonNullStringTypeReference() =>
            new("string", "System");
    }
}
