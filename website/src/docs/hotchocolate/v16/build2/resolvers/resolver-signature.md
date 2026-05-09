---
title: "Resolver Signatures"
---

A resolver signature is the C# boundary for one GraphQL field. It tells Hot Chocolate which client arguments to read, which runtime values to inject, how to run asynchronous work, and how to adapt the result into the GraphQL response.

Use the smallest signature that describes the field clearly. Add arguments, parent values, services, cancellation, context, and specialized return shapes only when the field needs them.

## What you will learn

- How the public GraphQL field contract differs from the C# resolver signature.
- Which resolver parameters become GraphQL arguments and which are bound by Hot Chocolate.
- How to add arguments, services, DataLoaders, cancellation, and `IResolverContext` deliberately.
- Which return shape to choose for synchronous values, asynchronous values, collections, data middleware, paging, and payload objects.
- How to avoid common signature problems such as accidental schema arguments, sync-over-async, and early query materialization.

---

## Start from a complete resolver

The following resolver exposes one nullable field named `bookById`. Assume `BookRepository` is registered in dependency injection.

```csharp
[QueryType]
public static partial class BookQueries
{
    public static async Task<Book?> GetBookByIdAsync(
        [ID] int id,
        BookRepository repository,
        CancellationToken cancellationToken)
        => await repository.GetBookByIdAsync(id, cancellationToken);
}
```

This signature produces a field shape like this:

```graphql
type Query {
  bookById(id: ID!): Book
}
```

| Signature part                        | Bound from                              | Schema effect                                                          |
| ------------------------------------- | --------------------------------------- | ---------------------------------------------------------------------- |
| `[ID] int id`                         | Client argument named `id`              | Exposed as a non-null `ID` argument.                                   |
| `BookRepository repository`           | Registered service                      | Not exposed in the schema.                                             |
| `CancellationToken cancellationToken` | Request cancellation from Hot Chocolate | Not exposed in the schema.                                             |
| `Task<Book?>`                         | Resolver result                         | Hot Chocolate awaits the task and allows the field value to be `null`. |

Later examples add one concept at a time.

---

## Understand what the signature controls

The GraphQL field contract is the public API clients see. It includes the field name, argument names, argument types, return type, nullability, and any schema directives or middleware effects.

The C# resolver signature is the implementation boundary Hot Chocolate calls at runtime. Its parameters can come from client arguments, the parent value, services, request state, or execution metadata. Its return value can be a finished value, an awaitable value, a queryable source, a paged connection, or a domain payload.

```text
GraphQL field
  arguments       -> ordinary resolver parameters and argument attributes
  parent value    -> [Parent] or IResolverContext.Parent<T>()
  services        -> registered service parameters or [Service]
  request state   -> CancellationToken, IResolverContext, state, metadata
  resolver result -> field middleware -> GraphQL response value or error
```

A parameter becomes a GraphQL argument unless Hot Chocolate recognizes it as a runtime binding. Registered services, `[Parent]` values, `CancellationToken`, `IResolverContext`, state parameters, and similar framework values are runtime inputs, not client arguments.

---

## Start with the smallest useful resolver

Synchronous resolvers are a good fit when the value is already in memory, the work is a cheap calculation, and no I/O is performed.

```csharp
public sealed record Book(int Id, string Title, int AuthorId);

[QueryType]
public static partial class BookQueries
{
    private static readonly Book[] s_books =
    [
        new(1, "The Hobbit", 10),
        new(2, "Kindred", 20)
    ];

    public static Book GetFeaturedBook()
        => s_books[0];

    public static Book? GetBookById([ID] int id)
        => s_books.FirstOrDefault(book => book.Id == id);
}
```

This produces a non-null field for `GetFeaturedBook` and a nullable field for `GetBookById`:

```graphql
type Query {
  featuredBook: Book!
  bookById(id: ID!): Book
}
```

Use `T` when the field always returns a value. Use `T?` when a missing value is a valid outcome. Do not wrap CPU-only mapping in `Task.Run` to make the field look asynchronous.

---

## Bind GraphQL arguments deliberately

Ordinary resolver parameters become GraphQL arguments. Argument attributes let you adjust the public schema name, default value, scalar mapping, or binding behavior without changing the rest of the signature.

```csharp
public sealed record SearchBooksInput(string? Title, int? PublishedAfter);

[QueryType]
public static partial class BookQueries
{
    public static IReadOnlyList<Book> SearchBooks(
        string? title,
        [DefaultValue(10)] int limit,
        [GraphQLName("after")] int? publishedAfter,
        [ID] int? authorId,
        SearchBooksInput? input,
        BookSearchService search)
        => search.Search(title, limit, publishedAfter, authorId, input);
}
```

The service parameter is a runtime input when `BookSearchService` is registered. The other parameters are client arguments.

```graphql
input SearchBooksInput {
  title: String
  publishedAfter: Int
}

type Query {
  searchBooks(
    title: String
    limit: Int! = 10
    after: Int
    authorId: ID
    input: SearchBooksInput
  ): [Book!]!
}
```

| Signature shape                                    | Schema effect                          | Details                                                                           |
| -------------------------------------------------- | -------------------------------------- | --------------------------------------------------------------------------------- |
| `string title`                                     | `title: String!`                       | [Arguments](/docs/hotchocolate/v16/building-a-schema/arguments)                   |
| `string? title`                                    | `title: String`                        | [Nullability](/docs/hotchocolate/v16/building-a-schema/non-null)                  |
| `[DefaultValue(10)] int limit` or `int limit = 10` | Default value appears in the schema    | [Arguments](/docs/hotchocolate/v16/building-a-schema/arguments)                   |
| `[GraphQLName("name")] string username`            | Public argument is `name`              | [Parameter Attributes](./parameter-attributes)                                    |
| `[ID] int id`                                      | Public argument uses the `ID` scalar   | [Relay and IDs](/docs/hotchocolate/v16/building-a-schema/relay)                   |
| `[ID<Product>] int id`                             | ID is restricted to the `Product` type | [Relay and IDs](/docs/hotchocolate/v16/building-a-schema/relay)                   |
| `SearchBooksInput input`                           | Input object argument                  | [Input Object Types](/docs/hotchocolate/v16/building-a-schema/input-object-types) |

> **Watch out:** Registered service types and framework types such as `CancellationToken` are not GraphQL arguments. If one appears in the schema, check the binding guidance in [Service Injection](./service-injection) and [Parameter Attributes](./parameter-attributes).

---

## Add asynchronous work and cancellation

Use `Task<T>` for ordinary I/O, such as database queries, HTTP calls, file access, and DataLoader calls.

```csharp
[QueryType]
public static partial class BookQueries
{
    public static Task<Book?> GetBookByIdAsync(
        [ID] int id,
        BookRepository repository,
        CancellationToken cancellationToken)
        => repository.GetBookByIdAsync(id, cancellationToken);
}
```

Hot Chocolate supplies `CancellationToken` from `IResolverContext.RequestAborted`. Pass it to every downstream API that accepts cancellation, including EF Core, HTTP clients, service methods, and DataLoader `LoadAsync` calls.

`ValueTask<T>` is also supported. Use it when the API you call already returns `ValueTask<T>` or when measurement shows the field is a hot path that benefits from it. Prefer `Task<T>` for most asynchronous resolver code because it is easier to compose and harder to misuse.

Plain `Task` and plain `ValueTask` are supported delegate shapes, but they do not describe a useful field value. Use them only when the field or middleware contract intentionally has no meaningful result. Data fields should return an explicit value shape such as `Task<Book?>`, `Task<IReadOnlyList<Book>>`, or `Task<Connection<Book>>`.

> **Watch out:** Do not call `.Result`, `.Wait()`, or other sync-over-async patterns inside resolvers. Await asynchronous APIs and pass the cancellation token through.

---

## Choose a return shape

Choose the return shape that matches where the data comes from and what Hot Chocolate should do with it after the resolver returns.

| Return shape                                | Use when                                                                        | Signature notes                                                                               | Details                                                                                      |
| ------------------------------------------- | ------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| `T`                                         | The value is already available.                                                 | Good for mapping and cheap derived fields.                                                    | [Result Handling](./resolver-result-handling)                                                |
| `T?`                                        | The field can be missing.                                                       | Align C# nullability with GraphQL nullability.                                                | [Nullability](/docs/hotchocolate/v16/building-a-schema/non-null)                             |
| `IEnumerable<T>`, `IReadOnlyList<T>`, array | The items are already materialized.                                             | Query middleware may run in memory or may not translate to the provider.                      | [Result Handling](./resolver-result-handling)                                                |
| `Task<T>`                                   | The resolver performs asynchronous I/O.                                         | Preferred general asynchronous shape.                                                         | [Async and cancellation](#add-asynchronous-work-and-cancellation)                            |
| `ValueTask<T>`                              | The called API returns `ValueTask<T>` or measurement supports it.               | Avoid as the default for ordinary asynchronous code.                                          | [Result Handling](./resolver-result-handling)                                                |
| `IQueryable<T>`                             | Paging, projection, filtering, or sorting should translate to a query provider. | Do not enumerate before middleware runs.                                                      | [Fetching from Databases](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-databases) |
| `IExecutable<T>`                            | A Hot Chocolate data provider executable should own query execution.            | Provider-specific. Follow the provider documentation for construction and middleware support. | [Result Handling](./resolver-result-handling)                                                |
| `IAsyncEnumerable<T>`                       | Values are produced asynchronously.                                             | Confirm the field's middleware and transport behavior before using it as a list shape.        | [Result Handling](./resolver-result-handling)                                                |
| `Connection<T>`                             | The resolver constructs a cursor paging result.                                 | Common with custom paging services and external APIs.                                         | [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination)                           |
| `QueryContext<T>`                           | You use the v16 integrated projection, filtering, and sorting pattern.          | Do not combine with `[UseProjection]`.                                                        | [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections)                         |
| Payload or result object                    | A mutation returns data plus expected domain errors.                            | Keep domain failures in the payload shape instead of throwing for expected cases.             | [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations)                              |

Treat `IExecutable<T>` and `IAsyncEnumerable<T>` as specialized shapes. They are supported by Hot Chocolate, but the correct usage depends on the provider, middleware stack, and transport expectations. Use provider documentation or a dedicated result handling page before adopting them broadly.

---

## Add parent values without hiding the field contract

Nested field resolvers often need the object produced by the parent field. Add `[Parent]` to make that runtime input explicit.

```csharp
[ObjectType<Book>]
public static partial class BookNode
{
    public static async Task<Author?> GetAuthorAsync(
        [Parent] Book book,
        IAuthorByIdDataLoader authorById,
        CancellationToken cancellationToken)
        => await authorById.LoadAsync(book.AuthorId, cancellationToken);
}
```

`[Parent] Book book` is not a GraphQL argument. It is the `Book` value already resolved for the current object. Use `context.Parent<T>()` when you write a delegate resolver or need context-based access.

See [Parent access](./parent-attribute) for the full parent value guidance.

---

## Add services and DataLoaders as runtime inputs

In Hot Chocolate v16, registered services are inferred automatically. Put services directly in the resolver method signature and keep application logic outside the resolver.

```csharp
[QueryType]
public static partial class BookQueries
{
    public static Task<Book?> GetBookByIdAsync(
        [ID] int id,
        BookRepository repository,
        CancellationToken cancellationToken)
        => repository.GetBookByIdAsync(id, cancellationToken);
}
```

Use `[Service]` when you need a keyed service or want to make an ambiguous signature explicit.

```csharp
public static Task<Book?> GetArchivedBookByIdAsync(
    [ID] int id,
    [Service("archive")] BookRepository repository,
    CancellationToken cancellationToken)
    => repository.GetBookByIdAsync(id, cancellationToken);
```

DataLoaders follow the same runtime-input pattern in resolver signatures. The generated interface is injected, the client never supplies it.

```csharp
public static async Task<Author?> GetAuthorAsync(
    [Parent] Book book,
    IAuthorByIdDataLoader authorById,
    CancellationToken cancellationToken)
    => await authorById.LoadAsync(book.AuthorId, cancellationToken);
```

> **Watch out:** If a service or DataLoader parameter appears as a GraphQL argument, verify that the type is registered. For keyed services or ambiguous cases, add `[Service]`. See [Service Injection](./service-injection) and [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader).

---

## Use `IResolverContext` when typed parameters are not enough

Typed parameters make resolver signatures clearer, so prefer them for arguments, parent values, services, and cancellation. Add `IResolverContext` when you need context APIs such as `ArgumentValue<T>()`, `Parent<T>()`, `Service<T>()`, `ReportError(...)`, `RequestAborted`, or global, scoped, and local state access.

Delegate resolvers commonly use `IResolverContext` because the context is the first delegate parameter.

```csharp
public sealed class BookQueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("book")
            .Argument("id", a => a.Type<NonNullType<IdType>>())
            .Resolve(async (context, cancellationToken) =>
            {
                var id = context.ArgumentValue<int>("id");
                var repository = context.Service<BookRepository>();

                return await repository.GetBookByIdAsync(id, cancellationToken);
            });
    }
}
```

Use dedicated pages for details: [Parent access](./parent-attribute), [Service Injection](./service-injection), [Parameter Attributes](./parameter-attributes), [HTTP Context and State](./ihttpcontextaccessor-and-context), [Global State](/docs/hotchocolate/v16/server/global-state), and [Result Handling](./resolver-result-handling).

---

## Recognize other bindable runtime parameters

Hot Chocolate can bind more than arguments, services, parent values, and cancellation. Keep these categories in mind when reading or designing signatures.

| Parameter category    | Examples                                                       | Guidance                                          |
| --------------------- | -------------------------------------------------------------- | ------------------------------------------------- |
| Request cancellation  | `CancellationToken`                                            | Add to async I/O resolvers and pass it through.   |
| Parent or source      | `[Parent] Book book`, `context.Parent<Book>()`                 | Use for nested object fields.                     |
| Services              | Registered service, `[Service]`, keyed service                 | Prefer method-level injection.                    |
| State                 | `[GlobalState]`, scoped state, local state, context state APIs | Use for request data shared across resolvers.     |
| Security and metadata | `ClaimsPrincipal`, path, field, selection, operation, schema   | Use for advanced infrastructure scenarios.        |
| Paging helpers        | `PagingArguments`, connection flags                            | Follow pagination examples and provider guidance. |
| Custom bindings       | `AddParameterExpressionBuilder(...)`                           | Treat as an advanced extension point.             |

Do not add `IResolverContext` to every resolver to reach these values. Typed parameters document the field inputs more clearly and make tests easier to read.

---

## Shape signatures for query middleware

Field middleware runs around the resolver and can consume or transform the resolver result. Data middleware such as paging, projections, filtering, and sorting often needs a provider-backed result shape.

Apply data middleware in this order:

```csharp
[QueryType]
public static partial class BookQueries
{
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Book> GetBooks(LibraryDbContext db)
        => db.Books;
}
```

`IQueryable<Book>` lets the provider translate paging, projection, filtering, and sorting to the backing store, commonly SQL through EF Core. Do not call `ToList()`, `ToArray()`, or other enumeration methods before middleware runs.

A client can then ask for a shaped page:

```graphql
query GetBooks {
  books(first: 10, where: { title: { contains: "GraphQL" } }) {
    nodes {
      id
      title
    }
  }
}
```

Materialized collections are valid when the data is already in memory or comes from an API that has already applied the desired shape.

```csharp
[QueryType]
public static partial class BookQueries
{
    public static IReadOnlyList<Book> GetFeaturedBooks(FeaturedBookService service)
        => service.GetFeaturedBooks();
}
```

This signature is fine for curated in-memory data. It is not the shape to choose when the goal is provider translation for paging, projection, filtering, or sorting.

`QueryContext<T>` is an alternative v16 pattern for integrated projection, filtering, and sorting. Do not combine `QueryContext<T>` with `[UseProjection]`; choose one approach. See [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections).

---

## Keep error and domain result choices explicit

Your signature should communicate whether failures are exceptional, partial, or expected domain outcomes.

| Situation                   | Signature choice                                                                            | Guidance                                                              |
| --------------------------- | ------------------------------------------------------------------------------------------- | --------------------------------------------------------------------- |
| Unexpected failure          | Throw an exception or `GraphQLException` from a resolver that returns the field value type. | Hot Chocolate reports the error and applies null propagation.         |
| Useful partial field value  | Accept `IResolverContext` and call `ReportError(...)` before returning a value.             | Use only when the field can still return meaningful data.             |
| Expected mutation failure   | Return a payload or result object such as `RenameBookPayload`.                              | Put domain errors in the payload shape.                               |
| Per-item DataLoader failure | Use `ResolverResult` in the batch layer.                                                    | Keep per-key partial errors out of the field signature when possible. |

For error details, see [Result Handling](./resolver-result-handling), [Error Handling](/docs/hotchocolate/v16/guides/error-handling), and [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader).

---

## Troubleshoot resolver signatures

| Symptom                                                                 | Likely cause                                                                                             | Fix                                                                                                | Detail                                                               |
| ----------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------- |
| A service parameter appears as a GraphQL argument.                      | The service type is not registered or Hot Chocolate cannot infer it.                                     | Register the service or use `[Service]` for keyed or ambiguous services.                           | [Service Injection](./service-injection)                             |
| A parent parameter appears as a GraphQL argument.                       | The parameter is missing `[Parent]`.                                                                     | Add `[Parent]` or use `IResolverContext.Parent<T>()`.                                              | [Parent access](./parent-attribute)                                  |
| An argument name or nullability is not what you expected.               | Nullable reference types, `[GraphQLName]`, `[DefaultValue]`, or `[ID]` do not match the intended schema. | Review the parameter type and attributes.                                                          | [Arguments](/docs/hotchocolate/v16/building-a-schema/arguments)      |
| The resolver blocks or deadlocks.                                       | Sync-over-async code is running inside the resolver.                                                     | Await async APIs. Do not call `.Result` or `.Wait()`.                                              | [Async and cancellation](#add-asynchronous-work-and-cancellation)    |
| Cancellation does not stop downstream work.                             | The token is not passed to data access or service calls.                                                 | Add `CancellationToken` and pass it to every cancellable API.                                      | [Async and cancellation](#add-asynchronous-work-and-cancellation)    |
| Paging, filtering, sorting, or projections run in memory or not at all. | The resolver materialized the data too early or middleware order is wrong.                               | Return a provider-backed `IQueryable<T>` or provider-recommended shape and check middleware order. | [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections) |
| You need `IResolverContext` everywhere.                                 | Typed parameters are not being used for common inputs.                                                   | Prefer typed parameters for arguments, parent values, services, state, and cancellation.           | [Parameter Attributes](./parameter-attributes)                       |
| `Task` or `ValueTask` returns an unexpected field value.                | The signature does not describe a result value.                                                          | Return `Task<T>` or `ValueTask<T>` for data fields.                                                | [Result Handling](./resolver-result-handling)                        |

---

## Where to go next

| Goal                                                     | Page                                                                                                                                                                                                                                                                     |
| -------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Define arguments, default values, input objects, and IDs | [Arguments](/docs/hotchocolate/v16/building-a-schema/arguments), [Parameter Attributes](./parameter-attributes)                                                                                                                                                          |
| Access parent values in nested field resolvers           | [Parent access](./parent-attribute)                                                                                                                                                                                                                                      |
| Inject services and keyed services                       | [Service Injection](./service-injection)                                                                                                                                                                                                                                 |
| Batch related data and avoid N+1                         | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader)                                                                                                                                                                                                       |
| Understand nulls, return adaptation, and result objects  | [Result Handling](./resolver-result-handling)                                                                                                                                                                                                                            |
| Add paging, projections, filtering, and sorting          | [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination), [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering), [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting) |
| Add field middleware                                     | [Field Middleware](/docs/hotchocolate/v16/execution-engine/field-middleware)                                                                                                                                                                                             |
| Handle errors and mutation payloads                      | [Error Handling](/docs/hotchocolate/v16/guides/error-handling), [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations)                                                                                                                                          |
| Access HTTP-specific data and request state              | [HTTP Context and State](./ihttpcontextaccessor-and-context), [Global State](/docs/hotchocolate/v16/server/global-state)                                                                                                                                                 |
