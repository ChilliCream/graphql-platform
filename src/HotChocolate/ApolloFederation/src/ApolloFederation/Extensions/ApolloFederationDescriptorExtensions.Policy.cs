using HotChocolate.ApolloFederation;

namespace HotChocolate.Types;

/// <summary>
/// Provides extensions for applying @policy directive on type system descriptors.
/// </summary>
public static partial class ApolloFederationDescriptorExtensions
{
    /// <summary>
    /// Indicates to composition that the target element is restricted based on authorization policies
    /// that are evaluated in a Rhai script or coprocessor.
    /// <example>
    /// type Foo {
    ///   description: String @policy(policies: [["policy1Or", "policy2"], ["andPolicy3"]])
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The type descriptor on which this directive shall be annotated.
    /// </param>
    /// <param name="policies">The policy collection</param>
    /// <returns>
    /// Returns the type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IEnumTypeDescriptor Policy(
        this IEnumTypeDescriptor descriptor,
        PolicyCollection policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(policies);
    }

    /// <inheritdoc cref="Policy(IEnumTypeDescriptor, PolicyCollection)"/>
    public static IInterfaceFieldDescriptor Policy(
        this IInterfaceFieldDescriptor descriptor,
        PolicyCollection policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(policies);
    }

    /// <inheritdoc cref="Policy(IEnumTypeDescriptor, PolicyCollection)"/>
    public static IInterfaceTypeDescriptor Policy(
        this IInterfaceTypeDescriptor descriptor,
        PolicyCollection policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(policies);
    }

    /// <inheritdoc cref="Policy(IEnumTypeDescriptor, PolicyCollection)"/>
    public static IObjectFieldDescriptor Policy(
        this IObjectFieldDescriptor descriptor,
        PolicyCollection policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(policies);
    }

    /// <inheritdoc cref="Policy(IEnumTypeDescriptor, PolicyCollection)"/>
    public static IObjectTypeDescriptor Policy(
        this IObjectTypeDescriptor descriptor,
        PolicyCollection policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(policies);
    }
}
