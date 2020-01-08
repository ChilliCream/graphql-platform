#pragma warning disable CA1812
#nullable disable

namespace HotChocolate.Utilities.Introspection
{
    internal class TypeRef
    {
        public TypeKind Kind { get; set; }
        public string Name { get; set; }
        public TypeRef OfType { get; set; }
    }
}
#pragma warning restore CA1812
