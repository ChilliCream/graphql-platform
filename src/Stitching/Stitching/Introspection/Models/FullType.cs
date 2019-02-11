using System.Collections.Generic;

namespace HotChocolate.Stitching.Introspection.Models
{
    internal class FullType
    {
        public TypeKind Kind { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ICollection<Field> Fields { get; set; }
        public ICollection<InputField> InputFields { get; set; }
        public ICollection<TypeRef> Interfaces { get; set; }
        public ICollection<EnumValue> EnumValues { get; set; }
        public ICollection<TypeRef> PossibleTypes { get; set; }
    }
}
