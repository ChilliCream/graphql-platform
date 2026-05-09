---
title: "Connecting to real data"
description: "Choose how Hot Chocolate v16 fields should read from databases, document stores, REST or gRPC services, and other backing systems without leaking storage details into your GraphQL schema."
---

Your data already exists, maybe in a relational database, a document store, a REST or gRPC service, a cache, a search index, or even spread across several systems.

But before you think about which provider to install, start by asking: What field should clients see, and what should that field mean?

This page guides you through connecting Hot Chocolate fields to your real application data, while keeping your schema stable, secure, and understandable. Work through these concepts before diving into provider-specific setup, such as [Entity Framework Core](/docs/hotchocolate/v16/integrations/entity-framework/), [MongoDB](/docs/hotchocolate/v16/integrations/mongodb/), [Marten](/docs/hotchocolate/v16/integrations/marten/), RavenDB, [Fetching from databases](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-databases/), or [Fetching from REST](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-rest/).

# Start with the field, not the database

Always begin with the client’s needs. Every GraphQL field should exist to solve a real client task:

```graphql
type Query {
  bookById(id: ID!): Book
  books(first: Int, after: String): BooksConnection
}

type Book {
  id: ID!
  title: String!
  author: Author!
  availability: BookAvailability!
}
```

Each of these fields answers a specific product question:

| Field | Client task | Backing data can come from |
| --- | --- | --- |
| `bookById(id)` | Show one book detail page. | EF Core, MongoDB, an upstream catalog API, or a read model. |
| `books` | Browse a bounded list of books. | A database query, a document collection, or a search-backed read model. |
| `Book.author` | Traverse from a book to its author. | A join, a document reference, a REST call, or a DataLoader batch. |
| `Book.availability` | Tell this viewer whether the book can be borrowed or purchased. | Inventory, entitlement, tenant rules, and cache data. |

Let the field’s name, arguments, return type, nullability, and error behavior describe your domain contract. The data source is an implementation detail behind the field. Remember, the [GraphQL specification defines fields as selections on types](https://spec.graphql.org/October2021/#sec-Language.Fields), not as database columns or HTTP routes.

Avoid exposing table names, collection names, endpoint paths, generated client models, or persistence DTOs. Every field you expose becomes a long-lived client dependency. It’s safer to add a field later than to remove or change one after clients have built operations, fragments, generated types, caches, and workflows around it.

**Checkpoint:** Write the field in product language before you choose a provider.

# Treat the schema as your domain model

In GraphQL, your schema is the public domain model. It should reflect the vocabulary clients use to get work done, not every internal object in your codebase.

A single domain type can combine fields from many systems:

```graphql
type Book {
  id: ID!
  title: String!
  author: Author!
  availability: BookAvailability!
  viewerCanReview: Boolean!
}
```

For example, the title might come from a catalog table, the author from another service, availability from inventory and policy data, and `viewerCanReview` from the authenticated user. The client only sees one `Book` concept, because that’s the domain shape it needs.

Use Hot Chocolate’s type configuration to shape your public contract. You can hide or rename source fields, replace them with computed fields, and configure descriptions, nullability, and middleware, all without creating a separate 1:1 DTO for every type.

Still, separate DTOs, read models, or application models are useful when they protect a real boundary:

| Use the source type as the GraphQL shape when | Use a separate model when |
| --- | --- |
| You can hide or rename unwanted members with type configuration. | The source model exposes persistence behavior or invariants that must not cross the API boundary. |
| The type already matches the public domain language. | The GraphQL contract combines several sources with an independent lifecycle. |
| The mapping layer would copy the same properties without adding ownership, validation, or safety. | You need a read model optimized for a workflow, tenant rule, or search result. |
| The team can evolve the schema without coupling clients to accidental storage details. | The model is shared with another boundary that must evolve separately. |

Your goal isn’t to minimize the number of classes. Your goal is a schema that survives storage changes. For more on modeling, see [Modeling entities vs operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/modeling-entities-vs-operations/) and [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/).

# Expose fields intentionally

Every field you add is a commitment. Each one needs a name, type, nullability rule, authorization, error handling, performance expectation, tests, documentation, and a compatibility plan.

Before exposing a source field, work through this checklist:

| Question | Why it matters |
| --- | --- |
| Which client task needs this field now? | Because a field exists in the source doesn’t mean it’s needed in the product. |
| Who may see it? | Source data often includes internal, tenant-owned, or privileged values. |
| What does `null` mean? | Missing upstream data, hidden data, and unknown data have different client behavior. |
| How expensive is it? | One selected field can trigger many database queries or upstream calls. |
| Can you support this field through the next version? | Removing or renaming a public field is a breaking change. |

Hide internal IDs, foreign keys, join tables, technical flags, status codes, and integration-specific values unless they are part of your domain contract. If clients need the information, add a replacement field with the right name and semantics.

# Put data access behind your application boundary

Resolvers should act as thin adapters. They read GraphQL arguments, receive services from dependency injection, call your application or data-access code, and return results shaped for your domain.

```csharp
// Types/BookQueries.cs
[QueryType]
public static partial class BookQueries
{
    public static async Task<Book?> GetBookByIdAsync(
        Guid id,
        BookQueriesService books,
        CancellationToken ct)
        => await books.GetBookByIdAsync(id, ct);
}
```

Here, the resolver maps the GraphQL field to a use case. The service owns the application logic:

```csharp
public sealed class BookQueriesService
{
    private readonly CatalogContext _db;

    public BookQueriesService(CatalogContext db)
    {
        _db = db;
    }

    public async Task<Book?> GetBookByIdAsync(Guid id, CancellationToken ct)
        => await _db.Books
            .Where(b => b.Id == id && b.IsPublished)
            .SingleOrDefaultAsync(ct);
}
```

Never share a single EF Core `DbContext` instance across query resolvers that might run in parallel. With the [default resolver scope](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection#default-scope), each query resolver that accepts scoped services gets its own scope, which prevents parallel operations on the same context. If you need to create contexts outside direct resolver injection (for example, in a service, DataLoader, or background component), use the `IDbContextFactory<T>` patterns described in the [Entity Framework Core integration](/docs/hotchocolate/v16/integrations/entity-framework/).

Keep these concerns out of large resolver bodies whenever possible:

| Concern | Better owner |
| --- | --- |
| Business rules and use-case branching | Application service, query object, command handler, or domain service |
| Data access, retries, connection policy, and upstream calls | Infrastructure or data-access adapter |
| Transactions | Mutation use case or application service |
| Tenant scoping and ownership checks | Authorization-aware application or data layer |
| HTTP timeouts and resilience | Typed client, `HttpClientFactory`, or infrastructure policy |
| DataLoader batch reads | Loader method delegating to an application or data-access service |

Some GraphQL-specific composition does belong near resolvers. For example, a relationship field may call a DataLoader because batching is driven by GraphQL execution. Still, the batch function should delegate real data access and business logic to code you can test without running GraphQL.

For more on service lifetimes and resolver injection, see [Dependency injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection/) and [Entity Framework Core DbContext guidance](/docs/hotchocolate/v16/integrations/entity-framework/).

# Classify each field by its behavior

Pick your data access pattern based on what the field does, not where the data lives.

| Field behavior | Good resolver shape | Common risk | Verification signal |
| --- | --- | --- | --- |
| Single entity | Read by ID or natural key through an application query. | Leaking internal IDs or source names. | Not-found, forbidden, and success paths are tested. |
| Collection | Return a bounded provider-backed query, `Connection<T>`, or application page. | Unbounded results or unstable ordering. | Representative query has a limit and deterministic order. |
| Relationship | Resolve from a parent object using DataLoader, batch resolver, provider join, or planned source query. | N+1 database queries or HTTP calls. | Logs show one planned lookup or one batched lookup per request wave. |
| Computed field | Derive a value from the parent, a service, or a batch. | Expensive work repeated for every item. | Call count is known for a list of realistic size. |
| Aggregate | Return counts, totals, summaries, status, or availability. | Pretending a report is a generic list filter. | The field has a bounded source query or service method. |
| Mutation payload | Return domain result data, validation errors, and updated objects clients need. | Transaction scope follows selected response fields instead of the command. | Mutation use case defines the consistency boundary. |
| Search | Expose search semantics with intentional arguments and ranking. | Reusing generic database filtering for search behavior. | Search order, limits, and text behavior are documented. |

You can mix all of these patterns in one schema. For example, an EF Core-backed `books` field, a REST-backed `Book.availability` field, and a DataLoader-backed `Book.author` field can all live in the same type, as long as the schema contract is coherent.

# Design list fields before exposing real data

Before you expose a collection, decide what should happen if the source contains 10,000 records. Plan your list fields up front:

| List decision | Example |
| --- | --- |
| Field name | `books`, `newReleases`, `searchBooks` |
| Default order | Newest first, title ascending, relevance score |
| Page style | Cursor connection, offset segment, fixed top N |
| Maximum size | Server default and maximum page size |
| Allowed filters | Product-level questions such as genre, status, owner, or date range |
| Allowed sorting | Stable domain sorts clients understand |
| Authorization | Tenant, owner, role, or visibility rule |
| Empty state | Empty list, `null`, or error based on domain meaning |

Paging, filtering, sorting, and projections are tools to help you implement a list, but they shouldn’t dictate your public API. Even if a provider can translate many filters, that doesn’t mean every column should become a public `where` field.

See [Pagination styles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/pagination-styles/) to choose between cursor, offset, or fixed-size list semantics. For implementation, use [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination/), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering/), [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting/), and [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections/).

# Resolve relationships without N+1 problems

GraphQL shines when clients can traverse relationships:

```graphql
query BooksWithAuthors {
  books(first: 5) {
    nodes {
      title
      author {
        name
      }
    }
  }
}
```

The execution engine walks the selected graph. If `books` returns 5 books and `Book.author` performs one lookup per book, you’ll get one list read plus 5 author reads. With 50 books, that’s 50 author reads. This is the classic N+1 problem.

Use DataLoader for repeated key-based lookups:

```csharp
// DataLoaders/AuthorDataLoaders.cs
internal static class AuthorDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<Guid, Author>> GetAuthorByIdAsync(
        IReadOnlyList<Guid> ids,
        AuthorQueriesService authors,
        CancellationToken ct)
        => await authors.GetAuthorsByIdAsync(ids, ct);
}
```

```csharp
// Types/BookNode.cs
[ObjectType<Book>]
public static partial class BookNode
{
    public static async Task<Author> GetAuthorAsync(
        [Parent] Book book,
        IAuthorByIdDataLoader authorById,
        CancellationToken ct)
        => await authorById.LoadAsync(book.AuthorId, ct);
}
```

DataLoader batches keys and deduplicates repeated loads during a single GraphQL request. It provides request-scoped batching and caching, not a durable cross-request cache.

Batch boundaries must respect tenant, authorization, cancellation, and data ownership. Never batch keys from contexts that shouldn’t see the same data. If your backing source can already fetch the relationship in one planned provider query, you may not need DataLoader for that field.

See the [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/) reference and the tutorial [Fix N+1 with DataLoader](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/06-fix-n-plus-1-with-dataloader/).

# Keep upstream models behind your schema

Treat existing systems as inputs to your schema design, not as the schema itself.

When you wrap a database, document store, REST endpoint, generated OpenAPI client, gRPC client, or legacy service, use a translation worksheet like this:

| Upstream detail | Domain decision |
| --- | --- |
| `book_rows`, `book_id`, `author_id` | Expose `Book.id` and `Book.author` if they are part of the public domain. |
| `inventory_status_code` | Replace with `availability` or a domain enum clients can act on. |
| `404` from upstream REST | Map to `null`, not-found payload data, or a GraphQL error based on the field contract. |
| Optional source property | Choose GraphQL nullability based on domain meaning, not source serializer defaults. |
| Provider-specific filter parameter | Expose a domain argument only when clients need that capability. |
| Internal tenant or partition key | Apply it server-side. Do not ask clients to pass it unless it is part of the API boundary. |

Hiding upstream details is not cosmetic. It protects clients from breaking when a table changes, an endpoint is replaced, a status code is renamed, or a field moves to another system.

Use [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/) and [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/) to map missing values, upstream failures, domain rejections, and partial data into GraphQL responses.

# Choose data middleware after you define the contract

Hot Chocolate’s data middleware lets you compose paging, projection, filtering, and sorting when your resolver’s return shape and provider support it.

```csharp
// Types/BookQueries.cs
[QueryType]
public static partial class BookQueries
{
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Book> GetBooks(CatalogContext db)
        => db.Books.Where(b => b.IsPublished);
}
```

The same EF Core scoping rule applies to `IQueryable<T>` resolvers. Middleware may compose the query later, so the underlying `DbContext` must remain valid for that resolver and must not be shared with other parallel query resolvers. Stick with the default query resolver scope, or use `AddDbContextFactory<T>` and `RegisterDbContextFactory<T>` if a factory-owned context is a better fit.

This pattern works well for provider-backed collection fields. The resolver returns a candidate query, middleware adds client-controlled shaping, and the provider decides what can translate to the backing source.

`IQueryable<T>` is a capability boundary. It lets Hot Chocolate compose expression trees before enumeration, but not every .NET expression, computed field, custom resolver, or provider behavior can translate efficiently.

Use this decision table to guide your implementation:

| Data source shape | Hot Chocolate fit | Watch for |
| --- | --- | --- |
| EF Core or another LINQ provider | `IQueryable<T>`, paging, projections, filtering, sorting, `QueryContext<T>` where appropriate | Early `ToListAsync`, unsupported translations, DbContext lifetime, authorization filters |
| MongoDB | `IExecutable<T>` through MongoDB integration and provider-specific middleware | Register matching MongoDB conventions, verify generated Mongo queries |
| Marten | LINQ with Marten integration for filtering and sorting | Provider translation capabilities and page boundaries |
| RavenDB | `IRavenQueryable<T>` from `IAsyncDocumentSession.Query<T>()`, or `IExecutable<T>` with `AsExecutable()`, plus Raven-backed paging, projections, filtering, and sorting | Register `HotChocolate.Data.Raven` middleware with `AddRavenPagingProviders`, `AddRavenProjections`, `AddRavenFiltering`, and `AddRavenSorting`; verify what RavenDB LINQ and document queries can translate |
| REST, gRPC, or generated clients | Typed services, manual paging, `Connection<T>`, DataLoader for repeated key calls | No automatic `IQueryable` translation, upstream limits, retries, timeouts |
| Search engine | Search-specific field and result type | Ranking, text semantics, filters, and pagination differ from generic list filtering |
| Mixed sources | Application service, composed resolver, DataLoader, or aggregate read model | Ownership, consistency boundary, cancellation, error mapping, observability |

For RavenDB, follow the provider-backed middleware pattern: register an `IDocumentStore` with dependency injection, add the Raven data providers to the GraphQL builder, then return `IRavenQueryable<T>` or wrap a Raven query with `AsExecutable()`. Make sure your filtering, sorting, projection, and paging expectations match what RavenDB can translate. Don’t assume every .NET expression will run server-side.

Don’t expose every possible filter or sort only because a provider can translate it. Start with your contract, then add middleware to help implement it. For more on middleware order and return shapes, see [Resolver and data middleware model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/resolver-and-data-middleware-model/).

# Plan for reliability at data boundaries

Real data brings real failure modes. Make reliability part of your field design from the start.

| Boundary concern | Design question | Where to implement |
| --- | --- | --- |
| Cancellation | Does work stop when the GraphQL request is aborted? | Pass `CancellationToken` to EF Core, MongoDB, HTTP, gRPC, and application services. |
| Timeout | How long may this field wait for its source? | Infrastructure client, application service, or hosting policy. |
| Retry | Which failures are safe to retry? | Infrastructure policy, not ad hoc resolver loops. |
| Authorization | Can this caller see every item in the batch or list? | GraphQL authorization plus data-layer tenant and ownership filters. |
| Transaction | Which writes must commit together? | Mutation use case, not the selected response tree. |
| Cost | Can clients request too much data? | Pagination limits, request limits, and cost analysis. |
| Observability | Can you see resolver timing, upstream calls, batch sizes, and errors? | Instrumentation, OpenTelemetry, logs, and Nitro where used. |

For production signals, see [Instrumentation](/docs/hotchocolate/v16/server/instrumentation/), [Performance mental model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/performance-mental-model/), [Request limits](/docs/hotchocolate/v16/securing-your-api/request-limits/), and [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis/). Use [Nitro operation monitoring](/docs/nitro/open-telemetry/operation-monitoring/) and [Nitro service monitoring](/docs/nitro/open-telemetry/service-monitoring/) for traces, resolver latency, upstream spans, errors, and client or version attribution.

For .NET service clients, see [ASP.NET Core HttpClientFactory guidance](https://learn.microsoft.com/aspnet/core/fundamentals/http-requests) and [gRPC client factory guidance](https://learn.microsoft.com/aspnet/core/grpc/clientfactory).

# Avoid common schema traps when connecting real data

| Trap | What it looks like | Safer first move | How to verify |
| --- | --- | --- | --- |
| Mirroring storage | `book_rows`, `author_id`, broad table-shaped types | Rename and reshape around client tasks | A client can explain the schema without knowing the database. |
| Exposing available fields | Every source property appears in GraphQL | Apply the field liability checklist | Each field has a use case, owner, auth rule, nullability rule, and cost expectation. |
| Creating 1:1 DTOs by reflex | Every GraphQL type has a copied DTO with the same fields | Shape the GraphQL type directly unless a boundary requires a model | Duplicated mapping disappears without leaking internal data. |
| Returning unbounded lists | `books: [Book!]!` over a large source | Add paging, stable ordering, or a narrower field | A representative query has bounded result size. |
| Putting business rules in resolvers | One resolver handles auth, retries, data access, transactions, and mapping | Move the use case to an application service | The use case can be tested without GraphQL execution. |
| Ignoring relationship fan-out | Nested fields produce one call per parent | Add DataLoader, batch resolver, or planned source query | Logs show planned call counts. |
| Materializing too early | `ToListAsync` runs before filtering, sorting, or projection | Keep the provider query shape until middleware composes, or handle shaping deliberately | Provider logs show translated operations, or docs state application-level behavior. |
| Mixing sources without rules | One field calls several systems with no timeout or error plan | Name ownership, timeout, cancellation, consistency, and error behavior | Traces show each call and the field has expected fallback behavior. |

# Plan your first real-data field

Before you implement your first production-backed field, work through this worksheet:

| Decision | Your answer |
| --- | --- |
| Field name and user need | What client task does it support? |
| Include now or defer | Is there a known client workflow? |
| Domain return type | What type and nullability express the contract? |
| GraphQL type, source type, or separate model | Can type configuration shape it, or do you need a boundary model? |
| Backing source or use case | Which service, query object, read model, or adapter owns the data? |
| Resolver shape | Single entity, collection, relationship, computed field, aggregate, search, or mutation payload? |
| Loader or middleware needs | DataLoader, batch resolver, paging, projection, filtering, sorting, manual `Connection<T>`, or none? |
| Security and tenant rules | Which caller and tenant may see which data? |
| Verification signals | Schema snapshot, successful operation, call count, latency, error mapping, authorization tests |

Once you’ve filled this out, open the implementation page that matches your field plan. Let your contract drive provider setup, not the other way around.

# Where to go next

Choose your next step based on what you need to accomplish:

| I need to... | Go to |
| --- | --- |
| Follow a guided implementation | [Connect to real data tutorial](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/05-connect-to-real-data/) |
| Batch relationship lookups | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/) and [Fix N+1 with DataLoader](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/06-fix-n-plus-1-with-dataloader/) |
| Design list fields | [Pagination styles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/pagination-styles/) and [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination/) |
| Compose provider-backed shaping | [Resolver and data middleware model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/resolver-and-data-middleware-model/), [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections/), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering/), and [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting/) |
| Configure common database providers | [Entity Framework Core](/docs/hotchocolate/v16/integrations/entity-framework/), [MongoDB](/docs/hotchocolate/v16/integrations/mongodb/), [Marten](/docs/hotchocolate/v16/integrations/marten/), and RavenDB with `HotChocolate.Data.Raven` |
| Wrap service APIs | [Fetching from REST](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-rest/) and your typed HTTP or gRPC client guidance |
| Model errors, nulls, and compatibility | [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/), [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/), [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/), and [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/) |
| Prove production behavior | [Testing GraphQL APIs](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/testing-graphql-apis/), [Security and API boundaries](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/security-and-api-boundaries/), and [Performance mental model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/performance-mental-model/) |
