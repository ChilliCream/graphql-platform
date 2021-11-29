#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Common extension for <c>.Input()</c>
/// </summary>
public static class InputDescriptorExtensions
{
    /// <summary>
    /// Is <c>.Input()</c> used on a argument then the argument will be added to the input
    /// object and removed as a parameter. With the <paramref name="argumentName"/> argument, you
    /// specify what the argument name of the input object should be.
    /// </summary>
    /// <example>
    /// <code lang="c#">
    /// public class Mutation
    /// {
    ///    public User CreateUserAsync(
    ///        string username,
    ///        string name,
    ///        string lastName)
    ///        => ///...;
    /// }
    ///
    /// public class MutationType : ObjectType&lt;Mutation&gt;
    /// {
    ///     protected override Configure(IObjectTypeDescriptor&lt;Mutation&gt; descriptor)
    ///     {
    ///         descriptor
    ///            .Field(x =&gt; x.CreateUserAsync(default, default, default)
    ///            .Argument("username", x => x.Input())
    ///            .Argument("name", x => x.Input())
    ///            .Argument("lastName", x => x.Input("custom"))
    ///     }
    /// }
    /// </code>
    /// <para>
    /// The code example above will created the following graphql schema:
    /// </para>
    /// <code lang="graphql">
    /// type Mutation {
    ///   createUser(input: CreateUserInput, custom: CreateUserCustomInput): CreateUserPayload
    /// }
    ///
    /// type CreateUserPayload {
    ///  user: User
    /// }
    ///
    /// input CreateUserInput {
    ///   username: String!
    ///   name: String!
    /// }
    /// input CreateUserCustomInput {
    ///   lastName: String!
    /// }
    /// </code>
    /// </example>
    /// <param name="descriptor">The descriptor of the argument</param>
    /// <param name="argumentName">
    /// The name of the argument
    /// <code>
    /// type Mutation {
    ///   createUser(thisIsTheArgumentName: CreateUserCustomInput): CreateUserPayload
    /// }
    /// </code>
    /// </param>
    /// <param name="typeName">
    /// The type name of the input type
    /// <code>
    /// type Mutation {
    ///   createUser(input: ThisIsTheTypeName): CreateUserPayload
    /// }
    /// </code>
    /// </param>
    public static IArgumentDescriptor Input(
        this IArgumentDescriptor descriptor,
        string argumentName = "input",
        string? typeName = null)
    {
        ExtensionData contextData = descriptor.Extend().Definition.ContextData;
        contextData[InputContextData.Input] = new InputContextData(typeName, argumentName);
        return descriptor;
    }

    /// <summary>
    /// The <c>.Input()</c> method can be used to combine arguments in a
    /// input object. By default all parameters of a method are combined into a input object
    /// and a argument is added.
    /// </summary>
    /// <example>
    /// <para>
    /// If you call <c>.Input()</c> on a field descriptor of a class, all the arguments are
    /// combined into one input type.
    /// </para>
    /// <code lang="c#">
    /// public class Mutation
    /// {
    ///    public User CreateUserAsync(
    ///        string username,
    ///        string name,
    ///        string lastName)
    ///        => ///...;
    /// }
    ///
    /// public class MutationType : ObjectType&lt;Mutation&gt;
    /// {
    ///     protected override Configure(IObjectTypeDescriptor&lt;Mutation&gt; descriptor)
    ///     {
    ///         descriptor
    ///            .Field(x =&gt; x.CreateUserAsync(default, default, default)
    ///            <b>.Input();</b>
    ///     }
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
    /// <c>name</c> parameter on input. e.g. <c>.Input("custom")</c>
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
    ///        => //;
    /// }
    /// public class MutationType : ObjectType&lt;Mutation>
    /// {
    ///     protected override Configure(IObjectTypeDescriptor&lt;Mutation> descriptor)
    ///     {
    ///         descriptor
    ///            .Field(x =>; x.CreateUserAsync(default, default, default)
    ///            <b>.Input("custom");</b>
    ///     }
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
    /// </example>
    /// <param name="descriptor">The descriptor of the field</param>
    /// <param name="argumentName">
    /// The name of the argument of the field
    /// <code>
    /// type Mutation {
    ///   createUser(thisIsTheArgumentName: CreateUserCustomInput): CreateUserPayload
    /// }
    /// </code>
    /// </param>
    /// <param name="typeName">
    /// The type name of the input type
    /// <code>
    /// type Mutation {
    ///   createUser(input: ThisIsTheTypeName): CreateUserPayload
    /// }
    /// </code>
    /// </param>
    public static IObjectFieldDescriptor Input(
        this IObjectFieldDescriptor descriptor,
        string argumentName = "input",
        string? typeName = null)
    {
        ExtensionData contextData = descriptor.Extend().Definition.ContextData;
        contextData[InputContextData.Input] = new InputContextData(typeName, argumentName);
        return descriptor;
    }
}
