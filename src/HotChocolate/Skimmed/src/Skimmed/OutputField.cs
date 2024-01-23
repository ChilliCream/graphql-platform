using HotChocolate.Utilities;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

public sealed class OutputField : IField, INamedTypeSystemMember<OutputField>
{
    private string _name;
    private bool _isDeprecated;
    private string? _deprecationReason;

    public OutputField(string name)
    {
        _name = name.EnsureGraphQLName();
        Type = NotSetType.Default;
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

    public FieldCollection<InputField> Arguments { get; } = [];

    public IType Type { get; set; }

    public IDictionary<string, object?> ContextData { get; } = new Dictionary<string, object?>();

    public override string ToString()
        => RewriteOutputField(this).ToString(true);

    public static OutputField Create(string name) => new(name);
}

