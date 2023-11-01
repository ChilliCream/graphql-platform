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

    public bool Equals(IType? other) => Equals(other, TypeComparison.Reference);

    public abstract bool Equals(IType? other, TypeComparison comparison);
}
