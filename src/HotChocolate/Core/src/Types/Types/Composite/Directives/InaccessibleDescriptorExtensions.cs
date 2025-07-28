#nullable enable

namespace HotChocolate.Types.Composite;

/// <summary>
/// Provides extension methods to type system descriptors to apply the @inaccessible directive.
/// </summary>
public static class InaccessibleDescriptorExtensions
{
    /// <summary>
    /// <para>
    /// Applies the @inaccessible directive to the enum type to prevent it
    /// from being accessible through the client-facing composite schema,
    /// even if it is accessible in the underlying source schemas.
    /// </para>
    /// <para>
    /// <code language="graphql">
    /// enum UserType @inaccessible {
    ///   ADMIN
    ///   USER
    ///   GUEST
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--inaccessible"/>
    /// </para>
    /// </summary>
    /// <param name="descriptor">
    /// The enum type descriptor to apply the directive to.
    /// </param>
    /// <returns>
    /// The enum type descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IEnumTypeDescriptor Inaccessible(
        this IEnumTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Composite.Inaccessible.Instance);
    }

    /// <summary>
    /// <para>
    /// Applies the @inaccessible directive to the enum value to prevent it
    /// from being accessible through the client-facing composite schema,
    /// even if it is accessible in the underlying source schemas.
    /// </para>
    /// <para>
    /// <code language="graphql">
    /// enum UserType {
    ///   ADMIN @inaccessible
    ///   USER
    ///   GUEST
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--inaccessible"/>
    /// </para>
    /// </summary>
    /// <param name="descriptor">
    /// The enum value descriptor to apply the directive to.
    /// </param>
    /// <returns>
    /// The enum value descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IEnumValueDescriptor Inaccessible(
        this IEnumValueDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Composite.Inaccessible.Instance);
    }

    /// <summary>
    /// <para>
    /// Applies the @inaccessible directive to the interface type to prevent it
    /// from being accessible through the client-facing composite schema,
    /// even if it is accessible in the underlying source schemas.
    /// </para>
    /// <para>
    /// <code language="graphql">
    /// interface User @inaccessible {
    ///   id: ID!
    ///   name: String!
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--inaccessible"/>
    /// </para>
    /// </summary>
    /// <param name="descriptor">
    /// The interface type descriptor to apply the directive to.
    /// </param>
    /// <returns>
    /// The interface type descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IInterfaceTypeDescriptor Inaccessible(
        this IInterfaceTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Composite.Inaccessible.Instance);
    }

    /// <summary>
    /// <para>
    /// Applies the @inaccessible directive to the interface field to prevent it
    /// from being accessible through the client-facing composite schema,
    /// even if it is accessible in the underlying source schemas.
    /// </para>
    /// <para>
    /// <code language="graphql">
    /// interface User {
    ///   id: ID! @inaccessible
    ///   name: String!
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--inaccessible"/>
    /// </para>
    /// </summary>
    /// <param name="descriptor">
    /// The interface field descriptor to apply the directive to.
    /// </param>
    /// <returns>
    /// The interface field descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IInterfaceFieldDescriptor Inaccessible(
        this IInterfaceFieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Composite.Inaccessible.Instance);
    }

    /// <summary>
    /// <para>
    /// Applies the @inaccessible directive to the input object type to prevent it
    /// from being accessible through the client-facing composite schema,
    /// even if it is accessible in the underlying source schemas.
    /// </para>
    /// <para>
    /// <code language="graphql">
    /// input UserInput @inaccessible {
    ///   id: ID!
    ///   name: String!
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--inaccessible"/>
    /// </para>
    /// </summary>
    /// <param name="descriptor">
    /// The input object type descriptor to apply the directive to.
    /// </param>
    /// <returns>
    /// The input object type descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IInputObjectTypeDescriptor Inaccessible(
        this IInputObjectTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Composite.Inaccessible.Instance);
    }

    /// <summary>
    /// <para>
    /// Applies the @inaccessible directive to the input field to prevent it
    /// from being accessible through the client-facing composite schema,
    /// even if it is accessible in the underlying source schemas.
    /// </para>
    /// <para>
    /// <code language="graphql">
    /// input UserInput {
    ///   id: ID! @inaccessible
    ///   name: String!
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--inaccessible"/>
    /// </para>
    /// </summary>
    /// <param name="descriptor">
    /// The input field descriptor to apply the directive to.
    /// </param>
    /// <returns>
    /// The input field descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IInputFieldDescriptor Inaccessible(
        this IInputFieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Composite.Inaccessible.Instance);
    }

    /// <summary>
    /// <para>
    /// Applies the @inaccessible directive to the object type to prevent it
    /// from being accessible through the client-facing composite schema,
    /// even if it is accessible in the underlying source schemas.
    /// </para>
    /// <para>
    /// <code language="graphql">
    /// type User @inaccessible {
    ///   id: ID!
    ///   name: String!
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--inaccessible"/>
    /// </para>
    /// </summary>
    /// <param name="descriptor">
    /// The object type descriptor to apply the directive to.
    /// </param>
    /// <returns>
    /// The object type descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor Inaccessible(
        this IObjectTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Composite.Inaccessible.Instance);
    }

    /// <summary>
    /// <para>
    /// Applies the @inaccessible directive to the object field to prevent it
    /// from being accessible through the client-facing composite schema,
    /// even if it is accessible in the underlying source schemas.
    /// </para>
    /// <para>
    /// <code language="graphql">
    /// type User {
    ///   id: ID! @inaccessible
    ///   name: String!
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--inaccessible"/>
    /// </para>
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor to apply the directive to.
    /// </param>
    /// <returns>
    /// The object field descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectFieldDescriptor Inaccessible(
        this IObjectFieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Composite.Inaccessible.Instance);
    }

    /// <summary>
    /// <para>
    /// Applies the @inaccessible directive to the argument to prevent it
    /// from being accessible through the client-facing composite schema,
    /// even if it is accessible in the underlying source schemas.
    /// </para>
    /// <para>
    /// <code language="graphql">
    /// type Query {
    ///   user(id: ID!): User! @inaccessible
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--inaccessible"/>
    /// </para>
    /// </summary>
    /// <param name="descriptor">
    /// The argument descriptor to apply the directive to.
    /// </param>
    /// <returns>
    /// The argument descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IArgumentDescriptor Inaccessible(
        this IArgumentDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Composite.Inaccessible.Instance);
    }

    /// <summary>
    /// <para>
    /// Applies the @inaccessible directive to the union type to prevent it
    /// from being accessible through the client-facing composite schema,
    /// even if it is accessible in the underlying source schemas.
    /// </para>
    /// <para>
    /// <code language="graphql">
    /// union User @inaccessible = Admin | User | Guest
    /// </code>
    /// </para>
    /// <para>
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--inaccessible"/>
    /// </para>
    /// </summary>
    /// <param name="descriptor">
    /// The union type descriptor to apply the directive to.
    /// </param>
    /// <returns>
    /// The union type descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IUnionTypeDescriptor Inaccessible(
        this IUnionTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Composite.Inaccessible.Instance);
    }
}
