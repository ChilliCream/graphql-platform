using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices;

internal static partial class BestPracticeData
{
    private static void AddErrorHandlingDocuments(List<BestPracticeDocument> docs)
    {
        docs.Add(
            new BestPracticeDocument
            {
                Id = "error-handling-error-filters",
                Title = "Error Filters and Exception Handling",
                Category = BestPracticeCategory.ErrorHandling,
                Tags = ["hot-chocolate-16"],
                Styles = ["all"],
                Keywords = "exception error catch handle try throw filter format translate log crash failure",
                Abstract =
                    "How to implement IErrorFilter to translate exceptions into structured GraphQL errors, add error codes, and strip internal details from production errors.",
                Body = """
                # Error Filters and Exception Handling

                ## When to Use

                Use error filters when you need to intercept unhandled exceptions and translate them into structured GraphQL errors. Error filters are the last line of defense before errors reach the client, and they serve two purposes:

                1. **Security**: Strip internal details (stack traces, connection strings) from production errors
                2. **Structure**: Add error codes, extensions, and consistent formatting to error responses

                Error filters handle unexpected exceptions that are not covered by mutation conventions. For expected domain errors in mutations, prefer mutation conventions with `[Error<T>]`.

                ## Implementation

                ### Basic Error Filter

                ```csharp
                namespace MyApp.GraphQL;

                public class GraphQLErrorFilter : IErrorFilter
                {
                    private readonly ILogger<GraphQLErrorFilter> _logger;
                    private readonly IHostEnvironment _environment;

                    public GraphQLErrorFilter(
                        ILogger<GraphQLErrorFilter> logger,
                        IHostEnvironment environment)
                    {
                        _logger = logger;
                        _environment = environment;
                    }

                    public IError OnError(IError error)
                    {
                        if (error.Exception is not null)
                        {
                            _logger.LogError(error.Exception, "Unhandled GraphQL error");

                            if (_environment.IsProduction())
                            {
                                return error
                                    .WithMessage("An unexpected error occurred.")
                                    .WithCode("INTERNAL_ERROR")
                                    .RemoveException();
                            }

                            return error
                                .WithCode("INTERNAL_ERROR")
                                .WithExtensions(new Dictionary<string, object?>
                                {
                                    ["stackTrace"] = error.Exception.StackTrace
                                });
                        }

                        return error;
                    }
                }
                ```

                ### Register the Error Filter

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes()
                    .AddErrorFilter<GraphQLErrorFilter>();
                ```

                ### Mapping Specific Exceptions to Error Codes

                ```csharp
                public class GraphQLErrorFilter : IErrorFilter
                {
                    public IError OnError(IError error)
                    {
                        return error.Exception switch
                        {
                            UnauthorizedAccessException =>
                                error
                                    .WithMessage("You are not authorized to perform this action.")
                                    .WithCode("UNAUTHORIZED")
                                    .RemoveException(),

                            EntityNotFoundException ex =>
                                error
                                    .WithMessage(ex.Message)
                                    .WithCode("NOT_FOUND")
                                    .RemoveException(),

                            ConcurrencyException =>
                                error
                                    .WithMessage("The resource was modified by another request. Please retry.")
                                    .WithCode("CONFLICT")
                                    .RemoveException(),

                            ValidationException ex =>
                                error
                                    .WithMessage(ex.Message)
                                    .WithCode("VALIDATION_ERROR")
                                    .WithExtensions(new Dictionary<string, object?>
                                    {
                                        ["field"] = ex.FieldName
                                    })
                                    .RemoveException(),

                            _ when error.Exception is not null =>
                                error
                                    .WithMessage("An unexpected error occurred.")
                                    .WithCode("INTERNAL_ERROR")
                                    .RemoveException(),

                            _ => error
                        };
                    }
                }
                ```

                ### Multiple Error Filters

                You can register multiple error filters. They are applied in registration order:

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddErrorFilter<SecurityErrorFilter>()   // Runs first
                    .AddErrorFilter<DomainErrorFilter>()     // Runs second
                    .AddErrorFilter<FallbackErrorFilter>();   // Runs last
                ```

                ## Anti-patterns

                **Leaking internal details in production:**

                ```csharp
                // BAD: Stack traces and exception details reach the client
                public IError OnError(IError error)
                {
                    return error; // Passes everything through, including stack traces
                }
                ```

                **Swallowing errors silently:**

                ```csharp
                // BAD: Replacing all errors with a generic message hides bugs
                public IError OnError(IError error)
                {
                    return ErrorBuilder.New()
                        .SetMessage("Something went wrong.")
                        .Build(); // Loses the original error code, path, and location
                }
                ```

                **Using error filters for expected domain errors:**

                ```csharp
                // BAD: Expected errors should use mutation conventions, not error filters
                public IError OnError(IError error)
                {
                    if (error.Exception is UserNotFoundException ex)
                    {
                        return error.WithMessage($"User {ex.UserId} not found.");
                    }
                    return error;
                }
                // Use [Error<UserNotFoundError>] on the mutation instead
                ```

                ## Key Points

                - Register error filters with `AddErrorFilter<T>()` on the GraphQL server builder
                - Always strip internal details (stack traces, connection strings) in production
                - Map known exception types to structured error codes for client consumption
                - Use `RemoveException()` to prevent exception details from reaching the client
                - Error filters run for unhandled exceptions — use mutation conventions for expected domain errors
                - Log the original exception before stripping details so you can diagnose issues

                ## Related Practices

                - [error-handling-mutation-conventions] — For typed mutation errors
                - [error-handling-problem-details] — For ProblemDetails integration
                - [security-production-hardening] — For production security settings
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "error-handling-mutation-conventions",
                Title = "Typed Errors with Mutation Conventions",
                Category = BestPracticeCategory.ErrorHandling,
                Tags = ["hot-chocolate-16", "code-first"],
                Styles = ["all"],
                Keywords = "mutation error result domain validation typed errors payload union failure response",
                Abstract =
                    "How to use AddMutationConventions() with [Error<T>] to define typed error union types on mutations, giving clients exhaustive error information.",
                Body = """
                # Typed Errors with Mutation Conventions

                ## When to Use

                Use mutation conventions whenever you write mutations that can fail in expected, domain-specific ways. Mutation conventions transform thrown exceptions into typed GraphQL error union types, giving clients exhaustive knowledge of all possible error states.

                This replaces the traditional approach of returning errors in the `errors` array, where clients have no type-safe way to distinguish between error kinds. With mutation conventions, the mutation return type becomes a union of the success payload and all declared error types.

                ## Implementation

                ### Enable Mutation Conventions

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddMutationType()
                    .AddTypes()
                    .AddMutationConventions();
                ```

                ### Define Error Types

                Error types are plain classes. Their public properties become fields on the GraphQL error type:

                ```csharp
                namespace MyApp.GraphQL.Errors;

                public class UserNotFoundError
                {
                    public UserNotFoundError(int userId)
                    {
                        Message = $"User with ID {userId} was not found.";
                    }

                    public string Message { get; }
                }

                public class EmailAlreadyInUseError
                {
                    public EmailAlreadyInUseError(string email)
                    {
                        Message = $"The email '{email}' is already in use.";
                    }

                    public string Message { get; }
                }

                public class ValidationError
                {
                    public ValidationError(string field, string message)
                    {
                        Field = field;
                        Message = message;
                    }

                    public string Field { get; }
                    public string Message { get; }
                }
                ```

                ### Declare Errors on Mutations

                Use `[Error<T>]` to declare which errors a mutation can produce:

                ```csharp
                [MutationType]
                public static class UserMutations
                {
                    [Error<UserNotFoundError>]
                    [Error<EmailAlreadyInUseError>]
                    [Error<ValidationError>]
                    public static async Task<User> UpdateUserEmailAsync(
                        UpdateUserEmailInput input,
                        AppDbContext dbContext,
                        CancellationToken cancellationToken)
                    {
                        if (string.IsNullOrWhiteSpace(input.NewEmail))
                        {
                            throw new ValidationError("email", "Email cannot be empty.");
                        }

                        var user = await dbContext.Users.FindAsync(input.UserId);
                        if (user is null)
                        {
                            throw new UserNotFoundError(input.UserId);
                        }

                        var emailTaken = await dbContext.Users
                            .AnyAsync(u => u.Email == input.NewEmail, cancellationToken);
                        if (emailTaken)
                        {
                            throw new EmailAlreadyInUseError(input.NewEmail);
                        }

                        user.Email = input.NewEmail;
                        await dbContext.SaveChangesAsync(cancellationToken);
                        return user;
                    }
                }
                ```

                ### Generated Schema

                The mutation conventions produce this schema:

                ```graphql
                type Mutation {
                  updateUserEmail(input: UpdateUserEmailInput!): UpdateUserEmailPayload!
                }

                union UpdateUserEmailError =
                    UserNotFoundError
                  | EmailAlreadyInUseError
                  | ValidationError

                type UpdateUserEmailPayload {
                  user: User
                  errors: [UpdateUserEmailError!]
                }
                ```

                ### Client Query Pattern

                ```graphql
                mutation {
                  updateUserEmail(input: { userId: 1, newEmail: "new@example.com" }) {
                    user {
                      id
                      email
                    }
                    errors {
                      ... on UserNotFoundError {
                        message
                      }
                      ... on EmailAlreadyInUseError {
                        message
                      }
                      ... on ValidationError {
                        field
                        message
                      }
                    }
                  }
                }
                ```

                ## Anti-patterns

                **Returning errors in the generic errors array:**

                ```csharp
                // BAD: Clients have no type-safe way to handle specific errors
                [MutationType]
                public static class UserMutations
                {
                    public static async Task<User> UpdateUserEmail(
                        UpdateUserEmailInput input,
                        AppDbContext dbContext)
                    {
                        var user = await dbContext.Users.FindAsync(input.UserId);
                        if (user is null)
                        {
                            throw new GraphQLException("User not found"); // Untyped error
                        }
                        // ...
                    }
                }
                ```

                **Using a single generic error type for everything:**

                ```csharp
                // BAD: One error type loses the benefit of exhaustive error matching
                [Error<GenericError>]
                public static async Task<User> UpdateUserEmail(...)
                {
                    // Clients cannot distinguish between different failure modes
                }
                ```

                ## Key Points

                - Call `AddMutationConventions()` on the server builder to enable typed errors
                - Use `[Error<T>]` to declare each error type a mutation can throw
                - Error types are plain classes — their public properties become GraphQL fields
                - Throw error type instances as exceptions from mutation resolvers
                - The framework wraps the return type in a payload with `data` and `errors` fields
                - Clients get exhaustive union types for type-safe error handling

                ## Related Practices

                - [error-handling-error-filters] — For exception-to-error translation
                - [defining-types-union] — For union type patterns
                - [defining-types-input] — For mutation input types
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "error-handling-problem-details",
                Title = "Problem Details Integration",
                Category = BestPracticeCategory.ErrorHandling,
                Tags = ["hot-chocolate-16"],
                Styles = ["all"],
                Keywords = "problem details RFC 9457 HTTP errors API error response REST consistent format",
                Abstract =
                    "How to integrate ASP.NET Core ProblemDetails with Hot Chocolate error handling for consistent HTTP and GraphQL error responses.",
                Body = """
                # Problem Details Integration

                ## When to Use

                Use ProblemDetails integration when your application exposes both REST and GraphQL endpoints and you want a consistent error format across both. ASP.NET Core's `ProblemDetails` (RFC 9457) provides a standardized error envelope that is useful when:

                - Your API serves both REST and GraphQL clients
                - You need consistent error codes and structures across HTTP and GraphQL responses
                - Your existing services throw exceptions that follow ProblemDetails conventions
                - You want to reuse error handling logic between REST controllers and GraphQL resolvers

                If your application is purely GraphQL, you can use Hot Chocolate's native error handling without ProblemDetails.

                ## Implementation

                ### ProblemDetails Exception Base

                ```csharp
                namespace MyApp.Domain.Exceptions;

                public abstract class DomainException : Exception
                {
                    public abstract string ErrorCode { get; }
                    public abstract int StatusCode { get; }

                    protected DomainException(string message) : base(message) { }
                }

                public class EntityNotFoundException : DomainException
                {
                    public string EntityType { get; }
                    public string EntityId { get; }

                    public EntityNotFoundException(string entityType, string entityId)
                        : base($"{entityType} with ID '{entityId}' was not found.")
                    {
                        EntityType = entityType;
                        EntityId = entityId;
                    }

                    public override string ErrorCode => "ENTITY_NOT_FOUND";
                    public override int StatusCode => 404;
                }

                public class BusinessRuleViolationException : DomainException
                {
                    public string Rule { get; }

                    public BusinessRuleViolationException(string rule, string message)
                        : base(message)
                    {
                        Rule = rule;
                    }

                    public override string ErrorCode => "BUSINESS_RULE_VIOLATION";
                    public override int StatusCode => 422;
                }
                ```

                ### Error Filter with ProblemDetails Mapping

                ```csharp
                namespace MyApp.GraphQL;

                public class ProblemDetailsErrorFilter : IErrorFilter
                {
                    private readonly IHostEnvironment _environment;
                    private readonly ILogger<ProblemDetailsErrorFilter> _logger;

                    public ProblemDetailsErrorFilter(
                        IHostEnvironment environment,
                        ILogger<ProblemDetailsErrorFilter> logger)
                    {
                        _environment = environment;
                        _logger = logger;
                    }

                    public IError OnError(IError error)
                    {
                        if (error.Exception is DomainException domainEx)
                        {
                            return error
                                .WithMessage(domainEx.Message)
                                .WithCode(domainEx.ErrorCode)
                                .WithExtensions(new Dictionary<string, object?>
                                {
                                    ["statusCode"] = domainEx.StatusCode,
                                    ["type"] = $"https://myapp.com/errors/{domainEx.ErrorCode.ToLowerInvariant()}"
                                })
                                .RemoveException();
                        }

                        if (error.Exception is not null)
                        {
                            _logger.LogError(error.Exception, "Unhandled exception in GraphQL");

                            return _environment.IsProduction()
                                ? error
                                    .WithMessage("An internal error occurred.")
                                    .WithCode("INTERNAL_ERROR")
                                    .WithExtensions(new Dictionary<string, object?>
                                    {
                                        ["statusCode"] = 500,
                                        ["type"] = "https://myapp.com/errors/internal-error"
                                    })
                                    .RemoveException()
                                : error.WithCode("INTERNAL_ERROR");
                        }

                        return error;
                    }
                }
                ```

                ### Registration

                ```csharp
                var builder = WebApplication.CreateBuilder(args);

                // ASP.NET Core ProblemDetails for REST endpoints
                builder.Services.AddProblemDetails();

                // GraphQL with ProblemDetails-style error filter
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddMutationType()
                    .AddTypes()
                    .AddErrorFilter<ProblemDetailsErrorFilter>();

                var app = builder.Build();

                // REST error handling
                app.UseExceptionHandler();
                app.UseStatusCodePages();

                // GraphQL endpoint
                app.MapGraphQL();

                app.Run();
                ```

                ### Consistent Error Response

                Both REST and GraphQL errors now follow a similar structure:

                ```json
                {
                  "errors": [
                    {
                      "message": "User with ID '42' was not found.",
                      "extensions": {
                        "code": "ENTITY_NOT_FOUND",
                        "statusCode": 404,
                        "type": "https://myapp.com/errors/entity_not_found"
                      }
                    }
                  ]
                }
                ```

                ## Anti-patterns

                **Returning ProblemDetails objects directly from resolvers:**

                ```csharp
                // BAD: ProblemDetails is an HTTP concept, not a GraphQL type
                [QueryType]
                public static class Queries
                {
                    public static object GetUser(int id, AppDbContext db)
                    {
                        var user = db.Users.Find(id);
                        if (user is null)
                            return new ProblemDetails { Status = 404, Title = "Not Found" };
                        return user;
                    }
                }
                ```

                **Different error formats for REST and GraphQL:**

                ```csharp
                // BAD: Inconsistent error codes between REST and GraphQL
                // REST returns: { "type": "NOT_FOUND", ... }
                // GraphQL returns: { "code": "ENTITY_NOT_FOUND", ... }
                // Keep error codes consistent across both
                ```

                ## Key Points

                - Use a base exception class with `ErrorCode` and `StatusCode` for consistent error mapping
                - Map domain exceptions to GraphQL errors with matching codes and extensions in an error filter
                - Include a `type` URI in extensions for RFC 9457 compliance
                - Always strip internal details in production using `RemoveException()`
                - Share exception types between REST controllers and GraphQL resolvers for consistency
                - Use Hot Chocolate mutation conventions for expected errors; use error filters for unexpected exceptions

                ## Related Practices

                - [error-handling-error-filters] — For the underlying error filter mechanism
                - [error-handling-mutation-conventions] — For typed mutation errors
                - [configuration-server-setup] — For server setup
                """
            });
    }
}
