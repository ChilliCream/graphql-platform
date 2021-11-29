using System;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using static System.AttributeTargets;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// The input attribute can be used to combine arguments in a input object. By default all
/// parameters of a method are combined into a input object and a argument is added.
/// </summary>
/// <example>
/// <para>
/// If you apply <c>Input</c> to a member of a class, all the arguments are combined into
/// one input type.
/// </para>
/// <code lang="c#">
/// public class Mutation
/// {
///    [Input]
///    public User CreateUserAsync(
///        [Service] IUserService service,
///        string username,
///        string name,
///        string lastName)
///        => userSerivce.CreateUser(username, name, lastName);
/// }
/// </code>
/// <para>
/// The code example above will created the following graphql schema:
/// </para>
/// <code lang="graphql">
/// type Mutation {
///   createUser(input: CreateUserInput): CreateUserPayload
/// }
///
/// type CreateUserPayload {
///  user: User
/// }
///
/// input CreateUserInput {
///   username: String!
///   name: String!
///   lastName: String!
/// }
/// </code>
/// <para>
/// By default the argument name is <c>input</c> but you can change it by specifying the
/// <see cref="InputAttribute.ArgumentName"/> parameter on input. e.g. <c>[Input("custom")]</c>
/// </para>
/// <code lang="cs">
/// public class Mutation
/// {
///    [Input("custom")]
///    public User CreateUserAsync(
///        [Service] IUserService service,
///        string username,
///        string name,
///        string lastName)
///        => userSerivce.CreateUser(username, name, lastName);
/// }
/// </code>
/// <para>
/// The code example above will created the following graphql schema:
/// </para>
/// <code lang="graphql">
/// type Mutation {
///   createUser(custom: CreateUserCustomInput): CreateUserPayload
/// }
///
/// type CreateUserPayload {
///  user: User
/// }
///
/// input CreateUserCustomInput {
///   username: String!
///   name: String!
///   lastName: String!
/// }
/// </code>
/// <para>
/// The [Input] attribute can also be applied on parameters.
/// </para>
/// <code lang="cs">
/// public class Mutation
/// {
///    [Input("custom")]
///    public User CreateUserAsync(
///        [Service] IUserService service,
///        string username,
///        string name,
///        [Input("foo")] string lastName)
///        => userSerivce.CreateUser(username, name, lastName);
/// }
/// </code>
/// <para>
/// The code example above will created the following graphql schema:
/// </para>
/// <code lang="graphql">
/// type Mutation {
///   createUser(custom: CreateUserCustomInput, foo: CreateUserFooInput): CreateUserPayload
/// }
///
/// type CreateUserPayload {
///  user: User
/// }
///
/// input CreateUserCustomInput {
///   username: String!
///   name: String!
/// }
/// input CreateUserFooInput {
///   lastName: String!
/// }
/// </code>
/// </example>
[AttributeUsage(Property | Method | Parameter)]
public class InputAttribute : DescriptorAttribute
{
    /// <summary>
    /// The name of the argument of the field
    /// <code>
    /// type Mutation {
    ///   createUser(thisIsTheArgumentName: CreateUserCustomInput): CreateUserPayload
    /// }
    /// </code>
    /// </summary>
    public string ArgumentName { get; set; } = "input";

    /// <summary>
    /// The type name of the argument of the field
    /// <code>
    /// type Mutation {
    ///   createUser(input: ThisIsTheTypeName): CreateUserPayload
    /// }
    /// </code>
    /// </summary>
    public string? TypeName { get; set; }

    /// <inheritdoc cref="InputAttribute"/>
    /// <param name="argumentName">
    /// The name of the argument of the field
    /// <code>
    /// type Mutation {
    ///   createUser(thisIsTheArgumentName: CreateUserCustomInput): CreateUserPayload
    /// }
    /// </code>
    /// </param>
    public InputAttribute(string argumentName)
    {
        ArgumentName = argumentName;
    }

    /// <inheritdoc cref="InputAttribute"/>
    public InputAttribute()
    {
    }

    /// <inheritdoc />
    protected override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        if (descriptor is ArgumentDescriptor argumentDescriptor)
        {
            argumentDescriptor.Input(ArgumentName, TypeName);
        }
        else if (descriptor is ObjectFieldDescriptor objectFieldDescriptor)
        {
            objectFieldDescriptor.Input(ArgumentName, TypeName);
        }
    }
}
