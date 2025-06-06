#nullable enable

namespace HotChocolate.Types.Descriptors.Configurations;

/// <summary>
/// A configuration object that is applied to a type system member at a certain event
/// during the type system initialization.
/// </summary>
public interface ITypeSystemConfigurationTask
{
    /// <summary>
    /// The definition of the type system member that shall be configured.
    /// </summary>
    /// <value></value>
    ITypeSystemConfiguration Owner { get; }

    /// <summary>
    /// Defines on which type initialization step this
    /// configurations is applied on.
    /// </summary>
    ApplyConfigurationOn On { get; }

    /// <summary>
    /// Defines types on on which this configuration is dependant on.
    /// </summary>
    IReadOnlyList<TypeDependency> Dependencies { get; }

    /// <summary>
    /// Adds an additional type dependency.
    /// </summary>
    /// <param name="dependency">
    /// The type dependency.
    /// </param>
    void AddDependency(TypeDependency dependency);

    /// <summary>
    /// Creates a copy of this object with the new <paramref name="newOwner"/>.
    /// </summary>
    /// <param name="newOwner">
    /// The new owner of this configuration.
    /// </param>
    /// <returns>
    /// Returns the new configuration.
    /// </returns>
    ITypeSystemConfigurationTask Copy(TypeSystemConfiguration newOwner);
}
