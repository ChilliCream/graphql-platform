namespace HotChocolate.ApolloFederation.Types;

public static class InaccessibleDescriptorExtensions
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
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(InaccessibleDirective.Default);
    }

    /// <inheritdoc cref="Inaccessible(IEnumTypeDescriptor)"/>
    public static IEnumValueDescriptor Inaccessible(
        this IEnumValueDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(InaccessibleDirective.Default);
    }

    /// <inheritdoc cref="Inaccessible(IEnumTypeDescriptor)"/>
    public static IInterfaceFieldDescriptor Inaccessible(
        this IInterfaceFieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(InaccessibleDirective.Default);
    }

    /// <inheritdoc cref="Inaccessible(IEnumTypeDescriptor)"/>
    public static IInterfaceTypeDescriptor Inaccessible(
        this IInterfaceTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(InaccessibleDirective.Default);
    }

    /// <inheritdoc cref="Inaccessible(IEnumTypeDescriptor)"/>
    public static IInputObjectTypeDescriptor Inaccessible(
        this IInputObjectTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(InaccessibleDirective.Default);
    }

    /// <inheritdoc cref="Inaccessible(IEnumTypeDescriptor)"/>
    public static IInputFieldDescriptor Inaccessible(
        this IInputFieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(InaccessibleDirective.Default);
    }

    /// <inheritdoc cref="Inaccessible(IEnumTypeDescriptor)"/>
    public static IObjectFieldDescriptor Inaccessible(
        this IObjectFieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(InaccessibleDirective.Default);
    }

    /// <inheritdoc cref="Inaccessible(IEnumTypeDescriptor)"/>
    public static IObjectTypeDescriptor Inaccessible(
        this IObjectTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(InaccessibleDirective.Default);
    }

    /// <inheritdoc cref="Inaccessible(IEnumTypeDescriptor)"/>
    public static IUnionTypeDescriptor Inaccessible(
        this IUnionTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(InaccessibleDirective.Default);
    }
}
