#nullable enable

using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;

namespace HotChocolate.Types.Composite;

/// <summary>
/// Provides extension methods to apply the <see cref="Composite.Shareable"/>
/// directive with the fluent API to object types and object fields.
/// </summary>
public static class ShareableDirectiveExtensions
{
    /// <summary>
    /// <para>
    /// Makes all fields of the section of the type that is in focus shareable by applying the @shareable
    /// directive.
    /// </para>
    /// <para>
    /// By default, only a single source schema is allowed to contribute
    /// a particular field to an object type.
    /// </para>
    /// <para>
    /// This prevents source schemas from inadvertently defining similarly named
    /// fields that are not semantically equivalent.
    /// </para>
    /// <para>
    /// This is why fields must be explicitly marked as @shareable to allow multiple source
    /// schemas to define them, ensuring that the decision to serve a field from
    /// more than one source schema is intentional and coordinated.
    /// </para>
    /// <para>
    /// <code language="csharp">
    /// extend type User @shareable {
    ///   name: String!
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--shareable"/>
    /// </para>
    /// </summary>
    /// <param name="descriptor">The object type descriptor.</param>
    /// <returns>The object type descriptor with the <see cref="Composite.Shareable"/> directive applied.</returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor Shareable(this IObjectTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        // The @sharable directive on a type is meant as a helper to apply it to all fields within its scope.
        // This behavior is mainly defined to aid schema-first workflows.
        // In Hot Chocolate, we focus on code-first and implementation-first flows.
        // This means we do not typically use the `extend` keyword, and types can be split in different ways.
        // To maintain the same scoping mechanism, we add the dependency to the sharable directive
        // so that it will be properly initialized.
        var extend = descriptor.Extend();
        extend.Configuration.Dependencies.Add(
            new TypeDependency(
                extend.Context.TypeInspector.GetTypeRef(typeof(Shareable)),
                TypeDependencyFulfilled.Named));

        // Second, we apply the shareable directive to all fields of the current type part before we merge them.
        descriptor.Extend().OnBeforeNaming(
            (ctx, def) =>
            {
                foreach (var field in def.Fields)
                {
                    field.AddDirective(Composite.Shareable.Instance, ctx.TypeInspector);
                }
            });

        return descriptor;
    }

    /// <summary>
    /// <para>
    /// Makes the field shareable by applying the @shareable directive to it.
    /// </para>
    /// <para>
    /// By default, only a single source schema is allowed to contribute
    /// a particular field to an object type.
    /// </para>
    /// <para>
    /// This prevents source schemas from inadvertently defining similarly named
    /// fields that are not semantically equivalent.
    /// </para>
    /// <para>
    /// This is why fields must be explicitly marked as @shareable to allow multiple source
    /// schemas to define them, ensuring that the decision to serve a field from
    /// more than one source schema is intentional and coordinated.
    /// </para>
    /// <para>
    /// <code language="csharp">
    /// type User {
    ///   name: String! @shareable
    ///   email: String!
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--shareable"/>
    /// </para>
    /// </summary>
    /// <param name="descriptor">The object field descriptor.</param>
    /// <returns>The object field descriptor with the <see cref="Composite.Shareable"/> directive applied.</returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectFieldDescriptor Shareable(this IObjectFieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Composite.Shareable.Instance);
    }
}
