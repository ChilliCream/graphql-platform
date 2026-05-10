# Hot Chocolate v16 first GraphQL server tutorial TOC

## Research summary

Use `.work/fullstack-workshop` as source material for the current v16 implementation patterns, not as the tutorial structure. The workshop is a full-stack course with database setup, UI, subscriptions, distributed/schema topics, deeper architecture, and production concerns. This tutorial should stay shallow and stop after the first mutation.

Useful workshop sources:

| Area | Source | Use in tutorial |
| --- | --- | --- |
| Template setup and Nitro | `.work/fullstack-workshop/docs/docs/02-Chapter 1/03-hello-world.md` | Project creation, running the server, opening `/graphql`. |
| First operation | `.work/fullstack-workshop/docs/docs/02-Chapter 1/02-first-operation.md` | Nitro workflow and first query rhythm. |
| Catalog goal and schema | `.work/fullstack-workshop/docs/docs/03-Chapter 2/01-start.md` and `.work/fullstack-workshop/src/Frontend/src/Catalog.API/Models` | Small product catalog domain and schema vocabulary. |
| Implementation-first style | `.work/fullstack-workshop/docs/docs/04-Chapter 3/01-project-styles.md` | Short explanation of `[QueryType]`, `[MutationType]`, and generated schema types. |
| Query resolvers | `.work/fullstack-workshop/src/Frontend/src/Catalog.API/Types/ProductQueries.cs` | Resolver shape, dependency injection, `PagingArguments`, service delegation. |
| Service-backed paging | `.work/fullstack-workshop/src/Frontend/src/Catalog.API/Services/ProductService.cs` | `Page<T>` from services, stable ordering, `.ToPageAsync(...)`, connection conversion. |
| Mutation basics | `.work/fullstack-workshop/docs/docs/10-Chapter 9/01-mutations.md` and `.work/fullstack-workshop/src/Frontend/src/Catalog.API/Types/ProductMutations.cs` | `Mutation` root, input object, mutation conventions, create operation. |

Exclude or only link as next steps:

- DataLoader and N+1 optimization.
- Subscriptions and realtime behavior.
- Testing chapter.
- Client calls.
- Security and authorization.
- Production preparation.
- Distributed schema, schema registry, schema layering, and architecture lessons.
- `IQueryable<T>` paging examples.
- Filtering and sorting in the main path. If mentioned, make them "learn next" links. If they are later added to this tutorial, they must use `QueryContext<T>` plus explicit Sort/Filter patterns, not `IQueryable<T>`.

## Replacement pages

The new tutorial should replace `website/src/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/` with the following pages only.

| File | Title | Purpose | Main source material |
| --- | --- | --- | --- |
| `index.md` | Build your first GraphQL server | Set scope, domain, prerequisites, and the short path from setup to mutation. State clearly that this tutorial stops at mutations. | Current tutorial landing page, workshop chapter 1 overview. |
| `01-set-up-the-project.md` | Set up the project | Install templates, create a small catalog server, run it, open Nitro, run the starter query. | Current setup page, workshop `03-hello-world.md`. |
| `02-define-your-first-types.md` | Define your first schema types | Introduce implementation-first schema design with simple `Product` and `Brand` records and the `Query` root. | Current type page, workshop catalog models, project styles page. |
| `03-write-query-resolvers.md` | Write query resolvers | Add `products` and `productById` query fields, explain selection sets, resolver names, and service/runtime parameters. | Current resolver page, workshop `ProductQueries.cs`. |
| `04-use-a-data-service.md` | Use a data service | Move in-memory catalog data behind a small DI service without teaching architecture. Add a simple argument-backed lookup. | Workshop service delegation patterns, current data page only for DI rhythm. |
| `05-add-pagination.md` | Add pagination | Replace list return values with cursor pagination using `PagingArguments`, `Page<Product>`, stable ordering, and `.ToConnection()`. Do not use `IQueryable<T>`. | Workshop `ProductService.cs`, `ProductQueries.cs`, Hot Chocolate paging docs. |
| `06-add-mutations.md` | Add mutations | Add mutation conventions, `CreateProductInput`, and a single `createProduct` mutation. Verify with a follow-up paged query. | Workshop mutation lesson and `ProductMutations.cs`. |
| `you-did-it.md` | You did it | Summarize what was built and link to deeper references for filtering, sorting, DataLoader, subscriptions, testing, clients, security, and production. | Current wrap-up page, v16 reference docs. |

## Page-level guidance

All pages should use one small catalog domain:

- `Product` with `Id`, `Name`, `Description`, and `BrandId` or `Brand`.
- `Brand` with `Id` and `Name`.
- Keep data in memory through the data service unless a page needs persistence for the mutation example. Avoid migrations, Docker, PostgreSQL, Redis, Aspire, repositories, or layers.
- Use `CatalogServer` as the project name and `CatalogServer.Types` / `CatalogServer.Services` as namespaces.
- Register the data service as a singleton so the mutation result can be queried during local tutorial runs:

```csharp
builder.Services.AddSingleton<CatalogService>();
```

- Use the v16 builder style used by the generated template:

```csharp
builder
    .AddGraphQL()
    .AddTypes()
    .AddPagingArguments()
    .AddMutationConventions();
```

Code patterns to preserve:

```csharp
[QueryType]
public static partial class Query
{
}
```

```csharp
[MutationType]
public static partial class Mutation
{
}
```

```csharp
public Task<Page<Product>> GetProductsAsync(
    PagingArguments pagingArguments,
    CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();

    var products = _products
        .OrderBy(t => t.Name)
        .ThenBy(t => t.Id)
        .ToArray();

    var first = pagingArguments.First ?? 10;
    var after = DecodeCursor(pagingArguments.After);
    var start = after is null
        ? 0
        : Array.FindIndex(products, t => t.Id == after.Value) + 1;

    var pageItems = products
        .Skip(start)
        .Take(first + 1)
        .ToArray();

    var hasNextPage = pageItems.Length > first;
    var items = pageItems
        .Take(first)
        .ToImmutableArray();

    return Task.FromResult(Page<Product>.Create(
        items,
        hasNextPage,
        start > 0,
        product => EncodeCursor(product.Id),
        products.Length));
}
```

```csharp
[UsePaging]
public static async Task<Connection<Product>> GetProductsAsync(
    PagingArguments pagingArguments,
    CatalogService catalogService,
    CancellationToken cancellationToken)
{
    var page = await catalogService.GetProductsAsync(pagingArguments, cancellationToken);

    return page.ToConnection();
}
```

If the final API pattern in the repo prefers `PageConnection<T>` over `Connection<T>`, use `PageConnection<T>` consistently instead, but keep the `Page<T>` service boundary and connection conversion.

## Files to remove from the tutorial directory

Remove these current pages because they are outside the requested scope or tied to the old wrong example:

- `00-source-code-and-checkpoints.md`
- `04-add-arguments-and-filters.md` (replaced by `04-use-a-data-service.md`)
- `05-connect-to-real-data.md` (replaced by `04-use-a-data-service.md`)
- `06-fix-n-plus-1-with-dataloader.md`
- `07-add-pagination.md` (replaced by new `05-add-pagination.md`)
- `08-add-mutations.md` (replaced by new `06-add-mutations.md`)
- `09-add-subscriptions.md`
- `10-test-your-server.md`
- `11-call-from-a-client.md`
- `12-secure-your-api.md`
- `13-prepare-for-production.md`
- `stuck.md`
