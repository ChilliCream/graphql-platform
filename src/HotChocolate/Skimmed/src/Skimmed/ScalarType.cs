using HotChocolate.Utilities;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

public sealed class ScalarType : INamedType, INamedTypeSystemMember<ScalarType>
{
    private string _name;

    public ScalarType(string name)
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

    public bool IsSpecScalar { get; set; }

    public DirectiveCollection Directives { get; } = new();

    public IDictionary<string, object?> ContextData { get; } = new Dictionary<string, object?>();

    public override string ToString()
        => RewriteScalarType(this).ToString(true);

    public static ScalarType Create(string name) => new(name);
}
