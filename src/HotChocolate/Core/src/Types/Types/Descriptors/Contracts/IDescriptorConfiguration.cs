using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

/// <summary>
/// Represents a configuration that can be applied to a GraphQL type system descriptor.
/// Descriptor configurations can be implemented as attributes or standalone classes
/// to provide reusable type system configuration logic.
/// </summary>
public interface IDescriptorConfiguration
{
    /// <summary>
    /// Gets the order in which this configuration should be applied.
    /// Configurations with lower order values are applied first.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Requires the attribute provide this configuration was applied to for reflection.
    /// </summary>
    bool RequiresAttributeProvider { get; }

    /// <summary>
    /// Attempts to configure the specified descriptor based on the provided element.
    /// </summary>
    /// <param name="context">
    /// The descriptor context providing access to schema-building services and conventions.
    /// </param>
    /// <param name="descriptor">
    /// The descriptor to configure. This could be a type descriptor, field descriptor,
    /// or any other GraphQL type system descriptor.
    /// </param>
    /// <param name="attributeProvider">
    /// The element (type, method, property, etc.) to which this configuration is being applied.
    /// This provides reflection metadata about the target element.
    /// </param>
    void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider? attributeProvider);
}
