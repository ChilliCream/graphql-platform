using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

public sealed class ObjectType(string name) : ComplexType(name), INamedTypeSystemMember<ObjectType>
{
    public override TypeKind Kind => TypeKind.Object;
    
    public override string ToString()
        => RewriteObjectType(this).ToString(true);
    
    public override bool Equals(IType? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }
        
        return other is ObjectType otherObject && otherObject.Name.Equals(Name, StringComparison.Ordinal);
    }

    public static ObjectType Create(string name) => new(name);
}
