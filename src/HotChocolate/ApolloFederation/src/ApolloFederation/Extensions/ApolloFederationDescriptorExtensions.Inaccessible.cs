using HotChocolate.ApolloFederation.Constants;

namespace HotChocolate.Types;

/// <summary>
/// Provides extensions for applying @inaccessible directive on type system descriptors.
/// </summary>
public static partial class ApolloFederationDescriptorExtensions
{
    /// <summary>
    /// Applies the @inaccessible directive which is used to mark location within schema as inaccessible
    /// from the GraphQL Router. While @inaccessible fields are not exposed by the router to the clients,
    /// they are still available for query plans and can be referenced from @key and @requires directives.
    /// This allows you to not expose sensitive fields to your clients but still make them available for
    /// computations. Inaccessible can also be used to incrementally add schema elements (e.g. fields) to
    /// multiple subgraphs without breaking composition.
    /// <example>
    /// type Foo @key(fields: "id") {
    ///   id: ID!
    ///   hidden: String @inaccessible
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
    public static IEnumTypeDescriptor Inaccessible(
        this IEnumTypeDescriptor descriptor)
    {
        ValidateDescriptor(descriptor);
        return descriptor.Directive(WellKnownTypeNames.Inaccessible);
    }

    /// <inheritdoc cref="Inaccessible(IEnumTypeDescriptor)"/>
    public static IEnumValueDescriptor Inaccessible(
        this IEnumValueDescriptor descriptor)
    {
        ValidateDescriptor(descriptor);
        return descriptor.Directive(WellKnownTypeNames.Inaccessible);
    }

    /// <inheritdoc cref="Inaccessible(IEnumTypeDescriptor)"/>
    public static IInterfaceFieldDescriptor Inaccessible(
        this IInterfaceFieldDescriptor descriptor)
    {
        ValidateDescriptor(descriptor);
        return descriptor.Directive(WellKnownTypeNames.Inaccessible);
    }

    /// <inheritdoc cref="Inaccessible(IEnumTypeDescriptor)"/>
    public static IInterfaceTypeDescriptor Inaccessible(
        this IInterfaceTypeDescriptor descriptor)
    {
        ValidateDescriptor(descriptor);
        return descriptor.Directive(WellKnownTypeNames.Inaccessible);
    }

    /// <inheritdoc cref="Inaccessible(IEnumTypeDescriptor)"/>
    public static IInputObjectTypeDescriptor Inaccessible(
        this IInputObjectTypeDescriptor descriptor)
    {
        ValidateDescriptor(descriptor);
        return descriptor.Directive(WellKnownTypeNames.Inaccessible);
    }

    /// <inheritdoc cref="Inaccessible(IEnumTypeDescriptor)"/>
    public static IInputFieldDescriptor Inaccessible(
        this IInputFieldDescriptor descriptor)
    {
        ValidateDescriptor(descriptor);
        return descriptor.Directive(WellKnownTypeNames.Inaccessible);
    }

    /// <inheritdoc cref="Inaccessible(IEnumTypeDescriptor)"/>
    public static IObjectFieldDescriptor Inaccessible(
        this IObjectFieldDescriptor descriptor)
    {
        ValidateDescriptor(descriptor);
        return descriptor.Directive(WellKnownTypeNames.Inaccessible);
    }

    /// <inheritdoc cref="Inaccessible(IEnumTypeDescriptor)"/>
    public static IObjectTypeDescriptor Inaccessible(
        this IObjectTypeDescriptor descriptor)
    {
        ValidateDescriptor(descriptor);
        return descriptor.Directive(WellKnownTypeNames.Inaccessible);
    }

    /// <inheritdoc cref="Inaccessible(IEnumTypeDescriptor)"/>
    public static IUnionTypeDescriptor Inaccessible(
        this IUnionTypeDescriptor descriptor)
    {
        ValidateDescriptor(descriptor);
        return descriptor.Directive(WellKnownTypeNames.Inaccessible);
    }

    private static void ValidateDescriptor(IDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
    }
}
