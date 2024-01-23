using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

public sealed class InputField : IField, INamedTypeSystemMember<InputField>
{
    private IType _type;
    private string _name;
    private bool _isDeprecated;
    private string? _deprecationReason;

    public InputField(string name, IType? type = null)
    {
        _name = name.EnsureGraphQLName();
        _type = type ?? NotSetType.Default;
    }

    public string Name
    {
        get => _name;
        set => _name = value.EnsureGraphQLName();
    }

    public string? Description { get; set; }

    public IValueNode? DefaultValue { get; set; }

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

    public IDictionary<string, object?> ContextData => new Dictionary<string, object?>();

    public IType Type
    {
        get => _type;
        set => _type = value.ExpectInputType();
    }

    public override string ToString()
        => RewriteInputField(this).ToString(true);

    public static InputField Create(string name) => new(name);
}
