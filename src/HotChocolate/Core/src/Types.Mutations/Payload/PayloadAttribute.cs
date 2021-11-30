#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// The <see cref="PayloadAttribute"/> is used to rewrite the response of a field and wrap it
/// in a payload.
/// </summary>
/// <para>
/// <c>[Payload]</c> is optimized for the use case of a single field on the payload.
/// (or two if you use <see cref="ErrorAttribute"/>). If you need more than one field, you
/// should define your custom payload object.
/// </para>
/// Like:
/// <code>
/// public record CreateUserPayload(User user, string Email);
/// </code>
/// <example>
/// <para>
/// By annotating a resolver with <c>[Payload]</c> the response type is wrapped in a payload
/// object. By default the name of the payload field is the name of the returned object.
/// </para>
/// <code lang="csharp">
/// public class Mutation
/// {
///     [Payload]
///     public User CreateUserAsync(
///         [Service] IUserService service,
///         string username,
///         string name,
///         string lastName)
///         => userSerivce.CreateUser(username, name, lastName);
/// }
/// </code>
/// <para>
/// This results in the following schema
/// </para>
/// <code lang="csharp">
/// type CreateUserPayload {
///    user: User
/// }
/// </code>
/// <para>
/// The name of the field that holds the result, can be configured with the
/// <see cref="PayloadAttribute.FieldName"/> parameter of the attribute.
/// </para>
/// <code lang="csharp">
/// public class Mutation
/// {
///     [Payload("custom")]
///     public User CreateUserAsync(
///         [Service] IUserService service,
///         string username,
///         string name,
///         string lastName)
///         => userSerivce.CreateUser(username, name, lastName);
/// }
/// </code>
/// <para>
/// This results in the following schema
/// </para>
/// <code lang="csharp">
/// type CreateUserPayload {
///    custom: User
/// }
/// </code>
/// </example>
public class PayloadAttribute : ObjectFieldDescriptorAttribute
{
    /// <summary>
    /// The name of the field in the payload
    /// <code>
    /// type CreateUserPayload {
    ///    thesIsTheFieldName: User
    /// }
    /// </code>
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// The type name of the field in the payload
    /// <code>
    /// type ThisIsTheTypeName {
    ///    user: User
    /// }
    /// </code>
    /// </summary>
    public string? TypeName { get; set; }

    /// <inheritdoc cref="PayloadAttribute"/>
    /// <param name="fieldName">
    /// The name of the field in the payload
    /// <code>
    /// type CreateUserPayload {
    ///    thisIsTheFieldName: User
    /// }
    /// </code>
    /// </param>
    public PayloadAttribute(string fieldName)
    {
        FieldName = fieldName;
    }

    /// <inheritdoc cref="PayloadAttribute"/>
    public PayloadAttribute()
    {
    }

    /// <inheritdoc />
    public override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member) =>
        descriptor.Payload(FieldName, TypeName);
}
