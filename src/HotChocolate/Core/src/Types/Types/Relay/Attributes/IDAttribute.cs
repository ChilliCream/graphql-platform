using System.Reflection;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types.Relay;

/// <summary>
/// The <see cref="IDAttribute"/> marks a field or parameter as a Global Unique Id.
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
///     [ID]
///     public int Id {get; set;}
/// }
/// </code>
/// <para>
/// In the resulting schema, the field `<c>User.id</c>` will be rewritten from `<c>Int</c>` to
/// `<c>ID</c>`
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
[AttributeUsage(
    AttributeTargets.Parameter |
    AttributeTargets.Property |
    AttributeTargets.Method)]
// ReSharper disable once InconsistentNaming
public class IDAttribute : DescriptorAttribute
{
    /// <inheritdoc cref="IDAttribute"/>
    public IDAttribute(string? typeName = null)
    {
        TypeName = typeName;
    }

    /// <summary>
    /// With the <see cref="IDAttribute.TypeName"/> property you can override the type name
    /// of the ID. This is useful to rewrite a parameter of a mutation or query, to a specific
    /// id.
    /// </summary>
    /// <example>
    /// <para>
    /// A field can be rewritten to a id by adding <c>[ID]</c> to the resolver.
    /// </para>
    /// <code>
    /// public class UserQuery
    /// {
    ///     public User GetUserById([ID("User")] int id) => //....
    /// }
    /// </code>
    /// <para>
    /// The argument is rewritten to <c>ID</c> and expect a id of type User.
    /// Assuming `<c>User.id</c>` has the value 1. The following string is base64 encoded
    /// </para>
    /// </example>
    public string? TypeName { get; }

    /// <inheritdoc />
    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        switch (descriptor)
        {
            case IInputFieldDescriptor d when element is PropertyInfo:
                d.ID(TypeName);
                break;
            case IArgumentDescriptor d when element is ParameterInfo:
                d.ID(TypeName);
                break;
            case IObjectFieldDescriptor d when element is MemberInfo:
                d.ID(TypeName);
                break;
            case IInterfaceFieldDescriptor d when element is MemberInfo:
                d.ID();
                break;
        }
    }
}

/// <summary>
/// The <see cref="IDAttribute{T}"/> marks a field or parameter as a Global Unique Id.
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
///     [ID]
///     public int Id {get; set;}
/// }
/// </code>
/// <para>
/// In the resulting schema, the field `<c>User.id</c>` will be rewritten from `<c>Int</c>` to
/// `<c>ID</c>`
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
[AttributeUsage(
    AttributeTargets.Parameter |
    AttributeTargets.Property |
    AttributeTargets.Method)]
// ReSharper disable once InconsistentNaming
public class IDAttribute<T> : DescriptorAttribute
{
    /// <inheritdoc cref="IDAttribute{T}"/>
    public IDAttribute()
    {
        TypeName = typeof(T).Name;
    }

    /// <summary>
    /// With the <see cref="IDAttribute.TypeName"/> property you can override the type name
    /// of the ID. This is useful to rewrite a parameter of a mutation or query, to a specific
    /// id.
    /// </summary>
    /// <example>
    /// <para>
    /// A field can be rewritten to a id by adding <c>[ID]</c> to the resolver.
    /// </para>
    /// <code>
    /// public class UserQuery
    /// {
    ///     public User GetUserById([ID("User")] int id) => //....
    /// }
    /// </code>
    /// <para>
    /// The argument is rewritten to <c>ID</c> and expect a id of type User.
    /// Assuming `<c>User.id</c>` has the value 1. The following string is base64 encoded
    /// </para>
    /// </example>
    public string? TypeName { get; }

    /// <inheritdoc />
    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        switch (descriptor)
        {
            case IInputFieldDescriptor d when element is PropertyInfo:
                d.ID(TypeName);
                break;
            case IArgumentDescriptor d when element is ParameterInfo:
                d.ID(TypeName);
                break;
            case IObjectFieldDescriptor d when element is MemberInfo:
                d.ID(TypeName);
                break;
            case IInterfaceFieldDescriptor d when element is MemberInfo:
                d.ID();
                break;
        }
    }
}
