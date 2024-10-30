using HotChocolate.Types.Relay;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// <c>.ID()</c> marks a field or parameter as a Global Unique Id.
/// The type of the target is rewritten to <c>ID</c> and a middleware is registered that,
/// automatically combines the value of fields annotated as ID with another value to form a
/// global identifier.
/// </summary>
/// <remarks>
/// Per default, this additional value is the name of the type the Id belongs to.
/// Since type names are unique within a schema, this ensures that we are returning a unique
/// Id within the schema. If our GraphQL server serves multiple schemas, the schema name
/// is also included in this combined Id. The resulting Id is then Base64 encoded to make
/// it opaque.
/// </remarks>
/// <example>
/// <para>
/// A field can be rewritten to a id by adding <c>[ID]</c> to the resolver.
/// </para>
/// <code>
/// public class User
/// {
///     public int Id {get; set;}
/// }
/// public class UserType : ObjectType&gt;User>
/// {
///     protected override void Configure(IObjectTypeDescriptor&gt;User> descriptor)
///     {
///         descriptor.Field(x => x.User).ID();
///     }
/// }
/// </code>
/// <para>
/// In the resulting schema, the field `<c>User.id</c>` will be rewritten from `<c>Int</c>`
/// to `<c>ID</c>`
/// </para>
/// <code>
/// type User
/// {
///     id: ID!
/// }
/// </code>
/// <para>
/// If `<c>User.id</c>` is requested in a query, the value is transformed to a base64 string
/// combined with the typename
/// Assuming `<c>User.id</c>` has the value 1. The following string is base64 encoded
/// <code>
/// User
/// i1
/// </code>
/// results in
/// <code>
/// VXNlcjox
/// </code>
/// </para>
/// </example>
public static class RelayIdFieldExtensions
{
    /// <inheritdoc cref="RelayIdFieldExtensions"/>
    /// <param name="descriptor">the descriptor</param>
    /// <param name="typeName">
    /// Sets the <see cref="IDAttribute.TypeName">type name</see> of the relay id
    /// </param>
    public static IInputFieldDescriptor ID(
        this IInputFieldDescriptor descriptor,
        string? typeName = default)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        RelayIdFieldHelpers.ApplyIdToField(descriptor, typeName);

        return descriptor;
    }

    /// <inheritdoc cref="RelayIdFieldExtensions"/>
    /// <param name="descriptor">the descriptor</param>
    /// <typeparam name="T">
    /// the type from which the <see cref="IDAttribute.TypeName">type name</see> is derived
    /// </typeparam>
    public static IInputFieldDescriptor ID<T>(this IInputFieldDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        RelayIdFieldHelpers.ApplyIdToField(descriptor, typeof(T).Name);

        return descriptor;
    }

    /// <inheritdoc cref="RelayIdFieldExtensions"/>
    /// <param name="descriptor">the descriptor</param>
    /// <param name="typeName">
    /// Sets the <see cref="IDAttribute.TypeName">type name</see> of the relay id
    /// </param>
    public static IArgumentDescriptor ID(
        this IArgumentDescriptor descriptor,
        string? typeName = default)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        RelayIdFieldHelpers.ApplyIdToField(descriptor, typeName);

        return descriptor;
    }

    /// <inheritdoc cref="RelayIdFieldExtensions"/>
    /// <param name="descriptor">the descriptor</param>
    /// <typeparam name="T">
    /// the type from which the <see cref="IDAttribute.TypeName">type name</see> is derived
    /// </typeparam>
    public static IArgumentDescriptor ID<T>(this IArgumentDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        RelayIdFieldHelpers.ApplyIdToField(descriptor, typeof(T).Name);

        return descriptor;
    }

    /// <inheritdoc cref="RelayIdFieldExtensions"/>
    /// <param name="descriptor">the descriptor</param>
    /// <param name="typeName">
    /// Sets the <see cref="IDAttribute.TypeName">type name</see> of the relay id
    /// </param>
    public static IObjectFieldDescriptor ID(
        this IObjectFieldDescriptor descriptor,
        string? typeName = default)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        RelayIdFieldHelpers.ApplyIdToField(descriptor, typeName);

        return descriptor;
    }

    /// <inheritdoc cref="RelayIdFieldExtensions"/>
    /// <param name="descriptor">the descriptor</param>
    /// <typeparam name="T">
    /// the type from which the <see cref="IDAttribute.TypeName">type name</see> is derived
    /// </typeparam>
    public static IObjectFieldDescriptor ID<T>(this IObjectFieldDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        RelayIdFieldHelpers.ApplyIdToField(descriptor, typeof(T).Name);

        return descriptor;
    }

    /// <inheritdoc cref="RelayIdFieldExtensions"/>
    /// <param name="descriptor">the descriptor</param>
    public static IInterfaceFieldDescriptor ID(this IInterfaceFieldDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        RelayIdFieldHelpers.ApplyIdToField(descriptor);

        return descriptor;
    }
}
