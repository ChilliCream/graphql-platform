using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

/// <summary>
/// Defines the properties of a GraphQL argument type.
/// </summary>
public class ArgumentDefinition : FieldDefinitionBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="ArgumentDefinition"/>.
    /// </summary>
    public ArgumentDefinition() { }

    /// <summary>
    /// Initializes a new instance of <see cref="ArgumentDefinition"/>.
    /// </summary>
    public ArgumentDefinition(
        string name,
        string? description = null,
        TypeReference? type = null,
        IValueNode? defaultValue = null,
        object? runtimeDefaultValue = null)
    {
        Name = name.EnsureGraphQLName();
        Description = description;
        Type = type;
        DefaultValue = defaultValue;
        RuntimeDefaultValue = runtimeDefaultValue;
    }

    private List<IInputValueFormatter>? _formatters;

    public IValueNode? DefaultValue { get; set; }

    public object? RuntimeDefaultValue { get; set; }

    public ParameterInfo? Parameter { get; set; }

    public Type? RuntimeType { get; set; }

    public IList<IInputValueFormatter> Formatters =>
        _formatters ??= [];

    public virtual Type? GetRuntimeType() => RuntimeType ?? Parameter?.ParameterType;

    public IReadOnlyList<IInputValueFormatter> GetFormatters()
    {
        if (_formatters is null)
        {
            return Array.Empty<IInputValueFormatter>();
        }

        return _formatters;
    }

    internal void CopyTo(ArgumentDefinition target)
    {
        base.CopyTo(target);

        target._formatters = _formatters;
        target.DefaultValue = DefaultValue;
        target.RuntimeDefaultValue = RuntimeDefaultValue;
        target.Parameter = Parameter;
        target.RuntimeType = RuntimeType;
    }

    internal void MergeInto(ArgumentDefinition target)
    {
        base.MergeInto(target);

        if (_formatters is { Count: > 0, })
        {
            target._formatters ??= [];
            target._formatters.AddRange(_formatters);
        }

        if (DefaultValue is not null)
        {
            target.DefaultValue = DefaultValue;
        }

        if (RuntimeDefaultValue is not null)
        {
            target.RuntimeDefaultValue = RuntimeDefaultValue;
        }

        if (Parameter is not null)
        {
            target.Parameter = Parameter;
        }

        if (RuntimeType is not null)
        {
            target.RuntimeType = RuntimeType;
        }
    }
}
