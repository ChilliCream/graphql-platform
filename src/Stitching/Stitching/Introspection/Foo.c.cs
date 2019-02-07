using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Introspection
{
    internal class Schema
    {
        public RootTypeRef QueryType { get; set; }
        public RootTypeRef MutationType { get; set; }
        public RootTypeRef SubscriptionType { get; set; }
        public ICollection<FullType> Types { get; set; }
        public object Directives { get; set; }
    }

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

    internal class Field
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ICollection<InputField> Args { get; set; }
        public TypeRef Type { get; set; }
        public bool IsDepricated { get; set; }
        public string DeprecationReason { get; set; }
    }

    internal class InputField
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public TypeRef Type { get; set; }
        public object DefaultValue { get; set; }
    }


    internal class EnumValue
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsDepricated { get; set; }
        public string DeprecationReason { get; set; }
    }
    internal class TypeRef
    {
        public TypeKind Kind { get; set; }
        public string Name { get; set; }
        public TypeRef OfType { get; set; }
    }

    internal class RootTypeRef
    {
        public string Name { get; set; }
    }
}
