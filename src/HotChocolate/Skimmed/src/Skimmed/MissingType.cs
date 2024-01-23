using HotChocolate.Utilities;

namespace HotChocolate.Skimmed;

public sealed class MissingType : INamedType
{
    private string _name;

    public MissingType(string name)
    {
        _name = name.EnsureGraphQLName();
    }

    public TypeKind Kind => TypeKind.Scalar;

    public string Name
    {
        get => _name;
        set => _name = value.EnsureGraphQLName();
    }

    public string? Description { get; set; }

    public DirectiveCollection Directives { get; } = [];

    public IDictionary<string, object?> ContextData { get; } = new Dictionary<string, object?>();

    public bool Equals(IType? other)
        => Equals(other, TypeComparison.Reference);
    
    public bool Equals(IType? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }
        
        return other is MissingType otherMissing && otherMissing.Name.Equals(Name, StringComparison.Ordinal);
    }
}
