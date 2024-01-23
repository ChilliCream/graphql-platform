using HotChocolate.Utilities;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

public sealed class EnumValue
    : IHasName
    , IHasDirectives
    , IHasContextData
    , INamedTypeSystemMember<EnumValue>
    , IHasDescription
    , ICanBeDeprecated
{
    private string _name;
    private bool _isDeprecated;
    private string? _deprecationReason;

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

    public bool IsDeprecated
    {
        get => _isDeprecated;
        set
        {
            _isDeprecated = value;

            if (!value)
            {
                DeprecationReason = null;
            }
        }
    }

    public string? DeprecationReason
    {
        get => _deprecationReason;
        set
        {
            _deprecationReason = value;

            if (!string.IsNullOrEmpty(value))
            {
                _isDeprecated = true;
            }
        }
    }

    public DirectiveCollection Directives { get; } = [];

    public IDictionary<string, object?> ContextData { get; } = new Dictionary<string, object?>();

    public override string ToString()
        => RewriteEnumValue(this).ToString(true);

    public static EnumValue Create(string name) => new(name);
}
