namespace HotChocolate.Types;

/// <summary>
/// Provides extensions to the <see cref="IObjectFieldDescriptor"/> for the mutation convention.
/// </summary>
public static class MutationObjectFieldDescriptorExtensions
{
    /// <summary>
    /// The UseMutationConvention allows to override the global mutation convention settings
    /// on a per field basis.
    /// </summary>
    /// <param name="descriptor">The descriptor of the field</param>
    /// <param name="options">
    /// The options that shall override the global mutation convention options.
    /// </param>
    /// <returns>
    /// Returns the <paramref name="descriptor"/> for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectFieldDescriptor UseMutationConvention(
        this IObjectFieldDescriptor descriptor,
        MutationFieldOptions options = default)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        descriptor.Extend().OnBeforeNaming((c, d) =>
        {
            c.ContextData
                .GetMutationFields()
                .Add(new(d,
                    options.InputTypeName,
                    options.InputArgumentName,
                    options.PayloadTypeName,
                    options.PayloadFieldName,
                    options.PayloadErrorTypeName,
                    options.PayloadErrorsFieldName,
                    !options.Disable));
        });

        return descriptor;
    }

    /// <summary>
    /// The <c>.Error&lt;TError>()</c> extension method registers a middleware that will catch
    /// all exceptions of type <typeparamref name="TError"/> on mutations.
    ///
    /// By applying the error extension to a mutation field the
    /// response type of the annotated resolver, will be automatically extended by a field of
    /// type <c>errors:[Error!]</c>. This field will return errors that are caught by the
    /// middleware. All the other fields on this type will be rewritten to nullable types.
    /// In case of a error these fields will be set to null.
    /// <para>
    /// There are three different ways to map exceptions to GraphQL errors.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The idea of the error middleware is to keep the resolver clean of any error handling
    /// code and use exceptions to signal a error state. The HotChocolate schema is
    /// automatically rewritten into a common error handling pattern.
    /// <a href="https://xuorig.medium.com/a-guide-to-graphql-errors-bb9ba9f15f85">
    /// Learn More
    /// </a>
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
    /// If <typeparamref name="TError"/> is a exception, the exception is automatically
    /// mapped into a GraphQL error and the middleware will catch this exception
    /// <code>
    /// public class Mutation
    /// {
    ///     public CreateUserPayload CreateUser(CreateUserInput input)
    ///     {
    ///        // ...
    ///     }
    /// }
    /// public class MutationType : ObjectType&lt;Mutation>
    /// {
    ///     protected override Configure(IObjectTypeDescriptor&lt;Mutation> descriptor)
    ///     {
    ///         descriptor
    ///            .Field(x =>; x.CreateUserAsync(default)
    ///            <b>.Error&lt;SomeSpecificDomainError>()</b>
    ///            <b>.Error&lt;SomeOtherError>();</b>
    ///     }
    /// }
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
    /// `CreateErrorFrom`. There should only be one parameter of type <see cref="Exception"/>
    /// and it can return a arbitrary class/struct/record that will be used as the
    /// representation of the error.
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
    /// public class MutationType : ObjectType&lt;Mutation>
    /// {
    ///     protected override Configure(IObjectTypeDescriptor&lt;Mutation> descriptor)
    ///     {
    ///         descriptor
    ///            .Field(x =>; x.CreateUserAsync(default)
    ///            <b>.Error&lt;MyCustomError>();</b>
    ///     }
    /// }
    /// </code>
    /// </item>
    /// <item>
    /// <para>
    /// <b>Map exceptions with a constructors</b>
    /// </para>
    /// <para>
    /// As a alternative to mapping exceptions with factory methods, you can also map the
    /// exception in the constructor of the object that should be used to represent the
    /// error in the schema.
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
    /// public class MutationType : ObjectType&lt;Mutation>
    /// {
    ///     protected override Configure(IObjectTypeDescriptor&lt;Mutation> descriptor)
    ///     {
    ///         descriptor
    ///            .Field(x =>; x.CreateUserAsync(default)
    ///            <b>.Error&lt;MyCustomError>();</b>
    ///     }
    /// }
    /// </code>
    /// </item>
    /// </list>
    /// </example>
    /// <param name="descriptor">The descriptor of the field</param>
    /// <typeparam name="TError">
    /// The type of the exception, the class with factory methods or the error with an exception
    /// as the argument. See the examples in <see cref="Error{TError}"/>.
    /// </typeparam>
    public static IObjectFieldDescriptor Error<TError>(this IObjectFieldDescriptor descriptor) =>
        Error(descriptor, typeof(TError));

    /// <summary>
    /// The <c>.Error&lt;TError>()</c> extension method registers a middleware that will catch
    /// all exceptions of type <typeparamref name="TError"/> on mutations.
    ///
    /// By applying the error extension to a mutation field the
    /// response type of the annotated resolver, will be automatically extended by a field of
    /// type <c>errors:[Error!]</c>. This field will return errors that are caught by the
    /// middleware. All the other fields on this type will be rewritten to nullable types.
    /// In case of a error these fields will be set to null.
    /// <para>
    /// There are three different ways to map exceptions to GraphQL errors.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The idea of the error middleware is to keep the resolver clean of any error handling
    /// code and use exceptions to signal a error state. The HotChocolate schema is
    /// automatically rewritten into a common error handling pattern.
    /// <a href="https://xuorig.medium.com/a-guide-to-graphql-errors-bb9ba9f15f85">
    /// Learn More
    /// </a>
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
    /// If <paramref name="errorType"/> is a exception, the exception is automatically
    /// mapped into a GraphQL error and the middleware will catch this exception
    /// <code>
    /// public class Mutation
    /// {
    ///     public CreateUserPayload CreateUser(CreateUserInput input)
    ///     {
    ///        // ...
    ///     }
    /// }
    /// public class MutationType : ObjectType&lt;Mutation>
    /// {
    ///     protected override Configure(IObjectTypeDescriptor&lt;Mutation> descriptor)
    ///     {
    ///         descriptor
    ///            .Field(x =>; x.CreateUserAsync(default)
    ///            <b>.Error(typeof(SomeSpecificDomainError))</b>
    ///            <b>.Error(typeof(SomeOtherError));</b>
    ///     }
    /// }
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
    /// public class MutationType : ObjectType&lt;Mutation>
    /// {
    ///     protected override Configure(IObjectTypeDescriptor&lt;Mutation> descriptor)
    ///     {
    ///         descriptor
    ///            .Field(x =>; x.CreateUserAsync(default)
    ///            <b>.Error(typeof(MyCustomError));</b>
    ///     }
    /// }
    /// </code>
    /// </item>
    /// <item>
    /// <para>
    /// <b>Map exceptions with a constructors</b>
    /// </para>
    /// <para>
    /// As a alternative to mapping exceptions with factory methods, you can also map the
    /// exception in the constructor of the object that should be used to represent the
    /// error in the schema.
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
    /// public class MutationType : ObjectType&lt;Mutation>
    /// {
    ///     protected override Configure(IObjectTypeDescriptor&lt;Mutation> descriptor)
    ///     {
    ///         descriptor
    ///            .Field(x =>; x.CreateUserAsync(default)
    ///            <b>.Error(typeof(MyCustomError));</b>
    ///     }
    /// }
    /// </code>
    /// </item>
    /// </list>
    /// </example>
    /// <param name="descriptor">The descriptor of the field</param>
    /// <param name="errorType">
    /// The type of the exception, the class with factory methods or the error with an exception
    /// as the argument. See the examples in <see cref="Error"/>.
    /// </param>
    public static IObjectFieldDescriptor Error(
        this IObjectFieldDescriptor descriptor,
        Type errorType)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (errorType is null)
        {
            throw new ArgumentNullException(nameof(errorType));
        }

        descriptor.Extend().OnBeforeCreate((ctx, d) => d.AddErrorType(ctx, errorType));

        return descriptor;
    }
}
