using HotChocolate.Internal;
using HotChocolate.Types.Descriptors.Configurations;

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
    /// <param name="scoped">
    /// if <c>true</c> and this directive is applied to an interface type extension,
    /// it will only be applied to the fields that this interface type extension exposes.
    /// if scoped is <c>false</c> the directive will be applied to the type and will thus make all
    /// fields of the type sharable.
    /// </param>
    /// <returns>
    /// The interface type descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IInterfaceTypeDescriptor Inaccessible(
        this IInterfaceTypeDescriptor descriptor,
        bool scoped = false)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (scoped)
        {
            // The @inaccessible directive on a type is meant as a helper to apply it to all fields within its scope.
            // This behavior is mainly defined to aid schema-first workflows.
            // In Hot Chocolate, we focus on code-first and implementation-first flows.
            // This means we do not typically use the `extend` keyword, and types can be split in different ways.
            // To maintain the same scoping mechanism, we add the dependency to the sharable directive
            // so that it will be properly initialized.
            var extend = descriptor.Extend();
            extend.Configuration.Dependencies.Add(
                new TypeDependency(
                    extend.Context.TypeInspector.GetTypeRef(typeof(Inaccessible)),
                    TypeDependencyFulfilled.Named));

            // Second, we apply the inaccessible directive to all fields of the current type part before we merge them.
            descriptor.Extend().OnBeforeNaming((ctx, def) =>
            {
                foreach (var field in def.Fields)
                {
                    field.AddDirective(Composite.Inaccessible.Instance, ctx.TypeInspector);
                }
            });
        }
        else
        {
            descriptor.Directive(Composite.Inaccessible.Instance);
        }

        return descriptor;
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
    /// <param name="scoped">
    /// if <c>true</c> and this directive is applied to an object type extension,
    /// it will only be applied to the fields that this object type extension exposes.
    /// if scoped is <c>false</c> the directive will be applied to the type and will thus make all
    /// fields of the type sharable.
    /// </param>
    /// <returns>
    /// The object type descriptor with the directive applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor Inaccessible(
        this IObjectTypeDescriptor descriptor,
        bool scoped = false)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (scoped)
        {
            // The @inaccessible directive on a type is meant as a helper to apply it to all fields within its scope.
            // This behavior is mainly defined to aid schema-first workflows.
            // In Hot Chocolate, we focus on code-first and implementation-first flows.
            // This means we do not typically use the `extend` keyword, and types can be split in different ways.
            // To maintain the same scoping mechanism, we add the dependency to the sharable directive
            // so that it will be properly initialized.
            var extend = descriptor.Extend();
            extend.Configuration.Dependencies.Add(
                new TypeDependency(
                    extend.Context.TypeInspector.GetTypeRef(typeof(Inaccessible)),
                    TypeDependencyFulfilled.Named));

            // Second, we apply the inaccessible directive to all fields of the current type part before we merge them.
            descriptor.Extend().OnBeforeNaming((ctx, def) =>
            {
                foreach (var field in def.Fields)
                {
                    field.AddDirective(Composite.Inaccessible.Instance, ctx.TypeInspector);
                }
            });
        }
        else
        {
            descriptor.Directive(Composite.Inaccessible.Instance);
        }

        return descriptor;
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
