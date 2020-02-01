using HotChocolate.Types;

namespace HotChocolate.Stitching.Introspection.Models
{
    internal class TypeRef
    {
        public TypeKind Kind { get; set; }
        public string Name { get; set; }
        public TypeRef OfType { get; set; }
    }
}
