#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

/// <summary>
/// This definition represents a field or argument.
/// </summary>
public abstract class FieldConfiguration
    : TypeSystemConfiguration
    , IDirectiveConfigurationProvider
    , IIgnoreConfiguration
{
    private List<DirectiveConfiguration>? _directives;
    private string? _deprecationReason;
    private FieldFlags _flags = FieldFlags.None;

    /// <summary>
    /// Gets the internal field flags from this field.
    /// </summary>
    internal FieldFlags Flags
    {
        get => _flags;
        set => _flags = value;
    }

    /// <summary>
    /// Describes why this syntax node is deprecated.
    /// </summary>
    public string? DeprecationReason
    {
        get => _deprecationReason;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                Flags &= ~FieldFlags.Deprecated;
            }
            else
            {
                Flags |= FieldFlags.Deprecated;
            }

            _deprecationReason = value;
        }
    }

    /// <summary>
    /// If true, the field is deprecated
    /// </summary>
    public bool IsDeprecated => (Flags & FieldFlags.Deprecated) == FieldFlags.Deprecated;

    /// <summary>
    /// Gets the field type.
    /// </summary>
    public TypeReference? Type { get; set; }

    /// <summary>
    /// Defines if this field is ignored and will
    /// not be included into the schema.
    /// </summary>
    public bool Ignore
    {
        get => (Flags & FieldFlags.Ignored) == FieldFlags.Ignored;
        set
        {
            if (value)
            {
                Flags |= FieldFlags.Ignored;
            }
            else
            {
                Flags &= ~FieldFlags.Ignored;
            }
        }
    }

    /// <summary>
    /// Gets the list of directives that are annotated to this field.
    /// </summary>
    public IList<DirectiveConfiguration> Directives
        => _directives ??= [];

    /// <summary>
    /// Specifies if this field has any directives.
    /// </summary>
    public bool HasDirectives => _directives?.Count > 0;

    /// <summary>
    /// Gets the list of directives that are annotated to this field.
    /// </summary>
    public IReadOnlyList<DirectiveConfiguration> GetDirectives()
    {
        if (_directives is null)
        {
            return Array.Empty<DirectiveConfiguration>();
        }

        return _directives;
    }

    public void SetSourceGeneratorFlags() => Flags |= FieldFlags.SourceGenerator;

    public void SetConnectionFlags() => Flags |= FieldFlags.Connection;

    public void SetConnectionEdgesFieldFlags() => Flags |= FieldFlags.ConnectionEdgesField;

    public void SetConnectionNodesFieldFlags() => Flags |= FieldFlags.ConnectionNodesField;

    public void SetConnectionTotalCountFieldFlags() => Flags |= FieldFlags.TotalCount;

    protected void CopyTo(FieldConfiguration target)
    {
        base.CopyTo(target);

        if (_directives is { Count: > 0 })
        {
            target._directives = [.._directives];
        }

        target.Type = Type;
        target.Flags = Flags;

        if (IsDeprecated)
        {
            target.DeprecationReason = DeprecationReason;
        }
    }

    protected void MergeInto(FieldConfiguration target)
    {
        base.MergeInto(target);

        if (_directives is { Count: > 0 })
        {
            target._directives ??= [];
            target._directives.AddRange(_directives);
        }

        if (Type is not null)
        {
            target.Type = Type;
        }

        target.Flags = Flags | target.Flags;

        if (IsDeprecated)
        {
            target.DeprecationReason = DeprecationReason;
        }
    }
}
