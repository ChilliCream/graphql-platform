namespace HotChocolate.ApolloFederation.Types;

public static class InterfaceObjectDescriptorExtensions
{
    /// <summary>
    /// Applies the @interfaceObject directive which provides meta information to the router that this entity
    /// type defined within this subgraph is an interface in the supergraph. This allows you to extend functionality
    /// of an interface across the supergraph without having to implement (or even be aware of) all its implementing types.
    /// <example>
    /// type Foo @interfaceObject @key(fields: "ids") {
    ///   id: ID!
    ///   newCommonField: String
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor on which this directive shall be annotated.
    /// </param>
    /// <returns>
    /// Returns the object field descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor<T> InterfaceObject<T>(this IObjectTypeDescriptor<T> descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return descriptor.Directive(InterfaceObjectDirective.Default);
    }

    /// <summary>
    /// Applies the @interfaceObject directive which provides meta information to the router that this entity
    /// type defined within this subgraph is an interface in the supergraph. This allows you to extend functionality
    /// of an interface across the supergraph without having to implement (or even be aware of) all its implementing types.
    /// <example>
    /// type Foo @interfaceObject @key(fields: "ids") {
    ///   id: ID!
    ///   newCommonField: String
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor on which this directive shall be annotated.
    /// </param>
    /// <returns>
    /// Returns the object field descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor InterfaceObject(this IObjectTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return descriptor.Directive(InterfaceObjectDirective.Default);
    }
}
