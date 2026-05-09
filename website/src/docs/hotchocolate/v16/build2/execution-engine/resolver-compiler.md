---
title: Resolver compiler
---

A resolver method can contain client arguments, services, the current parent object, state, cancellation, and context objects in one C# signature. The resolver compiler is the part of Hot Chocolate that turns that signature into the field delegate executed by the field pipeline.

Use this page when you need to predict how Hot Chocolate v16 classifies resolver parameters, why a parameter appears in the schema, or which resolver shape has the lowest request-time overhead.

## What you will learn

- What Hot Chocolate compiles or generates for a field resolver.
- Which C# members can act as resolvers.
- How parameters become GraphQL arguments, services, parent values, state values, or framework values.
- How return values are normalized for execution.
- How startup work differs from request execution work.
- How to troubleshoot unsupported or surprising resolver signatures.

## Know when this page helps

You usually do not need resolver compiler details to write ordinary fields. Start with [Resolver Signatures](/docs/hotchocolate/v16/build2/resolvers/resolver-signature), [Service Injection](/docs/hotchocolate/v16/build2/resolvers/service-injection), and [Parameter Attributes](/docs/hotchocolate/v16/build2/resolvers/parameter-attributes) when you want authoring guidance.

This page helps when:

- A service or framework parameter appears as a GraphQL argument.
- A keyed service or optional service fails at runtime.
- `[Parent]` does not bind the current object.
- You need `Optional<T>`, `IValueNode`, state attributes, `IResolverContext`, or `CancellationToken`.
- You are choosing between source-generated resolvers, runtime-compiled members, and hand-written delegate resolvers.
- You are investigating startup cost, AOT behavior, or hot field performance.

## Follow the field-to-delegate flow

Hot Chocolate needs a field delegate before execution can run a selected field. The delegate receives an `IResolverContext`, reads the values needed by your resolver, calls the resolver member, and returns a normalized result to field middleware.

```text
schema field
  -> resolver member or delegate
  -> parameter classification
  -> receiver binding
  -> result normalization
  -> field delegate
  -> field middleware invokes the delegate during requests
```

In v16 there are three common paths:

| Authoring path                        | What prepares the delegate                                 | When it happens       | Typical use                                                                            |
| ------------------------------------- | ---------------------------------------------------------- | --------------------- | -------------------------------------------------------------------------------------- |
| Implementation-first attributed types | Source generator emits schema and resolver wiring          | Build time            | New `[QueryType]`, `[MutationType]`, `[SubscriptionType]`, and `[ObjectType<T>]` APIs. |
| Reflected or descriptor-bound members | Runtime resolver compiler builds expression-tree delegates | Schema initialization | Code-first, schema-first, dynamic schemas, and reflected members.                      |
| `.Resolve(...)` delegate              | Your delegate is used directly                             | Schema initialization | Descriptor API fields where you read values from `IResolverContext`.                   |

Operation compilation is different. Operation compilation prepares a validated GraphQL operation for execution. Resolver compilation prepares schema fields and resolver delegates.

## Choose a supported resolver member shape

Hot Chocolate can invoke several C# shapes as resolvers:

| Resolver shape                                     | Receiver                           | Use when                                                                                                  |
| -------------------------------------------------- | ---------------------------------- | --------------------------------------------------------------------------------------------------------- |
| Public property on a source type                   | Current parent object              | The field maps to source data already present on the object.                                              |
| Public method on a source type                     | Current parent object              | The source object owns the field logic and can use `this`.                                                |
| Static method on an attributed root or object type | No receiver                        | You want dependencies visible as method parameters. This is the preferred v16 implementation-first shape. |
| Static method on `[ObjectType<T>]`                 | No receiver, parent is a parameter | You are adding fields to another runtime type. Use `[Parent] T parent`.                                   |
| Instance method on a separate resolver type        | Resolver object from Hot Chocolate | You need a resolver class. Do not store request-specific state on the instance.                           |
| Descriptor `.Resolve(...)` delegate                | Delegate parameter                 | You control value access with `ctx.ArgumentValue<T>()`, `ctx.Service<T>()`, and related APIs.             |

A static resolver keeps all request inputs in the method signature:

```csharp
[QueryType]
public static partial class BookQueries
{
    public static Task<Book?> GetBookAsync(
        int id,
        BookService books,
        CancellationToken cancellationToken)
        => books.GetBookAsync(id, cancellationToken);
}
```

Only the client argument appears in the schema:

```graphql
type Query {
  book(id: Int!): Book
}
```

## Apply the parameter classification rule

Every resolver parameter is classified. If Hot Chocolate does not recognize a parameter as a runtime binding, it treats the parameter as a GraphQL argument.

| Parameter shape                                  | Binding source             | In schema? | Notes                                                                      |
| ------------------------------------------------ | -------------------------- | ---------- | -------------------------------------------------------------------------- |
| `string term`, `int id`, `[Argument] T value`    | GraphQL argument           | Yes        | This is the fallback for unrecognized parameters.                          |
| `[GraphQLName("after")] int? publishedAfter`     | GraphQL argument           | Yes        | Renames the argument without changing its source.                          |
| `Optional<T>`                                    | GraphQL argument           | Yes        | Lets you distinguish omitted, explicit `null`, and a supplied value.       |
| `IValueNode` or a derived syntax node            | GraphQL argument literal   | Yes        | Use for advanced literal handling.                                         |
| `[Parent] T parent`                              | Parent value               | No         | Use in static extension and separate resolver methods.                     |
| Registered service type                          | Dependency injection       | No         | Requires the application service provider to report the type as a service. |
| `[Service] T service`                            | Dependency injection       | No         | Use for keyed services or explicit service binding.                        |
| `CancellationToken`                              | Request cancellation       | No         | Pass it to asynchronous I/O APIs.                                          |
| `IResolverContext`                               | Resolver context           | No         | Prefer specific parameters unless you need context APIs.                   |
| `[GlobalState]`, `[ScopedState]`, `[LocalState]` | Request or field state     | No         | The key must exist and the value must be coercible.                        |
| `[IsSelected] bool selected`                     | Selection flag             | No         | Useful when loading optional nested data.                                  |
| `[EventMessage] T message`                       | Subscription event message | No         | Use only in subscription event resolvers.                                  |
| Custom parameter binding                         | Custom runtime value       | Usually no | Advanced extension point. Prefer built-in bindings first.                  |

Framework integrations can add more bindings. For example, data middleware can add data-specific context parameters, and ASP.NET Core integration can add HTTP context related parameters.

## Bind GraphQL arguments deliberately

Use plain parameters for ordinary client input. Use `[Argument]` when you want to force argument binding or use an argument name that is not the C# parameter name. Use `[GraphQLName]` when you want to rename the schema argument.

```csharp
[QueryType]
public static partial class BookQueries
{
    public static IReadOnlyList<Book> SearchBooks(
        string? term,
        [GraphQLName("after")] int? publishedAfter,
        BookSearchService search)
        => search.Search(term, publishedAfter);
}
```

Expected SDL excerpt:

```graphql
type Query {
  searchBooks(term: String, after: Int): [Book!]!
}
```

Use `Optional<T>` when the resolver must react differently to omitted input and explicit `null`:

```csharp
public static IReadOnlyList<Book> SearchBooks(Optional<string?> term)
{
    if (!term.HasValue)
    {
        return BookSearchDefaults.AllBooks;
    }

    return BookSearchDefaults.Search(term.Value);
}
```

Use `IValueNode` only when you need the raw GraphQL syntax node:

```csharp
public static string EchoLiteral(StringValueNode value)
    => value.Value;
```

Most resolvers should accept typed CLR values instead of syntax nodes.

## Bind the parent value intentionally

For an instance member on the source type, the source object is the receiver. You use `this` because the member is called on the parent object:

```csharp
public sealed class Book
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string DisplayTitle()
        => $"Book: {Title}";
}
```

For static extension-style resolvers, add `[Parent]`:

```csharp
[ObjectType<Book>]
public static partial class BookNode
{
    public static Task<Author?> GetAuthorAsync(
        [Parent] Book book,
        AuthorService authors,
        CancellationToken cancellationToken)
        => authors.GetAuthorAsync(book.AuthorId, cancellationToken);
}
```

`[Parent] Book book` is not a GraphQL argument. Hot Chocolate supplies the current `Book` value from the resolver tree. The parent parameter type must match the runtime parent type, a base type, or an implemented interface.

Use `[Parent(requires: nameof(Book.AuthorId))]` when projection-aware fields need a specific parent member to be available. For full guidance, see [Parent access](/docs/hotchocolate/v16/build2/resolvers/parent-attribute).

## Bind services and respect DI scope

In v16, registered application services are inferred automatically:

```csharp
builder.Services.AddScoped<BookService>();

builder
    .AddGraphQL()
    .AddTypes();
```

```csharp
[QueryType]
public static partial class BookQueries
{
    public static Task<Book?> GetBookAsync(
        int id,
        BookService books,
        CancellationToken cancellationToken)
        => books.GetBookAsync(id, cancellationToken);
}
```

`BookService` stays out of the schema because it is a runtime service.

Use `[Service]` for keyed services:

```csharp
builder.Services.AddKeyedScoped<BookService>("archive");
```

```csharp
public static Task<Book?> GetArchivedBookAsync(
    int id,
    [Service("archive")] BookService books,
    CancellationToken cancellationToken)
    => books.GetBookAsync(id, cancellationToken);
```

Runtime service inference depends on `IServiceProviderIsService` support from the application service provider. If Hot Chocolate cannot ask the provider whether a type is registered, or the type is not registered in application DI, the parameter can fall through to GraphQL argument binding. `IEnumerable<T>` can be inferred as a service when `T` is reported as registered.

Write non-nullable service parameters for required dependencies. Use nullable service parameters only when `null` is a valid application path, and add `[Service]` when optional service inference must not fall back to argument binding.

For service scopes, `[UseRequestScope]`, EF Core, DataLoaders, and constructor-injection warnings, see [Service Injection](/docs/hotchocolate/v16/build2/resolvers/service-injection).

## Use context, cancellation, state, and selection parameters

Prefer typed parameters for common resolver inputs:

```csharp
[ObjectType<Book>]
public static partial class BookNode
{
    public static async Task<string> GetSummaryAsync(
        [Parent] Book book,
        [ScopedState("locale")] string locale,
        BookSummaryService summaries,
        CancellationToken cancellationToken)
        => await summaries.CreateSummaryAsync(book.Id, locale, cancellationToken);
}
```

Use `IResolverContext` when you need APIs that do not fit a specific parameter, such as dynamic argument names, manual error reporting, context data, or advanced middleware interaction:

```csharp
public static string GetTraceId(IResolverContext context)
    => context.ContextData.TryGetValue("traceId", out var value)
        ? value?.ToString() ?? "unknown"
        : "unknown";
```

State attributes bind values by key:

| Attribute              | Source                                                     |
| ---------------------- | ---------------------------------------------------------- |
| `[GlobalState("key")]` | Request-level global state.                                |
| `[ScopedState("key")]` | Scoped context data propagated through the field pipeline. |
| `[LocalState("key")]`  | State local to the current field pipeline.                 |

Missing or incompatible state values surface during execution. Verify the key, the state scope, and the code that adds the state. Global state is commonly added by request interceptors.

## Understand result handling

The compiled or generated delegate normalizes the resolver return value so field middleware and completion can process it through `IMiddlewareContext.Result`.

Common return shapes are:

| Return shape                                 | Use when                                                                                                                          |
| -------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| `T` or `T?`                                  | The value is already available.                                                                                                   |
| `Task<T>`                                    | The resolver performs asynchronous I/O.                                                                                           |
| `ValueTask<T>`                               | The API you call already returns `ValueTask<T>` or measurement supports it.                                                       |
| `IEnumerable<T>`, arrays, `IReadOnlyList<T>` | Items are already materialized.                                                                                                   |
| `IQueryable<T>`                              | Data middleware should compose provider operations before materialization.                                                        |
| `IExecutable<T>`                             | A Hot Chocolate data provider should own execution.                                                                               |
| `IAsyncEnumerable<T>`                        | Items are produced asynchronously. Ordinary field completion materializes list results. Use subscriptions for live event streams. |
| `Connection<T>`                              | The resolver constructs a cursor paging result.                                                                                   |
| `ResolverResult<T>`                          | The resolver returns a value plus field errors or status information.                                                             |

Keep `IQueryable<T>`, `IExecutable<T>`, `IAsyncEnumerable<T>`, and `Connection<T>` aligned with the field middleware and provider documentation. For details, see [Resolver Result Handling](/docs/hotchocolate/v16/build2/resolvers/resolver-result-handling).

## Plan for startup and request performance

Resolver signatures are analyzed before requests execute:

- Source-generated implementation-first resolvers are inspected at build time and emitted as generated schema and resolver wiring.
- Runtime-compiled resolvers are compiled while the schema is initialized.
- Completed schemas reuse resolver delegates across requests.
- Request execution should not rediscover resolver members through reflection for every field.

That model shifts work toward build time or startup. It reduces request-time reflection and lets the execution engine call prepared delegates.

For hot fields:

- Use source-generated static partial resolver types for new implementation-first APIs.
- Keep dependencies visible as typed parameters.
- Return a synchronous value when the work is already complete.
- Do not wrap synchronous work in `Task.Run`.
- Avoid `IResolverContext` when a specific parameter communicates the same need.
- Pass `CancellationToken` to I/O APIs.
- Return `IQueryable<T>` or `IExecutable<T>` only when middleware or a provider should compose execution.

Synchronous resolvers with pure parameter bindings can use a synchronous fast path. Async, executable, queryable, and streaming results use the normal async or data path.

## Consider AOT and source generation

The runtime resolver compiler builds expression trees during schema initialization and is intended for JIT-compatible environments. If you are targeting native AOT, prefer the v16 implementation-first source generator path and avoid dynamic schema patterns that require runtime expression compilation.

Source generation and runtime compilation follow the same user-facing binding model: arguments, services, parent values, state, cancellation, context, and custom bindings are still classified from the resolver signature. Diagnostics can differ. Source-generated code can report analyzer diagnostics during build, while runtime-compiled schemas usually report schema initialization or execution errors.

## Add custom parameter bindings only for infrastructure

Most applications should use built-in argument, service, parent, state, cancellation, and context bindings. If you are building infrastructure, you can register a custom parameter expression builder:

```csharp
builder
    .AddGraphQL()
    .AddParameterExpressionBuilder(
        context => (Tenant)context.ContextData["tenant"]!,
        parameter => parameter.ParameterType == typeof(Tenant));
```

A custom binding can keep a parameter out of the GraphQL schema and provide the value from `IResolverContext`. Mark the binding as pure only when the expression has no side effects and can be used in the synchronous resolver path.

Prefer a small domain-specific attribute or a normal service parameter when that makes the resolver clearer. Custom bindings affect schema discovery, generated code, and runtime compilation, so treat them as a shared framework extension point.

## Troubleshoot resolver signatures

| Symptom                                                    | What to check                                                                                                                                |
| ---------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| A service parameter appears as a GraphQL argument.         | Register the service in application DI, verify service inference support, or add `[Service]` for explicit binding.                           |
| A keyed service is missing or throws.                      | Verify the DI key and `[Service("key")]` match. Use nullable service parameters only when `null` is a valid path.                            |
| An argument has the wrong name.                            | Check descriptor argument configuration, `[Argument]`, `[GraphQLName]`, and generated field conventions.                                     |
| `[Parent]` fails.                                          | Add `[Parent]` to extension and separate resolver parameters. Check that the parameter type is compatible with the field parent type.        |
| A parent parameter appears in the schema.                  | Add `[Parent]`. Without a recognized binding, the parameter is an argument.                                                                  |
| The resolver cannot distinguish omitted input from `null`. | Use `Optional<T>` for that argument.                                                                                                         |
| The resolver needs raw GraphQL syntax.                     | Use `IValueNode` or a derived node only for advanced literal handling.                                                                       |
| Cancellation is ignored.                                   | Accept `CancellationToken` and pass it to EF Core, HTTP clients, DataLoaders, and service methods.                                           |
| A state value is missing.                                  | Verify the key, scope, interceptor or middleware setup, and value type.                                                                      |
| The field is slower than expected.                         | Remove unnecessary async wrappers, prefer typed parameters over context-heavy code, and verify that services did not fall back to arguments. |

## Recognize diagnostics and runtime errors

Source generation can report resolver-related diagnostics before the app starts:

| Diagnostic                                  | Meaning                                                                             |
| ------------------------------------------- | ----------------------------------------------------------------------------------- |
| `HC0097 Parent Attribute Type Mismatch`     | A `[Parent]` parameter type is not compatible with the field parent runtime type.   |
| `HC0098 Parent Method Type Mismatch`        | A `Parent<T>()` type argument is not compatible with the field parent runtime type. |
| `HC0099 QueryContext With UseProjection`    | A resolver uses `QueryContext<T>` together with `[UseProjection]`.                  |
| `HC0100 Data Attribute Order`               | Data attributes are not ordered as paging, projection, filtering, sorting.          |
| `HC0101 QueryContext Generic Type Mismatch` | A `QueryContext<T>` parameter does not match the connection node type.              |

Runtime-compiled schemas can report different failures:

- Schema initialization errors when a member shape cannot be compiled as a resolver.
- Execution-time service errors when a required service is missing.
- Execution-time state errors when a required state value is missing or cannot be coerced.
- Argument coercion errors when a client value cannot be converted to the parameter type.

## Next steps

- Write and review resolver signatures with [Resolver Signatures](/docs/hotchocolate/v16/build2/resolvers/resolver-signature).
- Bind services and choose scopes with [Service Injection](/docs/hotchocolate/v16/build2/resolvers/service-injection).
- Learn every parameter attribute in [Resolver Parameter Attributes](/docs/hotchocolate/v16/build2/resolvers/parameter-attributes).
- Follow field execution in [Execution pipeline](/docs/hotchocolate/v16/build2/execution-engine/pipeline).
- Choose return shapes with [Resolver Result Handling](/docs/hotchocolate/v16/build2/resolvers/resolver-result-handling).
