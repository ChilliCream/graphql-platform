using HotChocolate.Utilities;

namespace HotChocolate.Skimmed;

public abstract class ComplexType(string name) : INamedType
{
    private string _name = name.EnsureGraphQLName();

    public abstract TypeKind Kind { get; }

    public string Name
    {
        get => _name;
        set => _name = value.EnsureGraphQLName();
    }

    public string? Description { get; set; }

    public DirectiveCollection Directives { get; } = new();

    public IList<InterfaceType> Implements { get; } = new List<InterfaceType>();

    public FieldCollection<OutputField> Fields { get; } = new();

    public IDictionary<string, object?> ContextData { get; } = new ContextDataMap();

    public bool IsAssignableFrom(INamedType type, TypeComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (ReferenceEquals(type, this))
        {
            return true;
        }
        
        if (comparison is TypeComparison.Reference)
        {
            return Implements.Contains(type);
        }
        
        if (comparison is TypeComparison.Structural)
        {
            if (type.Kind.Equals(Kind) && type.Name.EqualsOrdinal(Name))
            {
                return true;
            }
            
            return Implements.Any(t => t.Name.EqualsOrdinal(type.Name));
        }

        return false;
    }

    public bool Equals(IType? other) => Equals(other, TypeComparison.Reference);

    public abstract bool Equals(IType? other, TypeComparison comparison);
}
