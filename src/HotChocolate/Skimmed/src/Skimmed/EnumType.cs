using HotChocolate.Utilities;

namespace HotChocolate.Skimmed;

public sealed class EnumType : INamedType, INamedTypeSystemMember<EnumType>
{
    private string _name;

    public EnumType(string name)
    {
        _name = name.EnsureGraphQLName();
    }

    public TypeKind Kind => TypeKind.Enum;

    public string Name
    {
        get => _name;
        set => _name = value.EnsureGraphQLName();
    }

    public string? Description { get; set; }

    public DirectiveCollection Directives { get; } = new();

    public EnumValueCollection Values { get; } = new();

    public IDictionary<string, object?> ContextData { get; } = new Dictionary<string, object?>();

    public static EnumType Create(string name) => new(name);
}
