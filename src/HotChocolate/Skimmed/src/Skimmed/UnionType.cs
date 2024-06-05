using HotChocolate.Utilities;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

public sealed class UnionType : INamedType, INamedTypeSystemMember<UnionType>
{
    private string _name;

    public UnionType(string name)
    {
        _name = name.EnsureGraphQLName();
    }

    public TypeKind Kind => TypeKind.Union;

    public string Name
    {
        get => _name;
        set => _name = value.EnsureGraphQLName();
    }

    public string? Description { get; set; }

    public DirectiveCollection Directives { get; } = [];

    public IList<ObjectType> Types { get; } = new List<ObjectType>();

    public IDictionary<string, object?> ContextData { get; } = new Dictionary<string, object?>();
    
    public override string ToString()
        => RewriteUnionType(this).ToString(true);
    
    public bool Equals(IType? other)
        => Equals(other, TypeComparison.Reference);
    
    public bool Equals(IType? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }
        
        return other is UnionType otherUnion && otherUnion.Name.Equals(Name, StringComparison.Ordinal);
    }

    public static UnionType Create(string name) => new(name);
}
