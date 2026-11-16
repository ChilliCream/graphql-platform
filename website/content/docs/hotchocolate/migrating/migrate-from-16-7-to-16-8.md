---
title: Migrate Hot Chocolate from 16.7 to 16.8
metaTitle: "Hot Chocolate 16.8 Migration Guide"
description: "Migration guide for Hot Chocolate v16.7 to v16.8: replace ReportError on the resolver context by throwing a GraphQLException."
---

Update every `HotChocolate.*` package in the application to version 16.8 before applying these changes.

# Deprecations

Things that will continue to function this release, but we encourage you to move away from.

## ReportError on the resolver context

The `ReportError` overloads on `IResolverContext` and `IMiddlewareContext` are obsolete and will be removed in v17. They add an error at the path of the current field while the field can still return a value. The GraphQL specification handles a field that raises an error as though it resolved to `null`.

Throw a `GraphQLException` instead. The execution engine resolves the field to `null` and adds the error at the field's path, so the error does not need a path of its own:

```diff
 public Book? GetBook(int id, IResolverContext context)
 {
     if (!_books.TryGetValue(id, out var book))
     {
-        context.ReportError("The book could not be found.");
-        return null;
+        throw new GraphQLException("The book could not be found.");
     }

     return book;
 }
```

An error built with `ErrorBuilder` works the same way:

```diff
-context.ReportError(
+throw new GraphQLException(
     ErrorBuilder.New()
         .SetMessage("The book could not be found.")
         .SetCode("BOOK_NOT_FOUND")
         .Build());
```

| Deprecated 16.7 member                                           | 16.8 replacement                                                                                                      |
| ---------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| `IResolverContext.ReportError(string)`                           | `throw new GraphQLException(string)`                                                                                  |
| `IResolverContext.ReportError(IError)`                           | `throw new GraphQLException(IError)`                                                                                  |
| `IResolverContext.ReportError(Exception, Action<ErrorBuilder>?)` | Rethrow the exception, or throw a `GraphQLException` with an error built from `ErrorBuilder.FromException(exception)` |

Throwing ends the resolver. If the field is non-null, the `null` propagates to the nearest nullable parent, as it does for any other field error.
