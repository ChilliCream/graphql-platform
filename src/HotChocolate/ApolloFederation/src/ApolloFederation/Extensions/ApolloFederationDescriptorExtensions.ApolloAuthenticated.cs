using HotChocolate.ApolloFederation.Constants;

namespace HotChocolate.Types;

/// <summary>
/// Provides extensions for applying @authenticated directive on type system descriptors.
/// </summary>
public static partial class ApolloFederationDescriptorExtensions
{
    /// <summary>
    /// Applies @authenticated directive to indicate that the target element is accessible only to the authenticated supergraph users.
    /// <example>
    /// type Foo @key(fields: "id") {
    ///   id: ID
    ///   description: String @authenticated
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The type descriptor on which this directive shall be annotated.
    /// </param>
    /// <returns>
    /// Returns the type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IEnumTypeDescriptor ApolloAuthenticated(this IEnumTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(WellKnownTypeNames.AuthenticatedDirective);
    }

    /// <inheritdoc cref="ApolloAuthenticated(IEnumTypeDescriptor)"/>
    public static IInterfaceFieldDescriptor ApolloAuthenticated(this IInterfaceFieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(WellKnownTypeNames.AuthenticatedDirective);
    }

    /// <inheritdoc cref="ApolloAuthenticated(IEnumTypeDescriptor)"/>
    public static IInterfaceTypeDescriptor ApolloAuthenticated(this IInterfaceTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(WellKnownTypeNames.AuthenticatedDirective);
    }

    /// <inheritdoc cref="ApolloAuthenticated(IEnumTypeDescriptor)"/>
    public static IObjectFieldDescriptor ApolloAuthenticated(this IObjectFieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(WellKnownTypeNames.AuthenticatedDirective);
    }

    /// <inheritdoc cref="ApolloAuthenticated(IEnumTypeDescriptor)"/>
    public static IObjectTypeDescriptor ApolloAuthenticated(this IObjectTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(WellKnownTypeNames.AuthenticatedDirective);
    }
}
