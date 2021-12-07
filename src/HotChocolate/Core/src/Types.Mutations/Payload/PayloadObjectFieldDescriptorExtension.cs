#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Common extensions for <c>Payload()</c>
/// </summary>
public static class PayloadObjectFieldDescriptorExtension
{
    /// <summary>
    /// <c>.Payload()</c> is used to rewrite the response of a field and wrap it in a payload.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>.Payload()</c> is optimized for the use case of a single field on the payload.
    /// (or two if you use <see cref="ErrorAttribute"/>). If you need more than one field, you
    /// should define your custom payload object.
    /// </para>
    /// Like:
    /// <code>
    /// public record CreateUserPayload(User user, string Email);
    /// </code>
    /// </remarks>
    /// <example>
    /// <para>
    /// By annotating a resolver with <c>[Payload]</c> the response type is wrapped in a payload
    /// object. By default the name of the payload field is the name of the returned object.
    /// </para>
    /// <code lang="csharp">
    /// public class Mutation
    /// {
    ///     public User CreateUserAsync(
    ///         string username,
    ///         string name,
    ///         string lastName)
    ///         => ///...;
    /// }
    ///
    /// public class MutationType : ObjectType&lt;Mutation&gt;
    /// {
    ///     protected override Configure(IObjectTypeDescriptor&lt;Mutation&gt; descriptor)
    ///     {
    ///         descriptor
    ///            .Field(x =>; x.CreateUserAsync(default, default, default)
    ///            <b>.Payload();</b>
    ///     }
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
    /// <paramref name="fieldName"/> parameter of the attribute.
    /// </para>
    /// <code lang="csharp">
    /// public class Mutation
    /// {
    ///     public User CreateUserAsync(
    ///         string username,
    ///         string name,
    ///         string lastName)
    ///         => ///...;
    /// }
    ///
    /// public class MutationType : ObjectType&lt;Mutation&gt;
    /// {
    ///     protected override Configure(IObjectTypeDescriptor&lt;Mutation&gt; descriptor)
    ///     {
    ///         descriptor
    ///            .Field(x =>; x.CreateUserAsync(default, default, default)
    ///            <b>.Payload("custom");</b>
    ///     }
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
    /// <param name="descriptor">The descriptor of the field</param>
    /// <param name="fieldName">
    /// The name of the field in the payload
    /// <code>
    /// type CreateUserPayload {
    ///    thisIsTheFieldName: User
    /// }
    /// </code>
    /// </param>
    /// <param name="typeName">
    /// The type name of the payload
    /// <code>
    /// type ThisIsTheTypeName {
    ///    user: User
    /// }
    /// </code>
    /// </param>
    public static IObjectFieldDescriptor Payload(
        this IObjectFieldDescriptor descriptor,
        string? fieldName = null,
        string? typeName = null)
    {
        ExtensionData contextData = descriptor.Extend().Definition.ContextData;
        contextData[PayloadContextData.Payload] = new PayloadContextData(fieldName, typeName);
        return descriptor;
    }
}
