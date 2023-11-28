using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

public sealed class InterfaceType : ComplexType, INamedTypeSystemMember<InterfaceType>
{
    public InterfaceType(string name) : base(name)
    {
    }

    public override TypeKind Kind => TypeKind.Interface;

    public override bool Equals(IType? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }
        
        return other is InterfaceType otherInterface && otherInterface.Name.Equals(Name, StringComparison.Ordinal);
    }

    public override string ToString()
        => RewriteInterfaceType(this).ToString(true);

    public static InterfaceType Create(string name) => new(name);
}
