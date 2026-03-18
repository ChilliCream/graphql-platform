---
title: "Error Handling"
---

GraphQL APIs produce two kinds of errors. **Request errors** occur when something goes wrong during execution, such as an unhandled exception in a resolver. **Domain errors** represent business logic rejections, such as a username already being taken or an invalid input value. Hot Chocolate handles both, with different mechanisms for each.

Request errors appear in the top-level `errors` array of the GraphQL response. Domain errors, when using mutation conventions, appear as typed error objects on the mutation payload. This guide covers both patterns in depth.

# Request Errors

When a resolver throws an unhandled exception, Hot Chocolate catches it and does two things: the field returns `null`, and an error entry appears in the `errors` array of the response.

By default, exception details are hidden in production. Instead of exposing the original exception message, the response contains a generic `"Unexpected Execution Error"` message. This prevents leaking internal implementation details to clients.

```json
{
  "data": {
    "userById": null
  },
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "locations": [{ "line": 2, "column": 3 }],
      "path": ["userById"]
    }
  ]
}
```

During development, if a debugger is attached, Hot Chocolate includes the original exception message and stack trace. You can also enable this behavior explicitly:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = true);
```

> **Warning:** Do not enable `IncludeExceptionDetails` in production. Exception messages and stack traces can expose sensitive information about your application internals.

# Error Filters

An error filter lets you intercept every error before it reaches the client. Use error filters to log the original exception, sanitize the error message, or add error codes.

Register an error filter with `AddErrorFilter`. The filter receives an `IError` and must return an `IError`. You can modify the error using its `With*` methods, which return a new `IError` instance with the changed property.

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddErrorFilter(error =>
    {
        if (error.Exception is not null)
        {
            // Log the original exception for debugging
            Console.Error.WriteLine(error.Exception);

            return error
                .WithMessage("An internal error occurred. Please try again later.")
                .WithCode("INTERNAL_ERROR");
        }

        return error;
    });
```

For more complex scenarios, implement the `IErrorFilter` interface as a class. This lets you inject services such as a logger.

```csharp
// Infrastructure/LoggingErrorFilter.cs
public class LoggingErrorFilter : IErrorFilter
{
    private readonly ILogger<LoggingErrorFilter> _logger;

    public LoggingErrorFilter(ILogger<LoggingErrorFilter> logger)
    {
        _logger = logger;
    }

    public IError OnError(IError error)
    {
        if (error.Exception is not null)
        {
            _logger.LogError(error.Exception, "Unhandled exception in resolver.");

            return error
                .WithMessage("An internal error occurred.")
                .WithCode("INTERNAL_ERROR")
                .WithException(null); // strip the exception from the error
        }

        return error;
    }
}
```

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddErrorFilter<LoggingErrorFilter>();
```

Multiple error filters can be registered. They run in the order they are added, and each filter receives the output of the previous one.

# Error Codes

The `IError` interface supports a `Code` property, which appears under `extensions.code` in the GraphQL response. Error codes let clients handle specific error conditions programmatically without parsing messages.

```json
{
  "errors": [
    {
      "message": "An internal error occurred.",
      "locations": [{ "line": 2, "column": 3 }],
      "path": ["userById"],
      "extensions": {
        "code": "INTERNAL_ERROR"
      }
    }
  ]
}
```

Set error codes in an error filter using `WithCode`, or build errors with codes from scratch using `ErrorBuilder`:

```csharp
var error = ErrorBuilder.New()
    .SetMessage("Rate limit exceeded.")
    .SetCode("RATE_LIMITED")
    .Build();
```

# Domain Errors with Mutation Conventions

Domain errors are the primary mechanism for communicating business logic failures to clients. When mutation conventions are enabled, you annotate mutations with `[Error]` attributes. Hot Chocolate catches the declared exception types and maps them to typed error objects on the mutation payload.

This keeps domain errors separate from request errors: they appear on the payload, not in the top-level `errors` array, and clients can query them with specific fields and types.

For mutation conventions setup, see [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations#mutation-conventions).

## Map Exceptions Directly

The most straightforward approach is to annotate the mutation with the exception type. The exception's `Message` property becomes the error message.

<ExampleTabs>
<Implementation>

```csharp
// Exceptions/UserNameTakenException.cs
public class UserNameTakenException : Exception
{
    public UserNameTakenException(string username)
        : base($"The username '{username}' is already taken.")
    {
        Username = username;
    }

    public string Username { get; }
}
```

```csharp
// Types/UserMutations.cs
[MutationType]
public static partial class UserMutations
{
    [Error(typeof(UserNameTakenException))]
    [Error(typeof(InvalidUserNameException))]
    public static async Task<User?> UpdateUserNameAsync(
        [ID] Guid userId,
        string username,
        UserService users,
        CancellationToken ct)
        => await users.UpdateNameAsync(userId, username, ct);
}
```

</Implementation>
<Code>

```csharp
// Types/UserMutationsType.cs
public class UserMutationsType : ObjectType<UserMutations>
{
    protected override void Configure(
        IObjectTypeDescriptor<UserMutations> descriptor)
    {
        descriptor
            .Field(f => f.UpdateUserNameAsync(
                default, default!, default!, default))
            .Argument("userId", a => a.ID())
            .Error<UserNameTakenException>()
            .Error<InvalidUserNameException>();
    }
}
```

</Code>
</ExampleTabs>

Hot Chocolate rewrites the exception class name for the schema: `UserNameTakenException` becomes `UserNameTakenError`. The generated schema looks like this:

```graphql
type UpdateUserNamePayload {
  user: User
  errors: [UpdateUserNameError!]
}

union UpdateUserNameError = UserNameTakenError | InvalidUserNameError

type UserNameTakenError implements Error {
  message: String!
}

interface Error {
  message: String!
}
```

## Map with a Factory Method

When you need control over the error shape, or want to hide internal details from the exception, create a dedicated error class with a static `CreateErrorFrom` method. Hot Chocolate discovers this method by convention.

```csharp
// Errors/UserNameTakenError.cs
public class UserNameTakenError
{
    private UserNameTakenError(string message, string username)
    {
        Message = message;
        Username = username;
    }

    public string Message { get; }

    public string Username { get; }

    public static UserNameTakenError CreateErrorFrom(UserNameTakenException ex)
        => new($"The username '{ex.Username}' is already taken.", ex.Username);
}
```

Then reference the error class instead of the exception:

```csharp
// Types/UserMutations.cs
[MutationType]
public static partial class UserMutations
{
    [Error(typeof(UserNameTakenError))]
    public static async Task<User?> UpdateUserNameAsync(
        [ID] Guid userId,
        string username,
        UserService users,
        CancellationToken ct)
        => await users.UpdateNameAsync(userId, username, ct);
}
```

A single error class can handle multiple exception types by defining multiple `CreateErrorFrom` overloads:

```csharp
// Errors/UserValidationError.cs
public class UserValidationError
{
    private UserValidationError(string message) => Message = message;

    public string Message { get; }

    public static UserValidationError CreateErrorFrom(UserNameTakenException ex)
        => new($"The username '{ex.Username}' is already taken.");

    public static UserValidationError CreateErrorFrom(InvalidUserNameException ex)
        => new($"The username is invalid: {ex.Reason}");
}
```

## Map with a Constructor

Alternatively, give the error class a constructor that accepts the exception.

```csharp
// Errors/UserNameTakenError.cs
public class UserNameTakenError
{
    public UserNameTakenError(UserNameTakenException ex)
    {
        Message = $"The username '{ex.Username}' is already taken.";
        Username = ex.Username;
    }

    public string Message { get; }

    public string Username { get; }
}
```

## Factory with Dependency Injection

For error factories that need access to services (such as a localizer or a logger), implement the `IPayloadErrorFactory<TException, TError>` interface. Hot Chocolate resolves the factory from the DI container.

```csharp
// Errors/UserNameTakenErrorFactory.cs
public class UserNameTakenErrorFactory
    : IPayloadErrorFactory<UserNameTakenException, UserNameTakenError>
{
    private readonly IStringLocalizer<UserErrors> _localizer;

    public UserNameTakenErrorFactory(IStringLocalizer<UserErrors> localizer)
    {
        _localizer = localizer;
    }

    public UserNameTakenError CreateErrorFrom(UserNameTakenException exception)
        => new(_localizer["UserNameTaken", exception.Username]);
}
```

Register the factory in the DI container:

```csharp
// Program.cs
builder.Services
    .AddSingleton<IPayloadErrorFactory<UserNameTakenException, UserNameTakenError>,
        UserNameTakenErrorFactory>();
```

## Returning Multiple Errors

A mutation can return multiple domain errors at once by throwing an `AggregateException`. Hot Chocolate unwraps it and maps each inner exception to its corresponding error type.

```csharp
// Services/UserService.cs
public async Task<User> UpdateNameAsync(
    Guid userId, string username, CancellationToken ct)
{
    var errors = new List<Exception>();

    if (username.Length < 3)
        errors.Add(new InvalidUserNameException("Must be at least 3 characters."));

    if (await IsUserNameTakenAsync(username, ct))
        errors.Add(new UserNameTakenException(username));

    if (errors.Count > 0)
        throw new AggregateException(errors);

    // ... proceed with update
}
```

## Sharing Errors Across Mutations

Error classes and error factories are not tied to a specific mutation. You can reuse the same `[Error(typeof(...))]` annotation across multiple mutation methods. This keeps your error types consistent and avoids duplication.

```csharp
[MutationType]
public static partial class UserMutations
{
    [Error(typeof(UserNameTakenError))]
    public static async Task<User?> UpdateUserNameAsync(/* ... */) { /* ... */ }

    [Error(typeof(UserNameTakenError))]
    public static async Task<User?> CreateUserAsync(/* ... */) { /* ... */ }
}
```

# Custom Error Interface

By default, mutation convention errors implement an `Error` interface with a single `message` field. You can replace this interface to require additional fields such as `code`.

<ExampleTabs>
<Implementation>

```csharp
// Types/IUserError.cs
[GraphQLName("UserError")]
public interface IUserError
{
    string Message { get; }
    string Code { get; }
}
```

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddMutationConventions(applyToAllMutations: true)
    .AddErrorInterfaceType<IUserError>();
```

</Implementation>
<Code>

```csharp
// Types/CustomErrorInterfaceType.cs
public class CustomErrorInterfaceType : InterfaceType
{
    protected override void Configure(IInterfaceTypeDescriptor descriptor)
    {
        descriptor.Name("UserError");
        descriptor.Field("message").Type<NonNullType<StringType>>();
        descriptor.Field("code").Type<NonNullType<StringType>>();
    }
}
```

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddMutationConventions(applyToAllMutations: true)
    .AddErrorInterfaceType<CustomErrorInterfaceType>();
```

</Code>
</ExampleTabs>

All error types must declare every field required by the interface. They do not need to implement the C# interface, but they must have matching properties.

```csharp
// Errors/UserNameTakenError.cs
public class UserNameTakenError
{
    public UserNameTakenError(UserNameTakenException ex)
    {
        Message = $"The username '{ex.Username}' is already taken.";
        Code = "USERNAME_TAKEN";
    }

    public string Message { get; }
    public string Code { get; }
}
```

The generated schema now requires both fields on every error type:

```graphql
interface UserError {
  message: String!
  code: String!
}

type UserNameTakenError implements UserError {
  message: String!
  code: String!
}
```

# Errors Outside Mutations

Query and subscription resolvers do not use mutation conventions, so domain errors work differently. You have several options.

## Report an Error and Return Null

Use `ReportError` on `IResolverContext` to add an error to the response while still returning data (or `null`) from the resolver. The error appears in the top-level `errors` array.

```csharp
// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    public static User? GetUserByEmail(
        string email,
        UserService users,
        IResolverContext context)
    {
        var user = users.FindByEmail(email);

        if (user is null)
        {
            context.ReportError(
                ErrorBuilder.New()
                    .SetMessage($"No user found with email '{email}'.")
                    .SetCode("USER_NOT_FOUND")
                    .Build());
            return null;
        }

        return user;
    }
}
```

`ReportError` has three overloads:

- `ReportError(string errorMessage)` for quick error messages.
- `ReportError(IError error)` for fully constructed error objects.
- `ReportError(Exception exception, Action<ErrorBuilder>? configure)` for reporting caught exceptions with optional customization.

## Use a Result Union

For queries where you need typed error handling similar to mutation conventions, return a union type. The client can then use inline fragments to handle each case.

<ExampleTabs>
<Implementation>

```csharp
// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    public static IUserByEmailResult GetUserByEmail(
        string email,
        UserService users)
    {
        var user = users.FindByEmail(email);

        if (user is null)
            return new UserNotFoundError($"No user found with email '{email}'.");

        return user;
    }
}
```

```csharp
// Types/UserNotFoundError.cs
public record UserNotFoundError(string Message);
```

```csharp
// Types/IUserByEmailResult.cs
[UnionType("UserByEmailResult")]
public interface IUserByEmailResult;

// Make User and UserNotFoundError implement the interface
public partial class User : IUserByEmailResult { }
public partial record UserNotFoundError : IUserByEmailResult;
```

</Implementation>
<Code>

```csharp
// Types/UserByEmailResultType.cs
public class UserByEmailResultType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Name("UserByEmailResult");
        descriptor.Type<UserType>();
        descriptor.Type<UserNotFoundErrorType>();
    }
}
```

</Code>
</ExampleTabs>

# Troubleshooting

## Exception messages not showing in responses

By default, Hot Chocolate replaces exception messages with `"Unexpected Execution Error"` when no debugger is attached. This is a security measure. To see the original messages during development, enable `IncludeExceptionDetails`:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = true);
```

In production, use an [error filter](#error-filters) to log the original exception and return a safe message to the client.

## Error types not appearing in the schema

Verify that mutation conventions are enabled. Domain errors on payloads require mutation conventions to rewrite the schema.

```csharp
builder.Services
    .AddGraphQLServer()
    .AddMutationConventions(applyToAllMutations: true);
```

Also check that the `[Error(typeof(...))]` attribute is on the mutation method, not on the class.

## AggregateException not unwrapping into multiple errors

Hot Chocolate unwraps `AggregateException` automatically, but only for exception types declared with `[Error]` on the mutation. If an inner exception type is not declared, it is treated as a request error and appears in the top-level `errors` array instead.

Make sure every exception type inside the `AggregateException` has a corresponding `[Error]` attribute on the mutation.

## Error class missing required interface fields

If you have a custom error interface (e.g., one that requires both `message` and `code`), every error class must expose matching properties. If a property is missing, schema generation fails. Check that each error class has all the properties defined by the interface.

# Next Steps

- **Need mutation conventions?** See [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations) for the full pattern including inputs, payloads, and naming customization.
- **Need to build a schema?** See [Schema Basics](/docs/hotchocolate/v16/building-a-schema/schema-basics) for an overview of how types, queries, and mutations fit together.
- **Need to fetch data?** See [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader) for efficient data fetching patterns.
