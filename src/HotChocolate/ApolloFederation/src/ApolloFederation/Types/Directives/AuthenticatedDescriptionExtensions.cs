namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// Provides extensions for applying @authenticated directive on type system descriptors.
/// </summary>
public static class AuthenticatedDescriptionExtensions
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
    public static IEnumTypeDescriptor Authenticated(this IEnumTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(AuthenticatedDirective.Default);
    }

    /// <inheritdoc cref="Authenticated(HotChocolate.Types.IEnumTypeDescriptor)"/>
    public static IInterfaceFieldDescriptor Authenticated(this IInterfaceFieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(AuthenticatedDirective.Default);
    }

    /// <inheritdoc cref="Authenticated(HotChocolate.Types.IEnumTypeDescriptor)"/>
    public static IInterfaceTypeDescriptor Authenticated(this IInterfaceTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(AuthenticatedDirective.Default);
    }

    /// <inheritdoc cref="Authenticated(HotChocolate.Types.IEnumTypeDescriptor)"/>
    public static IObjectFieldDescriptor Authenticated(this IObjectFieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(AuthenticatedDirective.Default);
    }

    /// <inheritdoc cref="Authenticated(HotChocolate.Types.IEnumTypeDescriptor)"/>
    public static IObjectTypeDescriptor Authenticated(this IObjectTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(AuthenticatedDirective.Default);
    }
}
