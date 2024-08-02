#pragma warning disable CA1812
#nullable disable

namespace HotChocolate.Utilities.Introspection;

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
#pragma warning restore CA1812
