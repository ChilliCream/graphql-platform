using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// A type system descriptor provides a fluent builder API to configure a type system member.
/// The output of a descriptor is a definition which represents the configuration for a
/// type system member.
/// </summary>
/// <typeparam name="T">
/// The type definition.
/// </typeparam>
public interface IDescriptor<out T> : IDescriptor where T : DefinitionBase
{
    /// <summary>
    /// Provides access to the underlying configuration. This is useful for extensions.
    /// </summary>
    /// <returns></returns>
    IDescriptorExtension<T> Extend();

    /// <summary>
    /// Provides access to the underlying configuration. This is useful for extensions.
    /// </summary>
    /// <returns></returns>
    IDescriptorExtension<T> ExtendWith(Action<IDescriptorExtension<T>> configure);
}
