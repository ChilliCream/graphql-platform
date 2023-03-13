using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Skimmed;

public sealed class InputObjectType : INamedType, INamedTypeSystemMember<InputObjectType>
{
    private string _name;

    public InputObjectType(string name)
    {
        _name = name.EnsureGraphQLName();
    }

    public TypeKind Kind => TypeKind.InputObject;

    public string Name
    {
        get => _name;
        set => _name = value.EnsureGraphQLName();
    }

    public string? Description { get; set; }

    public DirectiveCollection Directives { get; } = new();

    public FieldCollection<InputField> Fields { get; } = new();

    public IDictionary<string, object?> ContextData { get; } = new Dictionary<string, object?>();

    public static InputObjectType Create(string name) => new(name);
}
