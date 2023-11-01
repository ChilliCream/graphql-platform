using HotChocolate.Utilities;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a field of an <see cref="ObjectType"/> or <see cref="InterfaceType"/>.
/// </summary>
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
    
    public OutputField(string name, IType type)
    {
        _name = name.EnsureGraphQLName();
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <summary>
    /// Gets or sets the name of the field.
    /// </summary>
    public string Name
    {
        get => _name;
        set => _name = value.EnsureGraphQLName();
    }
    
    /// <summary>
    /// Gets or sets the description of the field.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the deprecation status of the field.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the deprecation reason of the field.
    /// </summary>
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

    /// <summary>
    /// Gets the directives that are annotated to this field.
    /// </summary>
    public DirectiveCollection Directives { get; } = new();

    /// <summary>
    /// Gets the arguments of this field.
    /// </summary>
    public FieldCollection<InputField> Arguments { get; } = new();

    /// <summary>
    /// Gets the return type of this field.
    /// </summary>
    public IType Type { get; set; }

    public IDictionary<string, object?> ContextData { get; } = new ContextDataMap();

    public override string ToString()
        => RewriteOutputField(this).ToString(true);

    public static OutputField Create(string name) => new(name);
}