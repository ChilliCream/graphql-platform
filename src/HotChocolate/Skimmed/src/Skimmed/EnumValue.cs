using HotChocolate.Utilities;

namespace HotChocolate.Skimmed;

public sealed class EnumValue
{
    private string _name;

    public EnumValue(string name)
    {
        _name = name.EnsureGraphQLName();
    }

    public string Name
    {
        get => _name;
        set => _name = value.EnsureGraphQLName();
    }

    public string? Description { get; set; }

    public IDictionary<string, object?> ContextData { get; } = new Dictionary<string, object?>();
}
