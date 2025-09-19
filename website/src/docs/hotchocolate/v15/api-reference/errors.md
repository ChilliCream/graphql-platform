---
title: Errors
---

GraphQL errors in Hot Chocolate are passed to the operation result by returning an instance of `IError` or an enumerable of `IError` in a field resolver.

Moreover, you can throw a `GraphQLException` that will be be caught by the execution engine and translated to a field error.

One further way to raise an error are non-terminating field errors. This can be raised by using `IResolverContext.ReportError`. With this you can provide a result and raise an error for your current field.

> If you do want to log errors head over to our diagnostic source [documentation](/docs/hotchocolate/v15/server/instrumentation) and see how you can hook up your logging framework of choice to it.

# Error Builder

Since errors can have a lot of properties, we have introduced a new error builder which provides a nice API without thousands of overloads.

```csharp
var error = ErrorBuilder
    .New()
    .SetMessage("This is my error.")
    .SetCode("FOO_BAR")
    .Build();
```

# Error Filters

If some other exception is thrown during execution, then the execution engine will create an instance of `IError` with the message **Unexpected Execution Error** and the actual exception assigned to the error. However, the exception details will not be serialized so by default the user will only see the error message **Unexpected Execution Error**.

If you want to translate exceptions into errors with useful information then you can write an `IErrorFilter`.

An error filter has to be registered as a service.

```csharp
builder.Services.AddErrorFilter<MyErrorFilter>();
```

It is also possible to just register the error filter as a delegate like the following.

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

Since errors are immutable we have added some helper functions like `WithMessage`, `WithCode` and so on that create a new error with the desired properties. Moreover, you can create an error builder from an error and modify multiple properties and then rebuild the error object.

```csharp
return ErrorBuilder
    .FromError(error)
    .SetMessage("This is my error.")
    .SetCode("FOO_BAR")
    .Build();
```

# Exception Details

In order to automatically add exception details to your GraphQL errors, you can enable the `IncludeExceptionDetails` option. By default this will be enabled when the debugger is attached.

```csharp
builder
    .AddGraphQL()
    .ModifyRequestOptions(
        o => o.IncludeExceptionDetails =
            builder.Environment.IsDevelopment());
```
