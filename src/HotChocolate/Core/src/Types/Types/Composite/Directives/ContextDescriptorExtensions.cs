namespace HotChocolate.Types.Composite;

/// <summary>
/// Provides extension methods to type system descriptors to apply the @context directive.
/// </summary>
public static class ContextDescriptorExtensions
{
    /// <summary>
    /// <para>
    /// Applies the @context directive to the object type to declare that its instances
    /// provide the named context.
    /// </para>
    /// <para>
    /// <code language="graphql">
    /// type User @context(name: "userContext") {
    ///   id: ID!
    /// }
    /// </code>
    /// </para>
    /// </summary>
    /// <param name="descriptor">The object type descriptor.</param>
    /// <param name="name">The name of the context.</param>
    /// <returns>The object type descriptor with the @context directive applied.</returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The <paramref name="name"/> is <c>null</c> or empty.
    /// </exception>
    public static IObjectTypeDescriptor Context(
        this IObjectTypeDescriptor descriptor,
        string name)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrEmpty(name);

        return descriptor.Directive(new Context(name));
    }

    /// <summary>
    /// <para>
    /// Applies the @context directive to the object type to declare that its instances
    /// provide the named context.
    /// </para>
    /// <para>
    /// <code language="graphql">
    /// type User @context(name: "userContext") {
    ///   id: ID!
    /// }
    /// </code>
    /// </para>
    /// </summary>
    /// <typeparam name="T">The runtime type of the object type.</typeparam>
    /// <param name="descriptor">The object type descriptor.</param>
    /// <param name="name">The name of the context.</param>
    /// <returns>The object type descriptor with the @context directive applied.</returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The <paramref name="name"/> is <c>null</c> or empty.
    /// </exception>
    public static IObjectTypeDescriptor<T> Context<T>(
        this IObjectTypeDescriptor<T> descriptor,
        string name)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrEmpty(name);

        return descriptor.Directive(new Context(name));
    }

    /// <summary>
    /// <para>
    /// Applies the @context directive to the interface type to declare that its implementations
    /// provide the named context.
    /// </para>
    /// <para>
    /// <code language="graphql">
    /// interface Node @context(name: "nodeContext") {
    ///   id: ID!
    /// }
    /// </code>
    /// </para>
    /// </summary>
    /// <param name="descriptor">The interface type descriptor.</param>
    /// <param name="name">The name of the context.</param>
    /// <returns>The interface type descriptor with the @context directive applied.</returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The <paramref name="name"/> is <c>null</c> or empty.
    /// </exception>
    public static IInterfaceTypeDescriptor Context(
        this IInterfaceTypeDescriptor descriptor,
        string name)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrEmpty(name);

        return descriptor.Directive(new Context(name));
    }

    /// <summary>
    /// <para>
    /// Applies the @context directive to the interface type to declare that its implementations
    /// provide the named context.
    /// </para>
    /// <para>
    /// <code language="graphql">
    /// interface Node @context(name: "nodeContext") {
    ///   id: ID!
    /// }
    /// </code>
    /// </para>
    /// </summary>
    /// <typeparam name="T">The runtime type of the interface type.</typeparam>
    /// <param name="descriptor">The interface type descriptor.</param>
    /// <param name="name">The name of the context.</param>
    /// <returns>The interface type descriptor with the @context directive applied.</returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The <paramref name="name"/> is <c>null</c> or empty.
    /// </exception>
    public static IInterfaceTypeDescriptor<T> Context<T>(
        this IInterfaceTypeDescriptor<T> descriptor,
        string name)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrEmpty(name);

        return descriptor.Directive(new Context(name));
    }

    /// <summary>
    /// <para>
    /// Applies the @context directive to the union type to declare that its member types
    /// provide the named context.
    /// </para>
    /// <para>
    /// <code language="graphql">
    /// union Account @context(name: "accountContext") = User | Organization
    /// </code>
    /// </para>
    /// </summary>
    /// <param name="descriptor">The union type descriptor.</param>
    /// <param name="name">The name of the context.</param>
    /// <returns>The union type descriptor with the @context directive applied.</returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The <paramref name="name"/> is <c>null</c> or empty.
    /// </exception>
    public static IUnionTypeDescriptor Context(
        this IUnionTypeDescriptor descriptor,
        string name)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrEmpty(name);

        return descriptor.Directive(new Context(name));
    }
}
