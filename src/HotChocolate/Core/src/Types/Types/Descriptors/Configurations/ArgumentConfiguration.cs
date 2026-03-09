using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors.Configurations;

/// <summary>
/// Defines the properties of a GraphQL argument type.
/// </summary>
public class ArgumentConfiguration : FieldConfiguration
{
    /// <summary>
    /// Initializes a new instance of <see cref="ArgumentConfiguration"/>.
    /// </summary>
    public ArgumentConfiguration() { }

    /// <summary>
    /// Initializes a new instance of <see cref="ArgumentConfiguration"/>.
    /// </summary>
    public ArgumentConfiguration(
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
            return [];
        }

        return _formatters;
    }

    internal void CopyTo(ArgumentConfiguration target)
    {
        base.CopyTo(target);

        target._formatters = _formatters;
        target.DefaultValue = DefaultValue;
        target.RuntimeDefaultValue = RuntimeDefaultValue;
        target.Parameter = Parameter;
        target.RuntimeType = RuntimeType;
    }

    internal void MergeInto(ArgumentConfiguration target)
    {
        base.MergeInto(target);

        if (_formatters is { Count: > 0 })
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
