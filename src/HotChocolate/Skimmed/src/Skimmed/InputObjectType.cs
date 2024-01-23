using HotChocolate.Utilities;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

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

    public DirectiveCollection Directives { get; } = [];

    public FieldCollection<InputField> Fields { get; } = [];

    public IDictionary<string, object?> ContextData { get; } = new Dictionary<string, object?>();

    public bool Equals(IType? other)
        => Equals(other, TypeComparison.Reference);
    
    public bool Equals(IType? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }
        
        return other is InputObjectType otherInput && otherInput.Name.Equals(Name, StringComparison.Ordinal);
    }

    public override string ToString()
        => RewriteInputObjectType(this).ToString(true);

    public static InputObjectType Create(string name) => new(name);
}
