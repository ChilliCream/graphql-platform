#nullable  enable

namespace HotChocolate.Types.Descriptors.Configurations;

/// <summary>
/// A definition that represents a type.
/// </summary>
public abstract class TypeConfiguration : TypeSystemConfiguration, ITypeConfiguration
{
    private List<DirectiveConfiguration>? _directives;
    private Type _runtimeType = typeof(object);

    protected TypeConfiguration() { }

    protected TypeConfiguration(Type runtimeType)
    {
        _runtimeType = runtimeType;
    }

    /// <summary>
    /// Specifies that this type system object needs an explicit name completion since it
    /// depends on another type system object to complete its name.
    /// </summary>
    public bool NeedsNameCompletion { get; set; }

    /// <summary>
    /// Gets or sets the .NET type representation of this type.
    /// </summary>
    public virtual Type RuntimeType
    {
        get => _runtimeType;
        set => _runtimeType = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// If this is a type definition extension this is the type we want to extend.
    /// </summary>
    public Type? ExtendsType { get; set; }

    /// <summary>
    /// Gets the list of directives that are annotated to this type.
    /// </summary>
    public IList<DirectiveConfiguration> Directives =>
        _directives ??= [];

    /// <summary>
    /// Specifies if this definition has directives.
    /// </summary>
    public bool HasDirectives => _directives is { Count: > 0 };

    /// <summary>
    /// Gets the list of directives that are annotated to this field.
    /// </summary>
    public IReadOnlyList<DirectiveConfiguration> GetDirectives()
    {
        if (_directives is null)
        {
            return [];
        }

        return _directives;
    }

    protected void CopyTo(TypeConfiguration target)
    {
        base.CopyTo(target);

        target._runtimeType = _runtimeType;
        target.ExtendsType = ExtendsType;

        if (_directives is { Count: > 0 })
        {
            target._directives = [.. _directives];
        }
    }

    protected void MergeInto(TypeConfiguration target)
    {
        base.MergeInto(target);

        // Note: we will not change ExtendsType or _runtimeType on merge.

        if (_directives is { Count: > 0 })
        {
            target._directives ??= [];
            target._directives.AddRange(Directives);
        }
    }
}
