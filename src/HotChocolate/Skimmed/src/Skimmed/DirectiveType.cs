using HotChocolate.Utilities;

namespace HotChocolate.Skimmed;

public sealed class DirectiveType : ITypeSystemMember
{
    private string _name;

    public DirectiveType(string name)
    {
        _name = name.EnsureGraphQLName();
    }

    public string Name
    {
        get => _name.EnsureGraphQLName();
        set => _name = value;
    }

    public string? Description { get; set; }

    /// <summary>
    /// Defines that this directive is repeatable and can be applied multiple times.
    /// </summary>
    public bool IsRepeatable { get; set; }

    public FieldCollection<InputField> Arguments { get; } = new();

    public DirectiveLocation Locations { get; set; }

    public IDictionary<string, object?> ContextData { get; } = new Dictionary<string, object?>();
}
