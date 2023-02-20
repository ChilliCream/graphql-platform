using HotChocolate.Utilities;

namespace HotChocolate.Skimmed;

public sealed class NotSetType : IType
{
    private NotSetType()
    {
    }

    public TypeKind Kind => TypeKind.Scalar;

    public static readonly NotSetType Default = new();
}

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

    public DirectiveCollection Directives { get; } = new();

    public IDictionary<string, object?> ContextData { get; } = new Dictionary<string, object?>();
}
