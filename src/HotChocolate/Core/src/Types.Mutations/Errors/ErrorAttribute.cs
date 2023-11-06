namespace HotChocolate.Types;

/// <summary>
/// The <see cref="ErrorAttribute"/> registers a middleware that will catch all exceptions of
/// type <see cref="ErrorAttribute.ErrorType"/> on mutations.
///
/// By annotating the attribute the response type
/// of the annotated mutation resolver, will be automatically extended by a field of type
/// <c>errors:[Error!]</c>. This field will return errors that are caught by the middleware.
/// All the other fields on this type will be rewritten to nullable types. In case of a error
/// these fields will be set to null.
/// <para>
/// There are three different ways to map exceptions to GraphQL errors.
/// </para>
/// </summary>
/// <remarks>
/// The idea of the error middleware is to keep the resolver clean of any error handling code
/// and use exceptions to signal a error state. The HotChocolate schema is automatically
/// rewritten into a common error handling pattern.
/// <a href="https://xuorig.medium.com/a-guide-to-graphql-errors-bb9ba9f15f85">Learn More</a>
/// </remarks>
/// <example>
/// <para>
/// There are three different ways to map exceptions to GraphQL errors.
/// </para>
/// <list type="number">
/// <item>
/// <para>
/// <b>Catching exceptions directly</b>
/// </para>
/// If <see cref="ErrorAttribute.ErrorType"/> is a exception, the exception is automatically
/// mapped into a GraphQL error and the middleware will catch this exception
/// <code>
/// public class Mutation
/// {
///     [Error(typeof(SomeSpecificDomainError))]
///     [Error(typeof(SomeOtherError))]
///     public CreateUserPayload CreateUser(CreateUserInput input)
///     {
///        // ...
///     }
/// }
///
/// public record CreateUserInput(string UserName);
///
/// public record CreateUserPayload(User User);
/// </code>
/// This will generate the following schema
/// <code>
/// type Mutation {
///   createUser(input: CreateUserInput!): CreateUserPayload!
/// }
///
/// input CreateUserInput {
///   userName: String!
/// }
///
/// type CreateUserPayload {
///   user: User
///   errors: [CreateUserError!]
/// }
///
/// type User {
///   username: String
/// }
///
/// interface Error {
///   message: String!
/// }
///
/// type SomeSpecificDomainError implements Error {
///   message: String!
/// }
///
/// type SomeOtherDomainError implements Error {
///   message: String!
/// }
///
/// union CreateUserError = SomeSpecificDomainError | SomeOtherDomainError
/// </code>
///    </item>
/// <item>
/// <para>
/// <b>Map Exceptions with a factory method</b>
/// </para>
/// <para>
/// If there should be any translation between exception and error, you can defined a class
/// with factory methods. These factory methods receive a <see cref="Exception"/> and return
/// a object which will be used as the representation of the error
/// </para>
/// <para>
/// A factory method has to be `public static` and the name of the method has to be
/// `CreateErrorFrom`. There should only be one parameter of type <see cref="Exception"/> and
/// it can return a arbitrary class/struct/record that will be used as the representation
/// of the error.
/// </para>
/// <code>
/// public class MyCustomError
/// {
///     public static MyCustomError CreateErrorFrom(DomainExceptionA ex)
///     {
///         return new MyCustomError();
///     }
///
///     public static MyCustomError CreateErrorFrom(DomainExceptionB ex)
///     {
///         return new MyCustomError();
///     }
///
///     public string Message => "My custom error Message";
/// }
///
/// public class Mutation
/// {
///     [Error(typeof(MyCustomError))]
///     public CreateUserPayload CreateUser(CreateUserInput input)
///     {
///        // ...
///     }
/// }
///
/// public record CreateUserInput(string UserName);
///
/// public record CreateUserPayload(User User);
/// </code>
/// </item>
/// <item>
/// <para>
/// <b>Map exceptions with a constructors</b>
/// </para>
/// <para>
/// As a alternative to mapping exceptions with factory methods, you can also map the exception
/// in the constructor of the object that should be used to represent the error in the schema.
/// </para>
/// <code>
/// public class MyCustomError
/// {
///     public MyCustomError(MyCustomDomainException exception)
///     {
///         Message = exception.Message;
///     }
///
///     public MyCustomError(MyCustomDomainException2 exception)
///     {
///         Message = exception.Message;
///     }
///
///     public string Message { get; }
/// }
///
/// public class Mutation
/// {
///     [Error(typeof(MyCustomError))]
///     public CreateUserPayload CreateUser(CreateUserInput input)
///     {
///        // ...
///     }
/// }
/// </code>
/// </item>
/// </list>
/// </example>
public class ErrorAttribute : ObjectFieldDescriptorAttribute
{
    /// <inheritdoc cref="ErrorAttribute"/>
    /// <param name="errorType">
    /// The type of the exception, the class with factory methods or the error with an exception
    /// as the argument. See the examples in <see cref="ErrorAttribute"/>.
    /// </param>
    public ErrorAttribute(Type errorType)
    {
        ErrorType = errorType;
    }

    /// <summary>
    /// The type of the exception, the class with factory methods or the error with an exception
    /// as the argument. See the examples in <see cref="ErrorAttribute"/>.
    /// </summary>
    public Type ErrorType { get; }

    /// <inheritdoc />
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
        => descriptor.Error(ErrorType);
}

#if NET6_0_OR_GREATER
/// <summary>
/// The <see cref="ErrorAttribute{T}"/> registers a middleware that will catch all exceptions of
/// type <see cref="ErrorAttribute.ErrorType"/> on mutations.
///
/// By annotating the attribute the response type
/// of the annotated mutation resolver, will be automatically extended by a field of type
/// <c>errors:[Error!]</c>. This field will return errors that are caught by the middleware.
/// All the other fields on this type will be rewritten to nullable types. In case of a error
/// these fields will be set to null.
/// <para>
/// There are three different ways to map exceptions to GraphQL errors.
/// </para>
/// </summary>
/// <remarks>
/// The idea of the error middleware is to keep the resolver clean of any error handling code
/// and use exceptions to signal a error state. The HotChocolate schema is automatically
/// rewritten into a common error handling pattern.
/// <a href="https://xuorig.medium.com/a-guide-to-graphql-errors-bb9ba9f15f85">Learn More</a>
/// </remarks>
/// <example>
/// <para>
/// There are three different ways to map exceptions to GraphQL errors.
/// </para>
/// <list type="number">
/// <item>
/// <para>
/// <b>Catching exceptions directly</b>
/// </para>
/// If <see cref="ErrorAttribute.ErrorType"/> is a exception, the exception is automatically
/// mapped into a GraphQL error and the middleware will catch this exception
/// <code>
/// <![CDATA[
/// public class Mutation
/// {
///     [Error<SomeSpecificDomainError>]
///     [Error<SomeOtherError>]
///     public CreateUserPayload CreateUser(CreateUserInput input)
///     {
///        // ...
///     }
/// }
///
/// public record CreateUserInput(string UserName);
///
/// public record CreateUserPayload(User User);
/// ]]>
/// </code>
/// This will generate the following schema
/// <code>
/// <![CDATA[
/// type Mutation {
///   createUser(input: CreateUserInput!): CreateUserPayload!
/// }
///
/// input CreateUserInput {
///   userName: String!
/// }
///
/// type CreateUserPayload {
///   user: User
///   errors: [CreateUserError!]
/// }
///
/// type User {
///   username: String
/// }
///
/// interface Error {
///   message: String!
/// }
///
/// type SomeSpecificDomainError implements Error {
///   message: String!
/// }
///
/// type SomeOtherDomainError implements Error {
///   message: String!
/// }
///
/// union CreateUserError = SomeSpecificDomainError | SomeOtherDomainError
/// ]]>
/// </code>
/// </item>
/// <item>
/// <para>
/// <b>Map Exceptions with a factory method</b>
/// </para>
/// <para>
/// If there should be any translation between exception and error, you can defined a class
/// with factory methods. These factory methods receive a <see cref="Exception"/> and return
/// a object which will be used as the representation of the error
/// </para>
/// <para>
/// A factory method has to be `public static` and the name of the method has to be
/// `CreateErrorFrom`. There should only be one parameter of type <see cref="Exception"/> and
/// it can return a arbitrary class/struct/record that will be used as the representation
/// of the error.
/// </para>
/// <code>
/// <![CDATA[
/// public class MyCustomError
/// {
///     public static MyCustomError CreateErrorFrom(DomainExceptionA ex)
///     {
///         return new MyCustomError();
///     }
///
///     public static MyCustomError CreateErrorFrom(DomainExceptionB ex)
///     {
///         return new MyCustomError();
///     }
///
///     public string Message => "My custom error Message";
/// }
///
/// public class Mutation
/// {
///     [Error<MyCustomError>]
///     public CreateUserPayload CreateUser(CreateUserInput input)
///     {
///        // ...
///     }
/// }
///
/// public record CreateUserInput(string UserName);
///
/// public record CreateUserPayload(User User);
/// ]]>
/// </code>
/// </item>
/// <item>
/// <para>
/// <b>Map exceptions with a constructors</b>
/// </para>
/// <para>
/// As a alternative to mapping exceptions with factory methods, you can also map the exception
/// in the constructor of the object that should be used to represent the error in the schema.
/// </para>
/// <code>
/// <![CDATA[
/// public class MyCustomError
/// {
///     public MyCustomError(MyCustomDomainException exception)
///     {
///         Message = exception.Message;
///     }
///
///     public MyCustomError(MyCustomDomainException2 exception)
///     {
///         Message = exception.Message;
///     }
///
///     public string Message { get; }
/// }
///
/// public class Mutation
/// {
///     [Error<MyCustomError>]
///     public CreateUserPayload CreateUser(CreateUserInput input)
///     {
///        // ...
///     }
/// }
/// ]]>
/// </code>
/// </item>
/// </list>
/// </example>
public sealed class ErrorAttribute<TError> : ErrorAttribute
{
    /// <inheritdoc cref="ErrorAttribute"/>
    public ErrorAttribute() : base(typeof(TError))
    {
    }
}
#endif
