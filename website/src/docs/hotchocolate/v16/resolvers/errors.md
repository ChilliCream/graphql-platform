---
title: Errors
description: Learn how to handle, create, and filter GraphQL errors in Hot Chocolate.
---

In GraphQL, errors are not all-or-nothing. If a resolver fails, the rest of the query can still return data. This is called a _field error_ (or non-terminating error). When this happens, the failed field returns `null`, and an entry is added to the `errors` array. Other fields continue to resolve as usual.

# Exceptions

The easiest way to signal an error is to throw an exception. Hot Chocolate will catch any exception thrown during resolver execution and automatically turn it into a GraphQL error.

<ExampleTabs>
<Implementation>

```csharp
[QueryType]
public static partial class Query
{
    public static Book GetBook()
    {
        throw new InvalidOperationException("Something went wrong.");
    }
}
```

</Implementation>
<Code>

```csharp
public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("book")
            .Type<BookType>()
            .Resolve(ctx =>
            {
                throw new InvalidOperationException("Something went wrong.");
            });
    }
}
```

</Code>
</ExampleTabs>

By default, Hot Chocolate does **not** send exception details to the client. Instead, the response contains a generic message to avoid exposing internal information.

```json
{
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "locations": [{ "line": 1, "column": 3 }],
      "path": ["book"]
    }
  ],
  "data": {
    "book": null
  }
}
```

# Error Filters

If you use typed exceptions and want to return specific GraphQL errors, you can implement error filters. Error filters let you catch errors before they reach the client and rewrite them as needed.

For example, if your service throws a `NotFoundException`, you can map it to a clear error message and code:

```csharp
builder
    .AddGraphQL()
    .AddErrorFilter(error =>
    {
        if (error.Exception is NotFoundException ex)
        {
            return ErrorBuilder
                .FromError(error)
                .SetMessage(ex.Message)
                .SetCode("NOT_FOUND")
                .Build();
        }

        return error;
    });
```

Now, when a resolver throws a `NotFoundException`, the client receives a structured error instead of a generic message:

```json
{
  "errors": [
    {
      "message": "The book with ID '123' was not found.",
      "locations": [{ "line": 1, "column": 3 }],
      "path": ["book"],
      "extensions": {
        "code": "NOT_FOUND"
      }
    }
  ],
  "data": {
    "book": null
  }
}
```

> **Note:** Errors are immutable. Methods like `WithMessage`, `WithCode`, and `RemoveExtension` return a new error instance. Use `ErrorBuilder.FromError(error)` to change multiple properties at once.

# GraphQLException

If you want full control over the error in a resolver, throw a `GraphQLException`. Unlike regular exceptions, these errors are sent to the client as-is, without being wrapped in a generic message.

<ExampleTabs>
<Implementation>

```csharp
[QueryType]
public static partial class Query
{
    public static Book GetBook()
    {
        throw new GraphQLException("The book could not be found.");
    }
}
```

</Implementation>
<Code>

```csharp
public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("book")
            .Type<BookType>()
            .Resolve(ctx =>
            {
                throw new GraphQLException("The book could not be found.");
            });
    }
}
```

</Code>
</ExampleTabs>

```json
{
  "errors": [
    {
      "message": "The book could not be found.",
      "locations": [{ "line": 1, "column": 3 }],
      "path": ["book"]
    }
  ],
  "data": {
    "book": null
  }
}
```

You can also use `GraphQLException` with an `ErrorBuilder` to add a code, extensions, or multiple errors:

```csharp
throw new GraphQLException(
    ErrorBuilder
        .New()
        .SetMessage("The book could not be found.")
        .SetCode("BOOK_NOT_FOUND")
        .Build());
```

```json
{
  "errors": [
    {
      "message": "The book could not be found.",
      "locations": [{ "line": 1, "column": 3 }],
      "path": ["book"],
      "extensions": {
        "code": "BOOK_NOT_FOUND"
      }
    }
  ]
}
```

# Next Steps

- [Mutation conventions](/docs/hotchocolate/v16/defining-a-schema/mutations) for structured mutation error handling
- [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) for logging and diagnostics
- [Options reference](/docs/hotchocolate/v16/server/options) for `IncludeExceptionDetails` and other settings
