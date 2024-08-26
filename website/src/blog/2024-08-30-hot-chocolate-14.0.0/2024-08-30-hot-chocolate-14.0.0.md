---
path: "/blog/2024/08/30/new-in-hot-chocolate-14"
date: "2024-08-30"
title: "What's new for Hot Chocolate 14"
tags: ["hotchocolate", "graphql", "dotnet", "aspnetcore"]
featuredImage: "hot-chocolate-14-banner.png"
author: Michael Staib
authorUrl: https://github.com/michaelstaib
authorImageUrl: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
---

We are almost ready to release a new major version of Hot Chocolate, and with it come many exciting new features. We have been working on this release for quite some time, and we are thrilled to share it with you. In this blog post, we will give you a sneak peek at what you can expect with Hot Chocolate 14.

In this post, I will be focusing on the Hot Chocolate server, but we have also been busy working on Hot Chocolate Fusion and the Composite Schema Specification. We will be releasing more information on these projects in the coming weeks.

## Ease of use

We have focused on making Hot Chocolate easier to use and more intuitive. To achieve this, we have added many new features that will simplify your work. This will be apparent right from the start when you begin using Hot Chocolate 14. One major area where you can see this improvement is in dependency injection. Hot Chocolate 13 was incredibly flexible in this area, allowing you to configure which dependencies could be used by the GraphQL execution engine with multiple resolvers simultaneously, and to specify which services the execution engine needed to synchronize or pool. While this was a powerful feature, it could be somewhat complex to use, especially when incorporating DataLoader into the mix.

You either ended up with lengthy configuration code that essentially re-declared all dependencies, or you ended up with very cluttered resolvers.

With Hot Chocolate 14, we have simplified this process by putting dependency injection on auto-pilot. Now, when you write your resolvers, you can simply inject services without needing to explicitly tell Hot Chocolate that they are services.

```csharp
public static IQueryable<Session> GetSessions(
    ApplicationDbContext context)
    => context.Sessions.OrderBy(s => s.Title);
```

This leads to clearer code that is more understandable and easier to maintain. For instance, the resolver above injects the `ApplicationDbContext`. There is no need to tell Hot Chocolate that this is a service or what characteristics this service has; it will just work. This is because we have simplified the way Hot Chocolate interacts with the dependency injection system.

In GraphQL, we essentially have two execution algorithms. The first, used for queries, allows for parallelization to optimize data fetching. This enables us to enqueue data fetching requests transparently and execute them in parallel. The second algorithm, used for mutations, is a sequential algorithm that executes one mutation after another.

So, how is this related to DI? In Hot Chocolate 14, if we have an async resolver that requires services from the DI, we create a service scope around it, ensuring that the services you use in the resolver are not used concurrently by other resolvers. Since query resolvers are, by specification, defined as side-effect-free, this is an excellent default behavior where you as the developer can just focus on writing code without concerning your self with concurrency.

For mutations, the situation is different, as mutations inherently cause side effects. For instance, you might want to use a shared DbContext between two mutations. When execution mutations Hot Chocolate will use the default request scope as its guaranteed that when a mutation is execution only this mutation resolver is really running for the operation that is being executed.

So, the new default execution behavior is much more opinionated but leads to an easier default experience. However, we recognize that there are reasons to maybe use the request DI scope everywhere and you can, as you can configure the default DI scope behavior with the default schema options.

EXAMPLE.

Also, you can override the default, whatever your default may be on a per resolver basis.

EXAMPLE.

TODO : Mention DataLoader DI handling!!!

## Query Inspection

Another area where we have made significant improvements is in query inspection. With Hot Chocolate 14, its now super simple to check what fields are being requested within the resolver without the need for complex syntax tree traversals. You now can formulate a pattern with the GraphQL selection syntax and let the executer inject you with a simple boolean that tells you if your pattern matched the user query.

```csharp
public sealed class BrandService(CatalogContext context)
{
    public async Task<Brand> GetBrandAsync(
        int id,
        [IsSelected("products { details }")]
        bool includeProductDetails,
        CancellationToken ct = default)
    {
        var query = context.Brands
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id);

        if(includeProductDetails)
        {
            query = query.Include(t => t.Products.Details);
        }

        return await query.FirstOrDefaultAsync(ct);
    }
}
```

The patterns also support inline fragments to match abstract types. But even with these complex patterns, sometimes its just great if you can write your own traversal logic but without complex trees. For this you can now simple inject the resolver context and use our fluent selector inspection API.

```csharp
public sealed class BrandService(CatalogContext context)
{
    public async Task<Brand> GetBrandAsync(
        int id,
        IResolverContext context,
        CancellationToken ct = default)
    {
        var query = context.Brands
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id);

        if(context.Select("products").IsSelected(details))
        {
            query = query.Include(t => t.Products.Details);
        }

        return await query.FirstOrDefaultAsync(ct);
    }
}
```

If you want to go full in and have all the power of the operation executor then you still can inject `ISelection` and traverse the compiled operation tree.

## Pagination

Pagination is a common requirement in GraphQL APIs, and Hot Chocolate 14 makes it easier than ever to implement no matter if you are building layered applications or if you are using DbContext right in your resolver.

For layered application patterns like DDD, CQRS or Clean Architecture, we have built a brand new paging API that is completely separate from the HotChocolate GraphQL core. When building layered application pagination should be a business concern and handled in your repository or services layer. Doing so brings some unique concerns with, like how the abstraction of a page looks like. For this we have introduced a couple of new primitives like `Page<T>`, `PagingArguments` and others that allow you to build your own paging API that fits your needs and interfaces well with GraphQL.

We also have implemented keyset pagination for Entity Framework core which you can use in your infrastructure layer. The Entity Framework team is planing to have at some point a paging API for keyset pagination natively integrated into EF Core (LINK). Until then you can use our API to get the best performance out of your EF Core queries when using pagination.

```csharp
public sealed class BrandService(CatalogContext context)
{
    public async Task<Page<Brand>> GetBrandsAsync(
        PagingArguments args,
        CancellationToken ct = default)
        => await context.Brands
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToPageAsync(args, ct);
}
```

We are focusing on keyset pagination because its the better way to do pagination as performance is constant per progression to the pages as opposed to growing linearly with offset pagination. Apart form the better performance keyset pagination also allows to have stable pagination result even if the underlying data changes.

We also worked hard to allow for pagination in your DataLoader. In GraphQL where nested pagination is a common thing having the capability to in essence batch multiple nested paging request in one database query is essential.

Lets assume we have the following query and we are using a layered architecture approach.

```graphql
query GetBrands {
  brands(first: 10) {
    nodes {
      id
      name
      products(first: 10) {
        nodes {
          id
          name
        }
      }
    }
  }
}
```

Let's assume we have the following two resolvers for the above query fetching the brands and the products.

```csharp
[UsePaging]
public static async Task<Connection<Brand>> GetBrandsAsync(
    PagingArguments pagingArguments,
    BrandService brandService,
    CancellationToken cancellationToken)
    => await brandService.GetBrandsAsync(pagingArguments, cancellationToken).ToConnectionAsync();


[UsePaging]
public static async Task<Connection<Product>> GetProductsAsync(
    [Parent] Brand brand,
    PagingArguments pagingArguments,
    ProductService productService,
    CancellationToken cancellationToken)
    => await productService.GetProductsByBrandAsync(brand.Id, pagingArguments, cancellationToken).ToConnectionAsync();
```

With the above resolvers the execution engine would call first the `BrandService` and then for each `Brand` would call the `ProductService` to get the products per brand. This would lead to a N+1 query problem within our GraphQL server. To solve this we can use a `DataLoader` within our `ProductService` and batch the product requests.

To allow this we have worked a lot on `DataLoader` and now support stateful DataLoader. This means we can pass on state to a `DataLoader` separate from the keys. If we would peek into the `ProductService` we would see something like this:

```csharp
public async Task<Page<Product>> GetProductsByBrandAsync(
    int brandId,
    PagingArguments args,
    CancellationToken ct = default)
    => await productsByBrandId.WithPagingArguments(args).LoadAsync(brandId, ct);
```

Our DataLoader in this case would look like the following:

```csharp
public sealed class ProductDataLoader
{
    [DataLoader]
    public static async Task<Dictionary<int, Page<Product>>> GetProductsByBrandIdAsync(
        IReadOnlyList<int> keys,
        PagingArguments pagingArguments,
        CatalogContext context,
        CancellationToken ct)
        => await context.Products
            .AsNoTracking()
            .Where(p => keys.Contains(p.BrandId))
            .OrderBy(p => p.Name).ThenBy(p => p.Id)
            .ToBatchPageAsync(t => t.BrandId, pagingArguments, ct);
}
```

The `ToBatchPageAsync` extension would rewrite the paging query so that each `brandId` would be a separate page allowing us to do one database call to get in this case 10 products per brand for 10 brands.

Important in keyset pagination is a stable order, that needs to have a unique key at the end. In the above case we order by `Name` and then chain in at the end the primary key `Id`. This ensures that the order is stable even if the `Name` is not unique.

> If you want to read more about keyset pagination you can do so [here](LINK).

We have brought the same capabilities also to non-layered applications where you now have for EF Core a new paging provider that allows for transparent keyset pagination.

So if you are doing something like this in your resolver:

```csharp
[UsePaging]
public static async IQueryable<Brand> GetBrandsAsync(
    PagingArguments pagingArguments,
    CatalogContext context)
    => context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id);
```

Then by default we would emulate cursor pagination by using skip/take underneath. However, as I said we have a new keyset pagination provider for EF core that you now can opt-in to. Its not the default btw as it is not compatible with SQLite.

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()


```






// talk about the non-layered paging providers
// sorting expression interception
// default sorting / IsDefined!
// benefits of keyset pagination
// cursor key serialization
// * Added inlining of total count when using cursor pagination. by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7366

```csharp
public static class BrandNode
{
    public static async Task<Brand?> GetBrandByIdAsync(
        [Parent] Brand brand,
        PagingArguments args,
        BrandByIdDataLoader brandById,
        CancellationToken cancellationToken)
        => await brandById
            .Select(selection)
            .WithPagingArguments(args)
            .LoadAsync(id, cancellationToken);
}
```

## DataLoader

/ ContextData
/ DataLoader auto-caching


```csharp
internal static class BrandDataLoader
{
    [DataLoader]
    public static async Task<Dictionary<int, Brand>> GetBrandByIdAsync(
        IReadOnlyList<int> ids,
        CatalogContext context,
        CancellationToken ct)
        => await context.Brands
            .AsNoTracking()
            .Where(t => ids.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, ct);
}
```

```csharp
internal static class BrandDataLoader
{
    [DataLoader(Lookups = [nameof(CreateBrandByIdLookup)])]
    public static async Task<Dictionary<int, Brand>> GetBrandByIdAsync(
        IReadOnlyList<int> ids,
        CatalogContext context,
        CancellationToken ct)
        => await context.Brands
            .AsNoTracking()
            .Where(t => ids.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, ct);

    private static int CreateBrandByIdLookup(Brand brand) => brand.Id;

    [DataLoader(Lookups = [nameof(CreateBrandByNameLookup)])]
    public static async Task<Dictionary<string, Brand>> GetBrandByNameAsync(
        IReadOnlyList<string> names,
        CatalogContext context,
        CancellationToken ct)
        => await context.Brands
            .AsNoTracking()
            .Where(t => names.Contains(t.Name))
            .ToDictionaryAsync(t => t.Name, ct);

    private static string CreateBrandByNameLookup(Brand brand) => brand.Name;
}
```

```csharp
public sealed class BrandService(CatalogContext context)
{
    public async Task<Page<Brand>> GetBrandByIdAsync(
        PagingArguments args,
        BrandByIdDataLoader brandById,
        CancellationToken ct = default)
        => await brandById
            .AsNoTracking()
            .Include(b => b.Products)
            .ToPageAsync(args, ct);
}
```

```csharp
internal static class ProductDataLoader
{
    [DataLoader(Lookups = [nameof(CreateProductByIdLookups)])]
    public static async Task<Dictionary<int, Product>> GetProductByIdAsync(
        => ...

    private static IEnumerable<KeyValuePair<int, Product>> CreateProductByIdLookups(Brand brand)
      => brand.Products.Select(p => new KeyValuePair<int, Product>(p.Id, p));
}
```

## Projections

// GreenDonut
// Isolated Code Generation for layered code.

```csharp
internal static class BrandDataLoader
{
    [DataLoader(Lookups = [nameof(CreateBrandByIdLookup)])]
    public static async Task<Dictionary<int, Brand>> GetBrandByIdAsync(
        IReadOnlyList<int> ids,
        CatalogContext context,
        ISelectorBuilder selector,
        CancellationToken ct)
        => await context.Brands
            .AsNoTracking()
            .Select(selector, key: b => b.Id)
            .ToDictionaryAsync(b => b.Id, ct);
}
```

```csharp
public class Query
{
    public async Task<Brand?> GetBrandByIdAsync(
        int id,
        ISelection selection,
        BrandByIdDataLoader brandById,
        CancellationToken cancellationToken)
        => await brandById
            .Select(selection)
            .LoadAsync(id, cancellationToken);
}
```

```csharp
public class Query
{
    public async Task<Brand?> GetBrandByIdAsync(
        int id,
        ISelection selection,
        BrandByIdDataLoader brandById,
        CancellationToken cancellationToken)
        => await brandById
            .Select(selection)
            .Include(b => b.Products)
            .LoadAsync(id, cancellationToken);
}
```

## Source Generators

## Query Errors

Interface Resolver

// from ExtendObjectType to ObjectType<T>

NodeIdSerializer (composite identifiers)

Security
  - no introspection
  - cost and stuff

Ease of use

Layering

DataLoader

Fusion

Composite Schema Specification

Source Schema Package

Community

  * Further optimize filter expressions by @nikolai-mb in https://github.com/ChilliCream/graphql-platform/pull/7311
  * DevContainer

Source Generators

IsSelected

Root Fields [Query, Mutation, Subscription]

OpenTelemetry/BCP

GraphQL Semantic Operation Routes
  - persisted operations and more

Variable and Request Batching

Cost Analysis

Schema Registry

DomeTrain Course

Executable / Cosmos Driver / EF Driver

Azure Data API Builder

HotChocolate.Transport

Transport Layer Changes and GraphQL over HTTP Spec / Fixed NotAuthenticated

Null Bubbling Mode and CCN

HotChocolate 15

  - Focus
  - .NET 8 / 9
  - DataLoader
  - Projections Engine
