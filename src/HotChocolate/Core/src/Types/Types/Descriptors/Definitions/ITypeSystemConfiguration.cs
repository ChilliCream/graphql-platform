#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

/// <summary>
/// A type system configuration is used in the type initialization
/// as a mutable object to define the types properties.
/// </summary>
public interface ITypeSystemConfiguration
{
    /// <summary>
    /// Gets or sets the name of the type system member.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the type system member.
    /// </summary>
    string? Description { get; set; }

    /// <summary>
    /// Gets or sets a name to which this definition is bound to.
    /// </summary>
    string? BindTo { get; set; }

    /// <summary>
    /// Defines whether descriptor attributes are applied or not.
    /// </summary>
    bool AttributesAreApplied { get; set; }

    /// <summary>
    /// Get access to context data that are copied to the type
    /// and can be used for customizations.
    /// </summary>
    ExtensionData ContextData { get; }

    /// <summary>
    /// Gets access to additional type dependencies.
    /// </summary>
    IList<TypeDependency> Dependencies { get; }

    /// <summary>
    /// Defines if this type has dependencies.
    /// </summary>
    bool HasDependencies { get; }

    /// <summary>
    /// Gets configurations that shall be applied at a later point.
    /// </summary>
    IList<ITypeSystemConfigurationTask> Configurations { get; }

    /// <summary>
    /// Defines if this type has configurations.
    /// </summary>
    bool HasConfigurations { get; }
}
