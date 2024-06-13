using HotChocolate.Utilities;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

public sealed class EnumType : INamedTypeDefinition, INamedTypeSystemMemberDefinition<EnumType>
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

    public DirectiveCollection Directives { get; } = [];

    public EnumValueCollection Values { get; } = [];

    public IDictionary<string, object?> ContextData { get; } =
        new Dictionary<string, object?>();

    public override string ToString()
        => RewriteEnumType(this).ToString(true);

    public bool Equals(ITypeDefinition? other) => Equals(other, TypeComparison.Reference);

    public bool Equals(ITypeDefinition? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is EnumType otherEnum && otherEnum.Name.Equals(Name, StringComparison.Ordinal);
    }

    public static EnumType Create(string name) => new(name);
}
