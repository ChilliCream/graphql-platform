---
title: Error Filter
---

GraphQL errors in Hot Chocolate are passed to the query result by returning an instance of `IError` or an enumerable of `IError` in a field resolver.

Moreover, you can throw a `QueryException` that will be be caught by the query engine and translated to a field error.

One further way to raise an error are non-terminating field errors. This can be raised by using `IResolverContext.RaiseError`. So, with this you can provide a result and raise an error for your current field.

> If you do want to log errors head over to our diagnostic source [documentation](/docs/hotchocolate/v10/execution-engine/instrumentation) and see how you can hook up your logging framework of choice to it.

# Error Builder

Since, errors can have a lot of properties depending on your case we have introduced a new error builder which provides a nice API without thousands of overloads.

```csharp
return ErrorBuilder.New()
    .SetMessage("This is my error.")
    .SetCode("FOO_BAR")
    .Build();
```

# Exceptions

If some other exception is thrown during the query execution, then the execution engine will create an instance of `IError` with the message **Unexpected Execution Error** and the actual exception assigned to the error. However, the exception details will not be serialized so by default the user will only see the error message **Unexpected Execution Error**.

If you want to translate exceptions into errors with useful information then you can write an `IErrorFilter`.

An error filter has to be registered with the execution builder or with your dependency injection.

```csharp
IQueryExecuter executer = schema.MakeExecutable(builder =>
    builder.UseDefaultPipeline(options)
        .AddErrorFilter<MyErrorFilter>());
```

OR

```csharp
services.AddErrorFilter<MyErrorFilter>();
```

It is also possible to just register the error filter as a delegate like the following.

```csharp
IQueryExecuter executer = schema.MakeExecutable(builder =>
    builder.UseDefaultPipeline(options)
        .AddErrorFilter(error =>
        {
            if (error.Exception is NullReferenceException)
            {
                return error.WithCode("NullRef");
            }
            return error;
        }));
```

Since errors are immutable we have added some helper functions like `WithMessage`, `WithCode` and so on that create a new error with the desired properties. Moreover, you can create an error builder from an error and modify multiple properties and then rebuild the error object.

```csharp
return ErrorBuilder.FromError(error)
    .SetMessage("This is my error.")
    .SetCode("FOO_BAR")
    .Build();
```

# Exception Details

In order to automatically add exception details to your GraphQL error you can switch the execution option to include exception details. By default we will switch this on if the debugger is attached. You can overwrite the behavior by setting the option.

```csharp
SchemaBuilder.New()
    ...
    .Create()
    .MakeExecutable(new QueryExecutionOptions
    {
        IncludeExceptionDetails = true
    });
```

 ## Customizable IErrors 

Hot Chocolate provides some methods in Error to help add or remove messages,
codes, locations, paths and more, making error messages more readable

 - `RemoveCode()`
 - `RemoveException()`
 - `RemoveExtension(string key)`
 - `RemoveExtensions()`
 - `RemoveLocations()`
 - `RemovePath()`
 - `SetExtension(string key, object? value)`
 - `WithCode(string? code)`
 - `WithException(Exception? exception)`
 - `WithExtensions(IReadOnlyDictionary<string, object?> extensions)`
 - `WithLocations(IReadOnlyList<Location>? locations)`
 - `WithMessage(string message)`
 - `WithPath(IReadOnlyList<object>? path)`
 - `WithPath(Path? path)`
