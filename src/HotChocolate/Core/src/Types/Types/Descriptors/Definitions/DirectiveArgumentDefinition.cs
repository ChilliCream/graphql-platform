using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

/// <summary>
/// This definition represents a directive argument.
/// </summary>
public class DirectiveArgumentDefinition : ArgumentDefinition
{
    /// <summary>
    /// Initializes a new instance of <see cref="ArgumentDefinition"/>.
    /// </summary>
    public DirectiveArgumentDefinition() { }

    /// <summary>
    /// Initializes a new instance of <see cref="ArgumentDefinition"/>.
    /// </summary>
    public DirectiveArgumentDefinition(
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

    /// <summary>
    /// The property to which this argument binds to.
    /// </summary>
    public PropertyInfo? Property { get; set; }

    public override Type? GetRuntimeType() => RuntimeType ?? Property?.PropertyType;
}
