using HotChocolate.Language;

namespace SkinnyLatte.Types;

/// <summary>
/// Represents a field or argument of input- or output-types.
/// </summary>
public abstract class Field
    : TypeSystemMember
    , IDirectiveProvider
{
    protected Field(
        string name,
        string? deprecationReason,
        IReadOnlyDictionary<string, object?> contextData,
        object directives)
        : base(name, contextData)
    {
        IsDeprecated = !string.IsNullOrEmpty(DeprecationReason);
        DeprecationReason = deprecationReason;
        Directives = directives;
    }

    /// <summary>
    /// Gets the type of this field.
    /// </summary>
    public abstract IType Type { get; }

    /// <summary>
    /// Gets the type of which declares this field.
    /// </summary>
    public abstract NamedType DeclaringType { get; }

    /// <summary>
    /// Defines if this field is deprecated.
    /// </summary>
    public bool IsDeprecated { get; }

    /// <summary>
    /// Gets the deprecation reason.
    /// </summary>
    public string? DeprecationReason { get; }

    public object Directives { get; }
}

public abstract class OutputField
{
    /// <summary>
    /// Gets the return type of this field.
    /// </summary>
    IOutputType Type { get; }

    /// <summary>
    /// Gets the field arguments.
    /// </summary>
    IFieldCollection<IInputField> Arguments { get; }

    /// <summary>
    /// Defines if this field is a introspection field.
    /// </summary>
    bool IsIntrospectionField { get; }

    /// <summary>
    /// Defines if this field is deprecated.
    /// </summary>
    bool IsDeprecated { get; }

    /// <summary>
    /// Gets the deprecation reason.
    /// </summary>
    string? DeprecationReason { get; }

    /// <summary>
    /// Gets the type that declares this field.
    /// </summary>
    new IComplexOutputType DeclaringType { get; }
}
