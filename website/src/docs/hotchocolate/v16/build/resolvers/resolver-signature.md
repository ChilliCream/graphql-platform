---
title: "Resolver Signatures"
---

A resolver signature defines the C# boundary for a single GraphQL field. It specifies which client arguments Hot Chocolate should read, which runtime values to inject, how to handle asynchronous operations, and how to adapt the result for the GraphQL response.

Start with the simplest signature that clearly describes the field. Only add arguments, parent values, services, cancellation tokens, context, or specialized return types when the field requires them.

## What you will learn

- The differences between the public GraphQL field contract and the C# resolver signature.
- Which resolver parameters become GraphQL arguments and which are handled by Hot Chocolate at runtime.
- How to intentionally add arguments, services, DataLoaders, cancellation, and `IResolverContext`.
- How to select the right return type for synchronous values, asynchronous values, collections, data middleware, paging, and payload objects.
- How to avoid common signature issues, including unintended schema arguments, sync-over-async problems, and premature query materialization.

---

## Start with a complete resolver example

Consider the following resolver, which exposes a nullable field called `bookById`. Here, `BookRepository` is assumed to be registered with dependency injection.

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

This signature results in the following GraphQL field:

```graphql
type Query {
  bookById(id: ID!): Book
}
```

| Signature part                        | Source                                   | Schema effect                                                          |
| ------------------------------------- | ---------------------------------------- | ---------------------------------------------------------------------- |
| `[ID] int id`                         | Client argument named `id`               | Exposed as a non-null `ID` argument.                                   |
| `BookRepository repository`           | Registered service                       | Not exposed in the schema.                                             |
| `CancellationToken cancellationToken` | Request cancellation provided by runtime | Not exposed in the schema.                                             |
| `Task<Book?>`                         | Resolver result                          | Hot Chocolate awaits the task and allows the field value to be `null`. |

The following sections introduce each concept individually.

---

## What does the resolver signature control?

The GraphQL field contract is the public API visible to clients. It defines the field name, argument names and types, return type, nullability, and any schema directives or middleware effects.

The C# resolver signature is the implementation boundary that Hot Chocolate invokes at runtime. Its parameters may come from client arguments, the parent value, registered services, request state, or execution metadata. The return value can be a completed value, an awaitable, a queryable source, a paged connection, or a domain payload.

```text
GraphQL field
  arguments       -> ordinary resolver parameters and argument attributes
  parent value    -> [Parent] or IResolverContext.Parent<T>()
  services        -> registered service parameters or [Service]
  request state   -> CancellationToken, IResolverContext, state, metadata
  resolver result -> field middleware -> GraphQL response value or error
```

A parameter is treated as a GraphQL argument unless Hot Chocolate recognizes it as a runtime binding. Registered services, `[Parent]` values, `CancellationToken`, `IResolverContext`, state parameters, and similar framework types are runtime inputs, not client arguments.

---

## Start with the simplest useful resolver

Use synchronous resolvers when the value is already in memory, the computation is inexpensive, and no I/O is required.

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

Use `T` when the field always returns a value. Use `T?` when a missing value is a valid outcome. Avoid wrapping CPU-only mapping in `Task.Run` to force asynchronous behavior.

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

When `BookSearchService` is registered, it is treated as a runtime input. The remaining parameters are exposed as client arguments.

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

| Signature shape                                    | Schema effect                          | Details                                                                               |
| -------------------------------------------------- | -------------------------------------- | ------------------------------------------------------------------------------------- |
| `string title`                                     | `title: String!`                       | [Arguments](/docs/hotchocolate/v16/build/schema-elements/arguments)                   |
| `string? title`                                    | `title: String`                        | [Nullability](/docs/hotchocolate/v16/build/schema-elements/lists-and-non-null)        |
| `[DefaultValue(10)] int limit` or `int limit = 10` | Default value appears in the schema    | [Arguments](/docs/hotchocolate/v16/build/schema-elements/arguments)                   |
| `[GraphQLName("name")] string username`            | Public argument is `name`              | [Parameter Attributes](./parameter-attributes)                                        |
| `[ID] int id`                                      | Public argument uses the `ID` scalar   | [Relay and IDs](/docs/hotchocolate/v16/build/schema-elements/relay)                   |
| `[ID<Product>] int id`                             | ID is restricted to the `Product` type | [Relay and IDs](/docs/hotchocolate/v16/build/schema-elements/relay)                   |
| `SearchBooksInput input`                           | Input object argument                  | [Input Object Types](/docs/hotchocolate/v16/build/schema-elements/input-object-types) |

> **Note:** Registered service types and framework types like `CancellationToken` are not GraphQL arguments. If one appears in your schema, review the binding guidance in [Service Injection](./service-injection) and [Parameter Attributes](./parameter-attributes).

---

## Add asynchronous work and cancellation

Use `Task<T>` for typical I/O operations, such as database queries, HTTP requests, file access, or DataLoader calls.

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

Hot Chocolate provides the `CancellationToken` via `IResolverContext.RequestAborted`. Always pass this token to downstream APIs that support cancellation, including EF Core, HTTP clients, service methods, and DataLoader `LoadAsync` calls.

`ValueTask<T>` is also supported. Use it if the called API already returns `ValueTask<T>` or if performance measurements show a benefit for hot paths. In most cases, prefer `Task<T>` for asynchronous resolver code, as it is easier to compose and less prone to misuse.

Plain `Task` and `ValueTask` (without a type parameter) are supported for delegate shapes, but they do not represent a useful field value. Use them only when the field or middleware contract intentionally has no result. Data fields should return explicit value shapes such as `Task<Book?>`, `Task<IReadOnlyList<Book>>`, or `Task<Connection<Book>>`.

> **Caution:** Avoid calling `.Result`, `.Wait()`, or other sync-over-async patterns inside resolvers. Always await asynchronous APIs and pass the cancellation token through.

---

## Select the appropriate return shape

Choose a return type that matches the data source and how Hot Chocolate should process the result after the resolver returns.

| Return shape                                | Use when                                                                        | Signature notes                                                                               | Details                                                                                              |
| ------------------------------------------- | ------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------- |
| `T`                                         | The value is already available.                                                 | Good for mapping and inexpensive derived fields.                                              | [Result Handling](./resolver-result-handling)                                                        |
| `T?`                                        | The field can be missing.                                                       | Align C# nullability with GraphQL nullability.                                                | [Nullability](/docs/hotchocolate/v16/build/schema-elements/lists-and-non-null)                       |
| `IEnumerable<T>`, `IReadOnlyList<T>`, array | The items are already materialized.                                             | Query middleware may run in memory or may not translate to the provider.                      | [Result Handling](./resolver-result-handling)                                                        |
| `Task<T>`                                   | The resolver performs asynchronous I/O.                                         | Preferred general asynchronous shape.                                                         | [Async and cancellation](#add-asynchronous-work-and-cancellation)                                    |
| `ValueTask<T>`                              | The called API returns `ValueTask<T>` or measurement supports it.               | Avoid as the default for ordinary asynchronous code.                                          | [Result Handling](./resolver-result-handling)                                                        |
| `IQueryable<T>`                             | Paging, projection, filtering, or sorting should translate to a query provider. | Do not enumerate before middleware runs.                                                      | [Fetching from Databases](/docs/hotchocolate/v16/_leagcy/resolvers-and-data/fetching-from-databases) |
| `IExecutable<T>`                            | A Hot Chocolate data provider executable should own query execution.            | Provider-specific. Follow the provider documentation for construction and middleware support. | [Result Handling](./resolver-result-handling)                                                        |
| `IAsyncEnumerable<T>`                       | Values are produced asynchronously.                                             | Confirm the field's middleware and transport behavior before using it as a list shape.        | [Result Handling](./resolver-result-handling)                                                        |
| `Connection<T>`                             | The resolver constructs a cursor paging result.                                 | Common with custom paging services and external APIs.                                         | [Pagination](/docs/hotchocolate/v16/build/pagination)                                                |
| `QueryContext<T>`                           | You use the v16 integrated projection, filtering, and sorting pattern.          | Do not combine with `[UseProjection]`.                                                        | [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options)         |
| Payload or result object                    | A mutation returns data plus expected domain errors.                            | Keep domain failures in the payload shape instead of throwing for expected cases.             | [Mutations](/docs/hotchocolate/v16/build/schema-elements/operations-mutations)                       |

Treat `IExecutable<T>` and `IAsyncEnumerable<T>` as specialized return types. Hot Chocolate supports them, but correct usage depends on the provider, middleware stack, and transport requirements. Consult provider documentation or the result handling page before using them widely.

---

## Add parent values while keeping the field contract clear

Nested field resolvers often require access to the object produced by the parent field. Use `[Parent]` to make this runtime input explicit.

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

`[Parent] Book book` is not a GraphQL argument. It represents the `Book` value already resolved for the current object. If you are writing a delegate resolver or need context-based access, use `context.Parent<T>()`.

See [Parent access](./parent-attribute) for more details on working with parent values.

---

## Add services and DataLoaders as runtime inputs

In Hot Chocolate v16, registered services are inferred automatically. Place services directly in the resolver method signature and keep application logic outside the resolver.

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

Use `[Service]` if you need a keyed service or want to clarify an ambiguous signature.

```csharp
public static Task<Book?> GetArchivedBookByIdAsync(
    [ID] int id,
    [Service("archive")] BookRepository repository,
    CancellationToken cancellationToken)
    => repository.GetBookByIdAsync(id, cancellationToken);
```

DataLoaders follow the same pattern: the generated interface is injected as a runtime input, never supplied by the client.

```csharp
public static async Task<Author?> GetAuthorAsync(
    [Parent] Book book,
    IAuthorByIdDataLoader authorById,
    CancellationToken cancellationToken)
    => await authorById.LoadAsync(book.AuthorId, cancellationToken);
```

> **Caution:** If a service or DataLoader parameter appears as a GraphQL argument, ensure the type is registered. For keyed services or ambiguous cases, add `[Service]`. See [Service Injection](./service-injection) and [DataLoader](/docs/hotchocolate/v16/build/dataloader).

---

## Use `IResolverContext` when typed parameters are not sufficient

Typed parameters make resolver signatures clearer, so use them for arguments, parent values, services, and cancellation whenever possible. Add `IResolverContext` only when you need access to context APIs such as `ArgumentValue<T>()`, `Parent<T>()`, `Service<T>()`, `ReportError(...)`, `RequestAborted`, or global, scoped, or local state.

Delegate resolvers often use `IResolverContext` because the context is the first parameter.

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

For more details, see: [Parent access](./parent-attribute), [Service Injection](./service-injection), [Parameter Attributes](./parameter-attributes), [HTTP Context and State](./ihttpcontextaccessor-and-context), [Global State](/docs/hotchocolate/v16/build/server-configuration/global-state), and [Result Handling](./resolver-result-handling).

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

Avoid adding `IResolverContext` to every resolver solely to access these values. Typed parameters make field inputs clearer and improve test readability.

---

## Shape signatures for query middleware

Field middleware wraps the resolver and can consume or transform its result. Data middleware, such as paging, projections, filtering, and sorting, often requires a provider-backed result shape.

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

Using `IQueryable<Book>` allows the provider to translate paging, projection, filtering, and sorting to the backing store, typically SQL via EF Core. Do not call `ToList()`, `ToArray()`, or enumerate the data before middleware runs.

A client can then request a shaped page:

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

Materialized collections are appropriate when the data is already in memory or comes from an API that has already applied the desired shape.

```csharp
[QueryType]
public static partial class BookQueries
{
    public static IReadOnlyList<Book> GetFeaturedBooks(FeaturedBookService service)
        => service.GetFeaturedBooks();
}
```

This signature works well for curated in-memory data, but is not suitable when you want provider translation for paging, projection, filtering, or sorting.

`QueryContext<T>` is an alternative v16 pattern for integrated projection, filtering, and sorting. Do not combine `QueryContext<T>` with `[UseProjection]`; select one approach. See [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options) for more information.

---

## Make error and domain result choices explicit

Your resolver signature should indicate whether failures are exceptional, partial, or expected domain outcomes.

| Situation                   | Signature choice                                                                            | Guidance                                                              |
| --------------------------- | ------------------------------------------------------------------------------------------- | --------------------------------------------------------------------- |
| Unexpected failure          | Throw an exception or `GraphQLException` from a resolver that returns the field value type. | Hot Chocolate reports the error and applies null propagation.         |
| Useful partial field value  | Accept `IResolverContext` and call `ReportError(...)` before returning a value.             | Use only when the field can still return meaningful data.             |
| Expected mutation failure   | Return a payload or result object such as `RenameBookPayload`.                              | Place domain errors in the payload shape.                             |
| Per-item DataLoader failure | Use `ResolverResult` in the batch layer.                                                    | Keep per-key partial errors out of the field signature when possible. |

For more on error handling, see [Result Handling](./resolver-result-handling), [Error Handling](/docs/hotchocolate/v16/_leagcy/guides/error-handling), and [DataLoader](/docs/hotchocolate/v16/build/dataloader).

---

## Troubleshooting resolver signatures

| Symptom                                                                 | Likely cause                                                                                             | Solution                                                                                           | More info                                                                                    |
| ----------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| A service parameter appears as a GraphQL argument.                      | The service type is not registered or Hot Chocolate cannot infer it.                                     | Register the service or use `[Service]` for keyed or ambiguous services.                           | [Service Injection](./service-injection)                                                     |
| A parent parameter appears as a GraphQL argument.                       | The parameter is missing `[Parent]`.                                                                     | Add `[Parent]` or use `IResolverContext.Parent<T>()`.                                              | [Parent access](./parent-attribute)                                                          |
| An argument name or nullability is not what you expected.               | Nullable reference types, `[GraphQLName]`, `[DefaultValue]`, or `[ID]` do not match the intended schema. | Review the parameter type and attributes.                                                          | [Arguments](/docs/hotchocolate/v16/build/schema-elements/arguments)                          |
| The resolver blocks or deadlocks.                                       | Sync-over-async code is running inside the resolver.                                                     | Await async APIs. Do not call `.Result` or `.Wait()`.                                              | [Async and cancellation](#add-asynchronous-work-and-cancellation)                            |
| Cancellation does not stop downstream work.                             | The token is not passed to data access or service calls.                                                 | Add `CancellationToken` and pass it to every cancellable API.                                      | [Async and cancellation](#add-asynchronous-work-and-cancellation)                            |
| Paging, filtering, sorting, or projections run in memory or not at all. | The resolver materialized the data too early or middleware order is wrong.                               | Return a provider-backed `IQueryable<T>` or provider-recommended shape and check middleware order. | [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options) |
| You need `IResolverContext` everywhere.                                 | Typed parameters are not being used for common inputs.                                                   | Prefer typed parameters for arguments, parent values, services, state, and cancellation.           | [Parameter Attributes](./parameter-attributes)                                               |
| `Task` or `ValueTask` returns an unexpected field value.                | The signature does not describe a result value.                                                          | Return `Task<T>` or `ValueTask<T>` for data fields.                                                | [Result Handling](./resolver-result-handling)                                                |

---

## Next steps

| Goal                                                     | Page                                                                                                                                                                                                                                                                                                                        |
| -------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Define arguments, default values, input objects, and IDs | [Arguments](/docs/hotchocolate/v16/build/schema-elements/arguments), [Parameter Attributes](./parameter-attributes)                                                                                                                                                                                                         |
| Access parent values in nested field resolvers           | [Parent access](./parent-attribute)                                                                                                                                                                                                                                                                                         |
| Inject services and keyed services                       | [Service Injection](./service-injection)                                                                                                                                                                                                                                                                                    |
| Batch related data and avoid N+1                         | [DataLoader](/docs/hotchocolate/v16/build/dataloader)                                                                                                                                                                                                                                                                       |
| Understand nulls, return adaptation, and result objects  | [Result Handling](./resolver-result-handling)                                                                                                                                                                                                                                                                               |
| Add paging, projections, filtering, and sorting          | [Pagination](/docs/hotchocolate/v16/build/pagination), [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options), [Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types), [Sorting](/docs/hotchocolate/v16/build/filtering-sorting-projections/sort-types) |
| Add field middleware                                     | [Field Middleware](/docs/hotchocolate/v16/build/execution-engine/field-middleware)                                                                                                                                                                                                                                          |
| Handle errors and mutation payloads                      | [Error Handling](/docs/hotchocolate/v16/_leagcy/guides/error-handling), [Mutations](/docs/hotchocolate/v16/build/schema-elements/operations-mutations)                                                                                                                                                                      |
| Access HTTP-specific data and request state              | [HTTP Context and State](./ihttpcontextaccessor-and-context), [Global State](/docs/hotchocolate/v16/build/server-configuration/global-state)                                                                                                                                                                                |
