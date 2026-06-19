---
title: "Migrate from GraphQL.NET"
description: "Step-by-step guide to migrating a GraphQL.NET server to Hot Chocolate."
---

This guide walks you through moving a GraphQL.NET (graphql-dotnet) server to Hot Chocolate.

# The conceptual shift

In GraphQL.NET code-first you describe your schema by writing graph type classes. A `BookType : ObjectGraphType<Book>` declares each field, its GraphQL type (`NonNullGraphType<IdGraphType>`), and a resolver delegate. The schema is the sum of those graph type objects.

Hot Chocolate flips this around. You write plain C# types (POCOs) and resolver methods, annotate the entry points with attributes (`[QueryType]`, `[ObjectType<T>]`, `[MutationType]`), and Hot Chocolate infers the schema from your C# code. There are no graph type classes to maintain. Most importantly, Hot Chocolate infers nullability from the .NET type system and nullable reference types (NRT): `string` becomes `String!`, `string?` becomes `String`, `int` becomes `Int!`. You describe the shape in idiomatic C# and the schema follows.

> [!NOTE]
> Enable nullable reference types (`<Nullable>enable</Nullable>`) before you start. Hot Chocolate reads NRT annotations to decide which fields are non-null. Migrating with NRT disabled silently flips most of your schema to nullable. This is the single biggest migration risk and is covered again under [Behavioral differences and gotchas](#behavioral-differences-and-gotchas).

# Packages and hosting

Replace the GraphQL.NET package stack with the Hot Chocolate equivalents.

**Before (GraphQL.NET)**

```xml
<PackageReference Include="GraphQL" Version="8.8.4" />
<PackageReference Include="GraphQL.SystemTextJson" Version="8.8.4" />
<PackageReference Include="GraphQL.MicrosoftDI" Version="8.8.4" />
<PackageReference Include="GraphQL.DataLoader" Version="8.8.4" />
<PackageReference Include="GraphQL.Server.Transports.AspNetCore" Version="8.3.3" />
<PackageReference Include="GraphQL.Server.Ui.GraphiQL" Version="8.3.3" />
<PackageReference Include="System.Reactive" Version="6.0.1" />
```

**After (Hot Chocolate)**

```xml
<PackageReference Include="HotChocolate.AspNetCore" Version="16.2.1" />
<PackageReference Include="HotChocolate.AspNetCore.Authorization" Version="16.2.1" />
<PackageReference Include="HotChocolate.Subscriptions.InMemory" Version="16.2.1" />
<PackageReference Include="HotChocolate.Types.Analyzers" Version="16.2.1">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

A few notes on the mapping:

- `HotChocolate.Types.Analyzers` is the source generator. It is mandatory for the annotation-based style (`[QueryType]`, `[ObjectType<T>]`, `[DataLoader]`, and the generated `.AddTypes()` call). Without it your schema is empty.
- `HotChocolate.Subscriptions.InMemory` replaces the `System.Reactive` plumbing you used for subscriptions.
- The GraphQL IDE packages (`GraphQL.Server.Ui.*`) have no equivalent. Hot Chocolate ships **Nitro** (the in-browser IDE) and serves it from the GraphQL endpoint automatically.
- There is no separate JSON package. System.Text.Json is built in, so drop `AddSystemTextJson` entirely.

The hosting calls change accordingly. `AddGraphQL` becomes `AddGraphQLServer` and `UseGraphQL` becomes `MapGraphQL`. The default endpoint path stays `/graphql`.

```diff
- builder.Services.AddGraphQL(b => b
-     .AddSchema<AppSchema>()
-     .AddSystemTextJson()
-     .AddDataLoader()
-     .AddGraphTypes()
-     .AddAuthorizationRule());
+ builder.Services
+     .AddGraphQLServer()
+     .AddAuthorization()
+     .AddAfterHotChocolateTypes()
+     .AddMutationConventions()
+     .AddInMemorySubscriptions();
```

```diff
- app.UseGraphQL("/graphql");
- app.UseGraphQLGraphiQL("/ui/graphiql");
+ app.MapGraphQL("/graphql");
```

`AddAfterHotChocolateTypes()` is the source-generated registration method (`Add{AssemblyName}Types`); it discovers every `[QueryType]`, `[ObjectType<T>]`, `[MutationType]`, `[SubscriptionType]`, and `[DataLoader]` in the assembly. Nitro is now served from `/graphql` when you open it in a browser, so the dedicated GraphiQL route is gone.

# Schema bootstrap and the query root

GraphQL.NET requires an explicit `Schema` object that wires up the query, mutation, and subscription roots, with each graph type registered in DI.

**Before (GraphQL.NET)**

```csharp
public sealed class AppSchema : GraphQL.Types.Schema
{
    public AppSchema(IServiceProvider provider, Query query, Mutation mutation, Subscription subscription)
        : base(provider)
    {
        Query = query;
        Mutation = mutation;
        Subscription = subscription;
    }
}
```

```csharp
builder.Services.AddSingleton<Query>();
builder.Services.AddSingleton<Mutation>();
builder.Services.AddSingleton<Subscription>();
builder.Services.AddSingleton<AppSchema>();
```

Hot Chocolate has no `Schema` object. You mark the root types with attributes, and `.AddTypes()` (here the generated `AddAfterHotChocolateTypes()`) merges them.

**After (Hot Chocolate)**

```csharp
[QueryType]
public static partial class Query
{
    public static IReadOnlyList<Author> GetAuthors(BookDataStore store)
        => store.GetAuthors();

    // further resolver methods ...
}
```

```csharp
builder.Services
    .AddGraphQLServer()
    .AddAfterHotChocolateTypes();
```

The class must be `partial` because the source generator emits the other half. The `Query` field name on the root comes from the method name (with the `Get` prefix stripped), so `GetAuthors` becomes the `authors` field.

# Object types and resolvers

In GraphQL.NET an object type is a class deriving from `ObjectGraphType<T>` whose constructor declares every field with an explicit GraphQL type and a resolver delegate that reads `context.Source`.

**Before (GraphQL.NET)**

```csharp
public sealed class AuthorType : ObjectGraphType<Author>
{
    public AuthorType(BookDataStore store)
    {
        Name = "Author";

        Field<NonNullGraphType<IdGraphType>>("id")
            .Resolve(context => context.Source.Id);

        Field<NonNullGraphType<StringGraphType>>("name")
            .Resolve(context => context.Source.Name);

        Field<NonNullGraphType<ListGraphType<NonNullGraphType<BookType>>>, IEnumerable<Book>>("books")
            .Resolve(context => store.GetBooksByAuthorId(context.Source.Id));
    }
}
```

In Hot Chocolate the plain `Author` POCO already provides the `id` and `name` fields (Hot Chocolate maps public properties automatically and infers nullability from their types). You only need an extension class for the `books` field that has resolver logic. Mark it `[ObjectType<Author>]`, write a resolver method, and take the parent object via `[Parent]` and services as plain parameters.

**After (Hot Chocolate)**

```csharp
public sealed class Author : ISearchResult
{
    [ID]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
```

```csharp
[ObjectType<Author>]
public static partial class AuthorType
{
    public static IReadOnlyList<Book> GetBooks(
        [Parent] Author author,
        BookDataStore store)
        => store.GetBooksByAuthorId(author.Id);
}
```

The `IResolveFieldContext` you reached for in GraphQL.NET (`context.Source`, `context.RequestServices`) is replaced by injected parameters: `[Parent] Author author` for the source object and `BookDataStore store` for a service. There is no attribute on the service parameter; Hot Chocolate resolves it from DI by type. If you need the full context, inject `IResolverContext` instead.

Note the `[GraphQLIgnore]` attribute on the `Book` POCO. Hot Chocolate exposes public properties by default, so the foreign key `AuthorId` (which should not appear in the schema) is hidden explicitly:

```csharp
// Backing foreign key used by the author DataLoader; not exposed in the schema.
[GraphQLIgnore]
public int AuthorId { get; set; }
```

# Arguments and input objects

GraphQL.NET declares arguments with `.Argument<TGraphType>("name")` and reads them with `context.GetArgument<T>("name")`. Input objects are `InputObjectGraphType<T>` classes.

**Before (GraphQL.NET)**

```csharp
Field<BookType, Book?>("bookById")
    .Argument<NonNullGraphType<IdGraphType>>("id")
    .Resolve(context =>
    {
        var id = context.GetArgument<int>("id");
        return store.GetBookById(id);
    });
```

```csharp
public sealed class BookFilterInputType : InputObjectGraphType<BookFilter>
{
    public BookFilterInputType()
    {
        Name = "BookFilterInput";

        Field<BookGenreEnum>("genre");
        Field<StringGraphType>("titleContains");
    }
}
```

In Hot Chocolate arguments are just method parameters, bound by name. Input objects are plain POCOs or records; Hot Chocolate appends the `Input` suffix automatically if it is not already present, so `BookFilter` surfaces as `BookFilterInput`. A nullable parameter (`BookFilter?`) is an optional argument.

**After (Hot Chocolate)**

```csharp
public static Book? GetBookById([ID] int id, BookDataStore store)
    => store.GetBookById(id);
```

```csharp
public static IEnumerable<Book> GetBooks(BookFilter? filter, BookDataStore store)
{
    var books = store.GetBooks().AsEnumerable();

    if (filter is not null)
    {
        if (filter.Genre is not null)
        {
            books = books.Where(b => b.Genre == filter.Genre.Value);
        }

        if (!string.IsNullOrEmpty(filter.TitleContains))
        {
            books = books.Where(b =>
                b.Title.Contains(filter.TitleContains, StringComparison.OrdinalIgnoreCase));
        }
    }

    return books.ToList();
}
```

The `BookFilter` POCO needs no graph type wrapper at all:

```csharp
public sealed class BookFilter
{
    public BookGenre? Genre { get; set; }

    public string? TitleContains { get; set; }
}
```

The nullable properties (`BookGenre?`, `string?`) become optional input fields, matching the GraphQL.NET behavior where the filter fields were not wrapped in `NonNullGraphType`.

# Scalars, ID, and enums

Hot Chocolate binds scalars implicitly from the .NET type, so the per-scalar graph types (`StringGraphType`, `IntGraphType`, `IdGraphType`) all disappear. Three differences matter when you migrate:

- **`ID` is not inferred.** GraphQL.NET surfaced `id` as `ID` because you wrote `IdGraphType`. Hot Chocolate will not infer `ID` from `int`/`string`/`Guid`; you must annotate `[ID]` on every identifier property and argument, or you get `Int`/`String`/`UUID` instead.
- **`Guid` is renamed to `UUID`.** If any of your identifiers are `Guid`, Hot Chocolate maps the `Guid` scalar to `UUID` on the wire. Use `[ID]` to keep them as `ID`, or expect the rename.
- **Enums become UPPER_SNAKE_CASE automatically.** A plain C# enum is discovered and serialized in UPPER_SNAKE_CASE, matching GraphQL.NET's CONSTANT_CASE for simple members. Verify acronyms and compound names against your existing contract.

**Before (GraphQL.NET)**

```csharp
public sealed class BookGenreEnum : EnumerationGraphType<BookGenre>
{
    public BookGenreEnum()
    {
        Name = "BookGenre";
    }
}
```

```csharp
Field<NonNullGraphType<IdGraphType>>("id")
    .Resolve(context => context.Source.Id);
```

**After (Hot Chocolate)**

```csharp
public enum BookGenre
{
    Fiction,
    Nonfiction,
    Fantasy,
    Science
}
```

```csharp
public sealed class Book : ISearchResult
{
    [ID]
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public BookGenre Genre { get; set; }

    public int PublishedYear { get; set; }
    // ...
}
```

Both apps produce the same enum values (`FICTION`, `NONFICTION`, `FANTASY`, `SCIENCE`) and the same `id: ID!` field.

# Interfaces and unions

GraphQL.NET unions derive from `UnionGraphType`, register their members with `Type<>()`, and supply a `ResolveType` delegate to map a runtime instance to its GraphQL type.

**Before (GraphQL.NET)**

```csharp
public sealed class SearchResultUnion : UnionGraphType
{
    public SearchResultUnion()
    {
        Name = "SearchResult";

        Type<BookType>();
        Type<AuthorType>();

        ResolveType = obj =>
        {
            if (obj is Book)
            {
                return PossibleTypes.First(t => t.Name == "Book");
            }

            if (obj is Author)
            {
                return PossibleTypes.First(t => t.Name == "Author");
            }

            return null;
        };
    }
}
```

In Hot Chocolate a union is a marker interface (or abstract base) annotated with `[UnionType]`. Every member implements it. There is no `ResolveType`: Hot Chocolate resolves the GraphQL type from the runtime CLR type automatically. The same applies to interfaces (`[InterfaceType]`), where `IsTypeOf` is also gone.

**After (Hot Chocolate)**

```csharp
// Marker interface that makes Book and Author members of the SearchResult union.
[UnionType("SearchResult")]
public interface ISearchResult;

public sealed class Book : ISearchResult { /* ... */ }

public sealed class Author : ISearchResult { /* ... */ }
```

The resolver simply returns the marker interface type:

```csharp
public static IEnumerable<ISearchResult> Search(string term, BookDataStore store)
{
    var results = new List<ISearchResult>();
    // add matching books and authors ...
    return results;
}
```

> [!NOTE]
> Hot Chocolate only discovers a type if it is referenced from a root or registered explicitly. When a type such as `Book` or `Author` is already reachable from the `Query` type, no extra registration is needed. If a union or interface implementer is never returned directly anywhere else in the schema, register it with `.AddType<TImplementer>()` so it appears as a possible type.

# Mutations, mutation conventions, and typed errors

A GraphQL.NET mutation is another `ObjectGraphType` with `addBook(title, authorId, genre, publishedYear): Book!`. The arguments sit directly on the field and the payload is the `Book` itself.

**Before (GraphQL.NET)**

```csharp
public sealed class Mutation : ObjectGraphType
{
    public Mutation(BookDataStore store, IBookEventService events)
    {
        Name = "Mutation";

        Field<NonNullGraphType<BookType>, Book>("addBook")
            .Argument<NonNullGraphType<StringGraphType>>("title")
            .Argument<NonNullGraphType<IdGraphType>>("authorId")
            .Argument<NonNullGraphType<BookGenreEnum>>("genre")
            .Argument<NonNullGraphType<IntGraphType>>("publishedYear")
            .Resolve(context =>
            {
                var title = context.GetArgument<string>("title");
                var authorId = context.GetArgument<int>("authorId");
                var genre = context.GetArgument<BookGenre>("genre");
                var publishedYear = context.GetArgument<int>("publishedYear");

                var book = store.AddBook(title, authorId, genre, publishedYear);
                events.PublishBookAdded(book);

                return book;
            });
    }
}
```

In Hot Chocolate the mutation is a method on a `[MutationType]` class. With `AddMutationConventions()` enabled (see the bootstrap), the arguments collapse into a single `input` and the return type is wrapped in a generated payload.

**After (Hot Chocolate)**

```csharp
[MutationType]
public static partial class Mutation
{
    // With mutation conventions enabled this surfaces as
    // addBook(input: AddBookInput!): AddBookPayload! where AddBookPayload { book: Book }.
    public static async Task<Book> AddBook(
        string title,
        [ID] int authorId,
        BookGenre genre,
        int publishedYear,
        BookDataStore store,
        ITopicEventSender eventSender,
        CancellationToken cancellationToken)
    {
        var book = store.AddBook(title, authorId, genre, publishedYear);

        await eventSender.SendAsync(
            nameof(Subscription.OnBookAdded),
            book,
            cancellationToken);

        return book;
    }
}
```

The payload shape changes on the wire. Clients must move from this:

```graphql
mutation {
  addBook(
    title: "New Book"
    authorId: "1"
    genre: SCIENCE
    publishedYear: 2024
  ) {
    id
    title
    author {
      name
    }
  }
}
```

to this:

```graphql
mutation {
  addBook(
    input: {
      title: "New Book"
      authorId: "1"
      genre: SCIENCE
      publishedYear: 2024
    }
  ) {
    book {
      id
      title
      author {
        name
      }
    }
  }
}
```

This is intentional and it is the convention that unlocks typed errors. To return a domain error instead of throwing, annotate the resolver with `[Error(typeof(MyException))]` and throw that exception; mutation conventions turn it into an `errors` field on the payload backed by a typed error union. A simple mutation does not need to declare a typed error, but the mechanism to add one is `[Error]` on the mutation method. If you do not want the reshaped payload for a given mutation, you can opt out of conventions, but you then also forgo typed `[Error]` payloads.

# Subscriptions

GraphQL.NET subscriptions own their event stream: the resolver returns an `IObservable<T>` (here an Rx `Subject`) and the mutation pushes onto that subject directly.

**Before (GraphQL.NET)**

```csharp
public sealed class Subscription : ObjectGraphType
{
    public Subscription(IBookEventService events)
    {
        Name = "Subscription";

        Field<NonNullGraphType<BookType>, Book>("onBookAdded")
            .ResolveStream(context => events.BookAdded);
    }
}
```

```csharp
// IBookEventService backed by an Rx ReplaySubject<Book>.
public void PublishBookAdded(Book book)
{
    _bookAdded.OnNext(book);
}
```

Hot Chocolate inverts this. The subscription method is marked `[Subscribe]` and receives the published message via `[EventMessage]`. The mutation publishes to a topic with `ITopicEventSender.SendAsync`. The topic name (`nameof(Subscription.OnBookAdded)`) must match on both sides, or messages are silently dropped. You no longer need the Rx subject or the event service.

**After (Hot Chocolate)**

```csharp
[SubscriptionType]
public static partial class Subscription
{
    // Topic name (nameof) must match the one used by ITopicEventSender in AddBook.
    [Subscribe]
    public static Book OnBookAdded([EventMessage] Book book) => book;
}
```

The mutation publishes with the sender shown above (`eventSender.SendAsync(nameof(Subscription.OnBookAdded), book, cancellationToken)`).

> [!NOTE]
> A subscription provider is mandatory. Hot Chocolate fails at schema build if exactly one provider is not registered. Register `.AddInMemorySubscriptions()` (suitable for development and single-server deployments). For multi-server deployments use a distributed provider such as `AddRedisSubscriptions`.

# DataLoaders

GraphQL.NET wires DataLoaders through an `IDataLoaderContextAccessor`, building or fetching a loader by string key inside the resolver.

**Before (GraphQL.NET)**

```csharp
Field<NonNullGraphType<AuthorType>, Author>("author")
    .ResolveAsync(context =>
    {
        var loader = accessor.Context!.GetOrAddBatchLoader<int, Author>(
            "GetAuthorsByIds",
            async authorIds =>
            {
                var authors = store.GetAuthorsByIds(authorIds);
                return await Task.FromResult(authors.ToDictionary(a => a.Id));
            });

        return loader.LoadAsync(context.Source.AuthorId);
    });
```

In Hot Chocolate a DataLoader is a static method annotated with `[DataLoader]`. The source generator emits a strongly typed interface (`IAuthorByIdDataLoader`) that you inject into any resolver. There is no accessor, no string key, and no manual DI registration; `.AddTypes()` registers it.

**After (Hot Chocolate)**

```csharp
public static class DataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<int, Author>> AuthorByIdAsync(
        IReadOnlyList<int> keys,
        BookDataStore store,
        CancellationToken cancellationToken)
        => await Task.FromResult(store.GetAuthorsByIds(keys).ToDictionary(a => a.Id));
}
```

```csharp
[ObjectType<Book>]
public static partial class BookType
{
    public static async Task<Author> GetAuthor(
        [Parent] Book book,
        IAuthorByIdDataLoader authorById,
        CancellationToken cancellationToken)
        => await authorById.LoadRequiredAsync(book.AuthorId, cancellationToken);
}
```

A batch (one-to-one) loader returns `Dictionary<K, V>`. For a group (one-to-many) loader, return `Dictionary<K, V[]>` instead; the array value tells Hot Chocolate it is a group loader, and you translate a GraphQL.NET `ToLookup(...)` into `GroupBy(...).ToDictionary(g => g.Key, g => g.ToArray())`. `LoadAsync` returns a real `Task` you `await` directly (replacing GraphQL.NET's `.Then(...)` chaining). Use `LoadRequiredAsync` when the value must be present, as in the `Book.author` resolver above.

> [!NOTE]
> The `keys` list is pooled and reused after the method returns. Do not capture or store it; enumerate it within the method.

# Hosting, middleware, and scoped services

The middleware pipeline is similar, but the GraphQL registration is now an endpoint (`MapGraphQL`) rather than middleware (`UseGraphQL`).

**Before (GraphQL.NET)**

```csharp
app.UseWebSockets();
app.UseAuthentication();
app.UseAuthorization();

app.UseGraphQL("/graphql");
app.UseGraphQLGraphiQL("/ui/graphiql");
```

**After (Hot Chocolate)**

```csharp
app.UseWebSockets();
app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL("/graphql");
```

The biggest behavioral change here is dependency injection scope. GraphQL.NET shares a single request scope across all sibling resolvers, so a scoped `DbContext` could be touched concurrently by parallel resolvers. Hot Chocolate creates a new scope per async resolver and per DataLoader dispatch by default, which protects non-thread-safe services. If you have code that relies on a shared per-request scoped instance, annotate the resolver with `[UseRequestScope]`, or set `DefaultQueryDependencyInjectionScope = DependencyInjectionScope.Request` globally to restore the GraphQL.NET behavior.

> [!NOTE]
> Do not constructor-inject request services into your `[QueryType]`/type classes. Those classes behave like singletons. Inject services as resolver-method parameters instead.

# Validation, depth, and cost

GraphQL.NET tunes execution limits through `AddComplexityAnalyzer` (with `MaxComplexity`, `MaxDepth`, `DefaultListImpactMultiplier`) and `.WithComplexityImpact(...)` per field.

Hot Chocolate splits this into two mechanisms:

- **Execution depth** is a validation rule: `.AddMaxExecutionDepthRule(maxAllowedExecutionDepth)`. There is also `.AddMaxAllowedFieldCycleDepthRule(...)`.
- **Cost** is static cost analysis. Configure it with `ModifyCostOptions(o => { o.MaxFieldCost = ...; o.MaxTypeCost = ...; o.EnforceCostLimits = true; })`, and annotate expensive fields with `[Cost(weight)]` or list fields with `[ListSize(...)]`.

```diff
- services.AddGraphQL(b => b.AddComplexityAnalyzer(c =>
-     {
-         c.MaxComplexity = 1000;
-         c.MaxDepth = 15;
-     }));
+ services
+     .AddGraphQLServer()
+     .AddMaxExecutionDepthRule(15)
+     .ModifyCostOptions(o =>
+     {
+         o.MaxFieldCost = 1000;
+         o.EnforceCostLimits = true;
+     });
```

Note that Hot Chocolate emits a `@cost` directive into the schema by default (visible in the exported SDL), and cost limits are enforced rather than complexity scores. GraphQL.NET has no public equivalent for custom validation rules in Hot Chocolate; rely on the built-in rules plus middleware/directives, and tune the validation surface through `ModifyValidationOptions` and `SetMaxAllowedValidationErrors(n)`.

# Errors and error filters

GraphQL.NET's `ExecutionError` is mutable, carries a `Code` and a `Data` dictionary, and auto-derives an `extensions.codes[]` chain from the exception type name. Hot Chocolate's `IError` is immutable and does none of that automatically.

- `SetCode("...")` populates `extensions.code` (singular, no `codes[]` array).
- Custom payload goes under `extensions` via `SetExtension("key", value)`. Port `error.Data["key"]` to `SetExtension("key", ...)`.
- A plain thrown exception yields a generic message with no code. To preserve a code, throw a `GraphQLException` built from an `IError`, or build the error with `ErrorBuilder` and report it via `ctx.ReportError`.

Error filters replace the unhandled-exception delegate:

```diff
- options.UnhandledExceptionDelegate = ctx => { /* map ctx.Exception */ };
+ services
+     .AddGraphQLServer()
+     .AddErrorFilter(error =>
+         error.Exception is MyDomainException
+             ? error.WithCode("MY_CODE").WithMessage("...")
+             : error);
```

You can also implement `IErrorFilter` as a class (it can take constructor dependencies). Filters run in registration order. The practical client impact: drop any reliance on `extensions.codes[]`. The same authorization denial surfaces as `ACCESS_DENIED` with a `codes[]` array in GraphQL.NET, and as a single `extensions.code` of `AUTH_NOT_AUTHENTICATED` in Hot Chocolate.

# Authorization

This is the most common authorization migration bug, so read it carefully: use the Hot Chocolate authorization attribute, `HotChocolate.Authorization.AuthorizeAttribute`, **not** `Microsoft.AspNetCore.Authorization.AuthorizeAttribute`. The Microsoft attribute silently does nothing in the Hot Chocolate pipeline.

**Before (GraphQL.NET)**

```csharp
Field<NonNullGraphType<StringGraphType>, string>("secret")
    .Resolve(context => "The cake is a lie.")
    .AuthorizeWithPolicy("Authenticated");
```

**After (Hot Chocolate)**

```csharp
using HotChocolate.Authorization;

[Authorize(Policy = "Authenticated")]
public static string GetSecret()
    => "The cake is a lie.";
```

You need authorization registered in two places: `.AddAuthorization()` on the GraphQL builder and `services.AddAuthorization(...)` for the policies themselves. The ASP.NET Core authentication/authorization middleware and your policy definitions port over unchanged.

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
});

builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddAfterHotChocolateTypes();
```

Role semantics (any-of) and stacked-policy semantics (all-of, the attribute is repeatable) match GraphQL.NET, and policies/handlers port nearly verbatim. The only handler-level change is the resource type: `IResolveFieldContext` becomes `IResolverContext`. Use `[AllowAnonymous]` (also from `HotChocolate.Authorization`) to exempt a field.

# Pagination

GraphQL.NET cursor pagination is hand-built. You declare a connection with the `Connection<...>` builder, often with custom edge/connection graph types to control the names, and you implement the slicing, cursor encoding, and `PageInfo` yourself.

**Before (GraphQL.NET)**

```csharp
// Custom edge/connection graph types give the schema the
// "BooksConnection" / "BooksEdge" names.
Connection<BookType, BooksEdgeType, BooksConnectionType>("booksConnection")
    .Bidirectional()
    .Resolve(context => ResolveBooksConnection(context, store));
```

```csharp
private static Connection<Book> ResolveBooksConnection(
    IResolveConnectionContext<object?> context,
    BookDataStore store)
{
    var ordered = store.GetBooks().OrderBy(b => b.Id).ToList();

    // ... decode after/before cursors, window the list,
    //     build Edge<Book> with EncodeCursor, build PageInfo ...

    return new Connection<Book>
    {
        Edges = edges,
        TotalCount = ordered.Count,
        PageInfo = new PageInfo { /* HasNextPage, EndCursor, ... */ }
    };
}
```

In Hot Chocolate you annotate the resolver with `[UsePaging]` and return the plain ordered sequence. Hot Chocolate builds the edges, cursors, and `pageInfo` for you. The custom edge/connection graph types are gone.

**After (Hot Chocolate)**

```csharp
// With [UsePaging] the connection is named after the field (booksConnection).
// Stable order by id so cursors are deterministic across pages.
[UsePaging]
public static IEnumerable<Book> GetBooksConnection(BookDataStore store)
    => store.GetBooks().OrderBy(b => b.Id);
```

Three differences are worth flagging:

- **Connection naming.** GraphQL.NET names the connection after the node type (`BooksConnection` here only because custom graph types were added for it). Hot Chocolate infers the connection name from the field, so `booksConnection` yields `BooksConnection`/`BooksEdge`. Tune with `InferConnectionNameFromField`/`ConnectionName` if you need a specific name.
- **Backward paging by default.** Hot Chocolate enables backward paging (`last`/`before`) by default (`AllowBackwardPagination = true`). GraphQL.NET is forward-only unless you call `.Bidirectional()`. Set `AllowBackwardPagination = false` to match a forward-only contract.
- **Cursor format is not portable.** Both styles return the same node data page-for-page, but the cursor encodings differ (GraphQL.NET uses a 1-based id cursor, `MQ==` = "1"; Hot Chocolate uses a Base64 list index, `MA==` = 0). Persisted client cursors break across the migration. Treat cursors as a fresh start. `totalCount` is also opt-in in Hot Chocolate (`IncludeTotalCount`), off by default.

# Behavioral differences and gotchas

1. **Nullability inverts.** GraphQL.NET is nullable-by-default (you opt into non-null with `NonNullGraphType<T>`). Hot Chocolate infers nullability from the C# type system and NRT (`string` -> `String!`, `string?` -> `String`, `int` -> `Int!`). NRT must be enabled. A literal port with NRT disabled silently flips most fields to nullable. This is the number one migration risk.
2. **Naming and casing are automatic.** Properties and methods become camelCase fields; on resolver methods the `Get` prefix and `Async` suffix are stripped (`GetBookByIdAsync` -> `bookById`); enum members become UPPER_SNAKE_CASE. Type names stay PascalCase. Use `[GraphQLName("...")]` to preserve an existing public name for clients.
3. **Async is the default.** There is no `FieldAsync`/`ResolveAsync` opt-in. Return `Task<T>`/`ValueTask<T>` and thread the injected `CancellationToken`.
4. **`Guid` is renamed and `ID` is not inferred.** Hot Chocolate maps `Guid` to the `UUID` scalar (a wire-visible rename) and will not infer `ID` from `int`/`string`/`Guid`. Annotate `[ID]` on every identifier (property and parameter).
5. **`DateTime` semantics differ.** GraphQL.NET's `DateTimeGraphType` is a naive `DateTime`; both `DateTime` and `DateTimeOffset` map to Hot Chocolate's offset-aware, RFC-3339 `DateTime` scalar. Expect format and precision differences; tune via `DateTimeOptions`. (`DateOnly` -> `LocalDate`, `TimeOnly` -> `LocalTime`, `byte[]` -> `Base64String`.)
6. **Mutation conventions reshape the payload.** `addBook(title, ...): Book` becomes `addBook(input: AddBookInput!): AddBookPayload!` with `AddBookPayload { book: Book }`. Client queries must change from `addBook(...) { id }` to `addBook(input: {...}) { book { id } }`. Decide per mutation; disabling conventions also forgoes typed `[Error]` payloads.
7. **Subscription provider registration is mandatory.** Hot Chocolate fails at schema build without exactly one provider. Call `AddInMemorySubscriptions()` or a distributed provider. Publishing inverts too: the mutation pushes with `ITopicEventSender.SendAsync(topic, payload)` and the topic name must match `nameof(Subscription.Method)`, or messages are silently dropped.
8. **Error shape differs.** Hot Chocolate has no `extensions.codes[]` and no auto-derived code from the exception type. `SetCode` populates `extensions.code` only; custom payload goes under `extensions` via `SetExtension(...)`; a plain thrown exception yields a generic message with no code. Port `error.Data[...]` to `SetExtension(...)` and drop any client reliance on `codes[]`.
9. **Connection naming, cursor format, and backward paging differ.** Hot Chocolate infers the connection name from the field (not the node type), enables backward paging by default, and uses an incompatible cursor encoding. Persisted client cursors break; treat them as a fresh start.
10. **DI scope is per-resolver, not shared per-request.** Hot Chocolate creates a new scope per async resolver and per DataLoader dispatch by default. Code relying on a shared per-request scoped instance needs `[UseRequestScope]` or `DefaultQueryDependencyInjectionScope = Request`. Do not constructor-inject request services into singleton type classes.
11. **GraphQL-over-HTTP response defaults differ.** Hot Chocolate follows the spec: default content-type `application/graphql-response+json` and non-200 status codes for errors. Many GraphQL.NET clients expect `application/json` and always-200. If clients break, set `AddHttpResponseFormatter(new HttpResponseFormatterOptions { HttpTransportVersion = HttpTransportVersion.Legacy })` or have clients send `Accept: application/json`. Hot Chocolate also restricts GET to query operations and disables introspection outside Development by default.

# Schema-first option

Hot Chocolate can author schemas SDL-first as well, but this guide leads with the annotation-based style because it is the closest fit for a code-first GraphQL.NET app. GraphQL.NET's `Schema.For(sdl)` has no direct one-to-one replacement: migrate schema-first apps to annotation-based types and diff the generated SDL (`GET /graphql?sdl`) against your original schema as a contract check. See the [defining a schema](../defining-a-schema/index.md) documentation for the available authoring styles.

# API mapping summary

| GraphQL.NET API                                                           | Hot Chocolate API                                                                      |
| ------------------------------------------------------------------------- | -------------------------------------------------------------------------------------- |
| `new Schema { Query = ... }` / `Schema.For(sdl)`                          | `services.AddGraphQLServer()` + `.AddTypes()`; no `Schema` object                      |
| `class BookType : ObjectGraphType<Book>`                                  | Plain POCO `Book`; extra fields via `[ObjectType<Book>]` partial class                 |
| `Field<T>("name").Resolve(ctx => ...)`                                    | Public method on the `[QueryType]`/`[ObjectType<T>]` class                             |
| `FieldAsync` / `.ResolveAsync(...)`                                       | Async by default: return `Task<T>`/`ValueTask<T>`; `Async` suffix stripped             |
| `.Argument<TGraphType>("id")` + `ctx.GetArgument<T>("id")`                | Plain method parameter `T id` (bound by name)                                          |
| `ctx.RequestServices.GetRequiredService<T>()`                             | Inject `T` as a method parameter                                                       |
| `ctx.Source` (cast manually)                                              | `[Parent] T parent` parameter                                                          |
| `StringGraphType`, `IntGraphType`, ...                                    | Implicit binding from the .NET type                                                    |
| `IdGraphType`                                                             | `[ID]` on the property/parameter (not inferred)                                        |
| `Guid` scalar                                                             | `UUID` scalar (use `[ID]` to keep `ID`)                                                |
| `EnumerationGraphType<TEnum>`                                             | Plain C# enum (UPPER_SNAKE_CASE, auto-discovered)                                      |
| `class XInputType : InputObjectGraphType<X>`                              | Plain POCO/record parameter (`Input` suffix appended)                                  |
| `InterfaceGraphType<T>` + `ResolveType`/`IsTypeOf`                        | `[InterfaceType]` C# interface; resolution by CLR type; `AddType<>` implementers       |
| `UnionGraphType` + `Type<>()` + `ResolveType`                             | `[UnionType]` marker interface; members implement it; `AddType<>` unreferenced members |
| `class M : ObjectGraphType` + `Schema.Mutation`                           | `[MutationType] partial class` (auto-merged)                                           |
| (hand-written payload + `errors`)                                         | `.AddMutationConventions()` + `[Error(typeof(Ex))]`                                    |
| `Field<T>(...).ResolveStream(ctx => IObservable<T>)`                      | `[SubscriptionType]` + `[Subscribe]` + `[EventMessage]`                                |
| `accessor.Context.GetOrAddBatchLoader<K,V>(...)`                          | `[DataLoader]` static method; inject generated `I{Name}DataLoader`                     |
| `GetOrAddCollectionBatchLoader` (`ILookup`)                               | `[DataLoader]` returning `Dictionary<K, V[]>` (group loader)                           |
| `app.UseGraphQL("/graphql")`                                              | `app.MapGraphQL("/graphql")`                                                           |
| `GraphQL.Server.Ui.*` + `UseGraphQLGraphiQL()`                            | Built-in Nitro served by `MapGraphQL()`                                                |
| `.AddSystemTextJson()` / `.AddNewtonsoftJson()`                           | Built in (do not port the serializer call)                                             |
| `ExecutionError` + `Code` + `Data`                                        | `IError` (immutable); `SetCode` -> `extensions.code`; `SetExtension(...)`              |
| `UnhandledExceptionDelegate`                                              | `.AddErrorFilter(error => ...)` or `IErrorFilter`                                      |
| `.Authorize()`/`.AuthorizeWithPolicy()` / Microsoft `[Authorize]`         | `HotChocolate.Authorization.[Authorize]` + `.AddAuthorization()`                       |
| `AddComplexityAnalyzer(c => { MaxDepth })`                                | `.AddMaxExecutionDepthRule(n)`                                                         |
| `AddComplexityAnalyzer(c => { MaxComplexity })`                           | `ModifyCostOptions(o => { MaxFieldCost, EnforceCostLimits })` + `[Cost]`               |
| `Connection<T>(name)` builder + hand-built `Connection`/`Edge`/`PageInfo` | `[UsePaging]`; framework builds edges/cursors/`pageInfo`                               |
