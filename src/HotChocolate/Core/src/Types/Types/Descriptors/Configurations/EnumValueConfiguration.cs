using System.Reflection;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors.Configurations;

/// <summary>
/// Defines the properties of a GraphQL enum value.
/// </summary>
public class EnumValueConfiguration
    : TypeConfiguration
    , IDeprecationConfiguration
    , IIgnoreConfiguration
{
    /// <summary>
    /// Initializes a new instance of <see cref="EnumValueConfiguration"/>.
    /// </summary>
    public EnumValueConfiguration() { }

    /// <summary>
    /// Initializes a new instance of <see cref="EnumValueConfiguration"/>.
    /// </summary>
    public EnumValueConfiguration(
        string name,
        string? description = null,
        object? runtimeValue = null)
    {
        Name = name.EnsureGraphQLName();
        Description = description;
        RuntimeValue = runtimeValue;
    }

    /// <summary>
    /// Gets the reason why this value was deprecated.
    /// </summary>
    public string? DeprecationReason { get; set; }

    /// <summary>
    /// Defines if this enum value is deprecated.
    /// </summary>
    public bool IsDeprecated => !string.IsNullOrEmpty(DeprecationReason);

    /// <summary>
    /// Defines if this enum value is ignored
    /// and therefore excluded from the schema.
    /// </summary>
    public bool Ignore { get; set; }

    /// <summary>
    /// Gets the runtime value.
    /// </summary>
    public object? RuntimeValue { get; set; }

    /// <summary>
    /// Gets or sets the enum value member.
    /// </summary>
    public MemberInfo? Member { get; set; }
}
