using System.Collections.Generic;
using HotChocolate.ApolloFederation;

namespace HotChocolate.Types;

/// <summary>
/// Provides extensions for applying @requiresScopes directive on type system descriptors.
/// </summary>
public static partial class ApolloFederationDescriptorExtensions
{
    /// <summary>
    /// Applies @requiresScopes directive to indicate that the target element is accessible only to the authenticated supergraph users with the appropriate JWT scopes.
    /// <example>
    /// type Foo @key(fields: "id") {
    ///   id: ID
    ///   description: String @requiresScopes(scopes: [["scope1"]])
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The type descriptor on which this directive shall be annotated.
    /// </param>
    /// <param name="scopes">Required JWT scopes</param>
    /// <returns>
    /// Returns the type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IEnumTypeDescriptor RequiresScopes(
        this IEnumTypeDescriptor descriptor,
        List<List<Scope>> scopes)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(new RequiresScopes(scopes));
    }

    /// <inheritdoc cref="RequiresScopes(IEnumTypeDescriptor, List{List{Scope}})"/>
    public static IInterfaceFieldDescriptor RequiresScopes(
        this IInterfaceFieldDescriptor descriptor,
        List<List<Scope>> scopes)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(new RequiresScopes(scopes));
    }

    /// <inheritdoc cref="RequiresScopes(IEnumTypeDescriptor, List{List{Scope}})"/>
    public static IInterfaceTypeDescriptor RequiresScopes(
        this IInterfaceTypeDescriptor descriptor,
        List<List<Scope>> scopes)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(new RequiresScopes(scopes));
    }

    /// <inheritdoc cref="RequiresScopes(IEnumTypeDescriptor, List{List{Scope}})"/>
    public static IObjectFieldDescriptor RequiresScopes(
        this IObjectFieldDescriptor descriptor,
        List<List<Scope>> scopes)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(new RequiresScopes(scopes));
    }

    /// <inheritdoc cref="RequiresScopes(IEnumTypeDescriptor, List{List{Scope}})"/>
    public static IObjectTypeDescriptor RequiresScopes(
        this IObjectTypeDescriptor descriptor,
        List<List<Scope>> scopes)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(new RequiresScopes(scopes));
    }
}
