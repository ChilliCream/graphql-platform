---
title: Errors
description: Learn how to handle, create, and filter GraphQL errors in Hot Chocolate v16.
---

Hot Chocolate provides several ways to report errors from your GraphQL resolvers. You can return `IError` instances, throw a `GraphQLException`, or use non-terminating field errors through `IResolverContext.ReportError`.

# Returning Errors

Return an `IError` or `IEnumerable<IError>` from a field resolver to report errors in the GraphQL response.

Throw a `GraphQLException` from any resolver, and the execution engine catches it and translates it into a field error.

Call `IResolverContext.ReportError` to raise a non-terminating error. This allows you to return a result and report an error for the same field.

> To log errors, see the [instrumentation documentation](/docs/hotchocolate/v16/server/instrumentation) for connecting your logging framework.

# Error Builder

Errors can have many properties. The `ErrorBuilder` provides a fluent API for constructing them:

```csharp
var error = ErrorBuilder
    .New()
    .SetMessage("This is my error.")
    .SetCode("FOO_BAR")
    .Build();
```

# Error Filters

When an unexpected exception is thrown during execution, the engine creates an `IError` with the message **Unexpected Execution Error** and attaches the original exception. Exception details are not serialized by default, so the user sees only the generic message.

To translate exceptions into errors with useful information, implement an `IErrorFilter` and register it:

```csharp
builder.Services.AddErrorFilter<MyErrorFilter>();
```

You can also register a filter as a delegate:

```csharp
builder.Services.AddErrorFilter(error =>
{
    if (error.Exception is NullReferenceException)
    {
        return error.WithCode("NullRef");
    }

    return error;
});
```

Errors are immutable. Helper methods like `WithMessage`, `WithCode`, and others return a new error with the desired properties. You can also create a builder from an existing error to modify multiple properties:

```csharp
return ErrorBuilder
    .FromError(error)
    .SetMessage("This is my error.")
    .SetCode("FOO_BAR")
    .Build();
```

# Exception Details

To include exception details in GraphQL errors automatically, enable the `IncludeExceptionDetails` option. By default, this is enabled when the debugger is attached:

```csharp
builder
    .AddGraphQL()
    .ModifyRequestOptions(
        o => o.IncludeExceptionDetails =
            builder.Environment.IsDevelopment());
```

> Do not enable `IncludeExceptionDetails` in production. Exception details can leak security-sensitive information.

# Troubleshooting

**"Unexpected Execution Error" with no details**
Register an `IErrorFilter` to translate exceptions into meaningful GraphQL errors. Alternatively, enable `IncludeExceptionDetails` during development to see the full exception.

**Error filter not being invoked**
Verify that you registered the filter using `AddErrorFilter<T>()` on `IServiceCollection`. If you are using the split service provider model in v16, you may need to register the filter's dependencies using `AddApplicationService<T>()` on the `IRequestExecutorBuilder`.

**ReportError does not appear in the response**
Confirm that the resolver continues to execute after calling `ReportError`. Non-terminating errors require the resolver to return a value (or `null`).

# Next Steps

- [Mutation conventions](/docs/hotchocolate/v16/building-a-schema/mutations) for structured mutation error handling
- [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) for logging and diagnostics
- [Options reference](/docs/hotchocolate/v16/api-reference/options) for `IncludeExceptionDetails` and other settings
