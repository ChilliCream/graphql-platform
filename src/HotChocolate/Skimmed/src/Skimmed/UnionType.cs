using HotChocolate.Utilities;

namespace HotChocolate.Skimmed;

public sealed class UnionType : INamedType
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

    public DirectiveCollection Directives { get; } = new();

    public IList<ObjectType> Types { get; } = new List<ObjectType>();

    public IDictionary<string, object?> ContextData { get; } = new Dictionary<string, object?>();
}
