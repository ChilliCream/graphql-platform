#nullable enable

using HotChocolate.Features;

namespace HotChocolate.Types.Descriptors.Configurations;

/// <summary>
/// A type system configuration is used in the type initialization
/// as a mutable object to define the types properties.
/// </summary>
public interface ITypeSystemConfiguration : IFeatureProvider
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
    IList<ITypeSystemConfigurationTask> Tasks { get; }

    /// <summary>
    /// Defines if this type has configurations.
    /// </summary>
    bool HasTasks { get; }
}
