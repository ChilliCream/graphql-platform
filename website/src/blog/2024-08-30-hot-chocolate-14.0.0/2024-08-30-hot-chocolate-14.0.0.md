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

Another area where we have made significant improvements is in query inspection. With Hot Chocolate 14, it’s now incredibly simple to check which fields are being requested within the resolver without the need for complex syntax tree traversals. You can now formulate a pattern with the GraphQL selection syntax and let the executor inject a simple boolean that tells you if your pattern matched the user query.

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

The patterns also support inline fragments to match abstract types. However, even with these complex patterns, it can be beneficial to write your own traversal logic without dealing with complex trees. For this, you can now simply inject the resolver context and use our fluent selector inspection API.

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

If you want to go all in and have the full power of the operation executor, you can still inject `ISelection` and traverse the compiled operation tree.

## Pagination

Pagination is a common requirement in GraphQL APIs, and Hot Chocolate 14 makes it easier than ever to implement, no matter if you are building layered applications or using `DbContext` right in your resolver.

For layered application patterns like DDD, CQRS, or Clean Architecture, we have built a brand new paging API that is completely separate from the Hot Chocolate GraphQL core. When building layered applications, pagination should be a business concern and handled in your repository or services layer. Doing so brings some unique concerns, like how the abstraction of a page looks. For this, we have introduced a couple of new primitives like `Page<T>`, `PagingArguments`, and others that allow you to build your own paging API that fits your needs and interfaces well with GraphQL.

We have also implemented keyset pagination for Entity Framework Core, which you can use in your infrastructure layer. The Entity Framework team is planning to have, at some point, a paging API for keyset pagination natively integrated into EF Core (LINK). Until then, you can use our API to get the best performance out of your EF Core queries when using pagination.

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

We are focusing on keyset pagination because it’s the better way to do pagination, as performance is constant per progression to the pages, as opposed to growing linearly with offset pagination. Apart from the better performance, keyset pagination also allows for stable pagination results even if the underlying data changes.

We also worked hard to allow for pagination in your DataLoader. In GraphQL, where nested pagination is a common requirement, having the capability to batch multiple nested paging requests into one database query is essential.

Let’s assume we have the following query and we are using a layered architecture approach.

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

Let's assume we have the following two resolvers for the above query, fetching the brands and the products.

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

With the above resolvers, the execution engine would first call the `BrandService`, and then for each `Brand`, it would call the `ProductService` to get the products per brand. This would lead to an N+1 query problem within our GraphQL server. To solve this, we can use a DataLoader within our `ProductService` and batch the product requests.

To enable this, we have worked extensively on DataLoader and now support stateful DataLoader. This means we can pass on state to a DataLoader separate from the keys. If we were to peek into the `ProductService`, we would see something like this:

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

The `ToBatchPageAsync` extension would rewrite the paging query so that each `brandId` would be a separate page, allowing us to make one database call to get, in this case, 10 products per brand for 10 brands.

An important aspect of keyset pagination is maintaining a stable order, which requires a unique key at the end. In the above case, we order by `Name` and then chain the primary key `Id` at the end. This ensures that the order remains stable even if the `Name` is not unique.

> If you want to read more about keyset pagination, you can do so [here](LINK).

We have brought the same capabilities to non-layered applications, where you now have a new paging provider for EF Core that allows for transparent keyset pagination.

So if you are doing something like this in your resolver:

```csharp
[UsePaging]
public static async IQueryable<Brand> GetBrands(
    PagingArguments pagingArguments,
    CatalogContext context)
    => context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id);
```

By default, this would emulate cursor pagination by using `skip/take` underneath. However, as I mentioned, we now have a new keyset pagination provider for EF Core that you can opt-in to. It's not the default, by the way, as it is not compatible with SQLite.

```csharp
builder.Services
    .AddGraphQLServer()
    ...
    .AddDbContextCursorPagingProvider();
```

But what about user-controlled sorting? The above example would fall apart when using `[UseSorting]`, as we could not guarantee that the order is stable. To address this, we have added a couple of helpers to the `ISortingContext` that allow you to manipulate the sorting expression.

```csharp
[UsePaging]
[UseSorting]
public static async IQueryable<Brand> GetBrands(
    CatalogContext context,
    ISortingContext sorting)
{
    // this signals that the expression was not handled within the resolver
    // and the sorting middleware should take over.
    sorting.Handled(false);

    sorting.OnAfterSortingApplied<IQueryable<Brand>>(
        static (sortingApplied, query) =>
        {
            if (sortingApplied && query is IOrderedQueryable<Brand> ordered)
            {
                return ordered.ThenBy(b => b.Id);
            }

            return query.OrderBy(b => b.Id);
        });

    return context.Brands;
}
```

With the `ISortingContext`, we now have a hook that is executed after the user sorting has been applied. This allows us to append a stable order to the user sorting. Typically, this could be generalized and moved into a user extension method to make the resolver look cleaner.

```csharp
[UsePaging]
[UseSorting]
public static async IQueryable<Brand> GetBrands(
    CatalogContext context,
    ISortingContext sorting)
{
    sorting.AppendStableOrder(b => b.Id);
    return context.Brands;
}
```

You even could go further and bake it into a custom middleware.

```csharp
[UsePaging]
[UseCustomSorting]
public static async IQueryable<Brand> GetBrands(
    CatalogContext context,
    ISortingContext sorting)
    => context.Products;
```

With the new paging providers, we now also inline the total count into the database query that slices the page, meaning you have a single call to the database. The paging middleware will inspect what data is actually needed and either fetch the page and the total count in one database query, just the page if the total count is not needed, or just the total count if the page is not needed. All of this is built on top of the new `IsSelected` query inspection API.

## DataLoader

Let's talk about `DataLoader`. As we already touched on how `DataLoader` is now more flexible with pagination, what's underneath is the new state that can be associated with `DataLoader`. Since `DataLoader` can be accessed from multiple threads concurrently and also be dispatched at multiple points during execution, you have unreliable state that can be used when it's available but should not cause the `DataLoader` to fail. However, you can also have state that is used to branch a `DataLoader`, where the state is guaranteed within that branch.

Let me give you some examples. In the following example, we are fetching brands for ID 1 and 2. We also provide some state when we ask for brand 2. The state is guaranteed to be there when I fetch the second brand, but it could be there for the first brand—this all depends on the dispatcher.

```csharp
var task1 = brandById.LoadAsync(1);
var task2 = brandById.SetState("some-state", "some-value").LoadAsync(2);
Task.WaitAll(task1, task2);
```

However, in some cases like paging, I want the state to be guaranteed. In these cases, we can branch a `DataLoader`, and into this branch, we pass in some data that make up the context of this branch.

```csharp
var branch = brandById
  .Branch("SomeKey")
  .SetState("some-state", "some-value");

var task1 = branch.LoadAsync(1);
var task2 = branch.LoadAsync(2);
Task.WaitAll(task1, task2);
```

When we look at paging, for instance, we use the paging arguments to create a branch key. So, whenever you pass in the same paging arguments, you will get the same branch. This allows us to batch the paging requests for the same paging arguments.

```csharp
productsByBrandId.WithPagingArguments(args).LoadAsync(brandId, ct);
```

We also use the same state mechanism for `DataLoader` with projections.

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

You can pass an `ISelection` into the `DataLoader`. Any selection that is structurally equivalent will point to the same `DataLoader` branch and be batched together. We can even add to that state things we might want to include on top, like elements we want to be guaranteed when fetching the entity.

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

From the `DataLoader` side, we can inject these selections and apply them to our queryable.

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

When using our `DataLoader` projections, we are utilizing a new projection engine that is separate from `HotChocolate.Data`, and we are using this to redefine what projections are in Hot Chocolate. This is why `IsProjectedAttribute` is not supported. Instead, we have modified the `ParentAttribute` to specify requirements.

```csharp
public static class ProductExtensions
{
    [UsePaging]
    public static async Task<Connection<Product>> GetProductsAsync(
        [Parent(nameof(Brand.Id))] Brand brand,
        PagingArguments pagingArguments,
        ProductService productService,
        CancellationToken cancellationToken)
        => await productService.GetProductsByBrandAsync(brand.Id, pagingArguments, cancellationToken).ToConnectionAsync();
}
```

The optional argument on the `ParentAttribute` specifies a selection set that describes the requirements for the parent object. In the example above, it defines that the brand ID is required. However, you could also specify that you need the IDs of the products as well, such as `Id Products { Id }`. The parent we inject is guaranteed to have the properties filled with the required data. We evaluate this string in the source generator, and if it does not match the object structure, it would yield a compile-time error. The whole `DataLoader` projections engine is marked as experimental, and we are looking for feedback.

Apart from this, we have invested a lot into `GreenDonut` to ensure that you can use the source-generated `DataLoader` without any dependencies on `HotChocolate`. Since `DataLoader` is ideally used between the business layer and the data layer and is transparent to the REST or GraphQL layer.

With Hot Chocolate 14, you can now add the `HotChocolate.Types.Analyzers` package and the `GreenDonut` package to your data layer. The analyzers package is just the source generator and will not be a dependency of your package. We will generate the `DataLoader` code plus the dependency injection code for registering your `DataLoader`. You simply need to add the `DataLoaderModuleAttribute` to your project like the following:

```csharp
[assembly: DataLoaderModule("CatalogDataLoader")]
```

Lastly, on the topic of DataLoader we have made the DataLoader cache observable allowing you to share entities between DataLoader for even more efficient caching. Lets for say that we have two brand DataLoader, one fetches the entity by id and the other one by name.

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

    [DataLoader]
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

Lastly, on the topic of `DataLoader`, we have made the `DataLoader` cache observable, allowing you to share entities between `DataLoader` instances for even more efficient caching. Let's say that we have two brand `DataLoader` instances: one fetches the entity by ID, and the other one by name.

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

This can be easily done by writing two observer methods that create a new cache lookup for the same object. So, at the moment one of the `DataLoader` instances is instantiated, it will subscribe for `Brand` entities on the cache and create lookups. After that, the `DataLoader` will receive real-time notifications if any other `DataLoader` has fetched a `Brand` entity and will be able to use the cached entity.

Where this really shines is with optional includes. For instance, when using the `BrandByIdDataLoader`, we could include the products in one request because we know that we will need them.

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

In this case, we can subscribe to `Brand` entities on the cache and check if they have the products list populated. If they do, we can create lookups for the products.

## Source Generators

With Hot Chocolate 14, we have started to expand our use of source-generated code. We have already used source generators in the past to automatically register types or generate the boilerplate code for `DataLoader`. With Hot Chocolate 14, we are now beginning to use source generators to generate resolvers. This feature is opt-in and, at the moment, only available for our new type extension API.

The new `ObjectType<T>` attribute will, over the next few versions, replace the `ExtendObjectType` attribute. The new attribute works only in combination with the source generator and combines the power of the implementation-first approach with the code-first fluent API.

```csharp
[ObjectType<Brand>]
public static partial class BrandNode
{
    static partial void Configure(IObjectTypeDescriptor<Brand> descriptor)
    {
        descriptor.Ignore(t => t.Subscriptions);
    }

    [UsePaging]
    public static async Task<Connection<Product>> GetProductsAsync(
        [Parent] Brand brand,
        PagingArguments pagingArguments,
        ProductService productService,
        CancellationToken cancellationToken)
        => await productService.GetProductsByBrandAsync(brand.Id, pagingArguments, cancellationToken).ToConnectionAsync();
}
```

The beauty of the source generator is that, in contrast to expression compilation, the results are fully inspectable, and we can guide you by issuing compile-time warnings and errors. The source generator output can be viewed within your IDE and is debuggable.

IMAGE

With the new type extension API, we also allow for new ways to declare root fields and colocate queries, mutations, and subscriptions.

```csharp
public static class Operations
{
    [Query]
    public static async Task<Connection<Brand>> GetBrandsAsync(
        BrandService brandService,
        PagingArguments pagingArgs,
        CancellationToken ct)
        => await brandService.GetBrandsAsync(pagingArgs, ct);

    [Mutation]
    public static async Task<Brand> CreateBrand(
        CreateBrandInput input,
        BrandService brandService,
        CancellationToken ct)
        => await brandService.CreateBrandAsync(input, ct);
}
```

Operation fields can also be colocated into extension types.

```csharp
[ObjectType<Brand>]
public static partial class BrandNode
{
    static partial void Configure(IObjectTypeDescriptor<Brand> descriptor)
    {
        descriptor.Ignore(t => t.Subscriptions);
    }

    [UsePaging]
    public static async Task<Connection<Product>> GetProductsAsync(
        [Parent] Brand brand,
        PagingArguments pagingArguments,
        ProductService productService,
        CancellationToken cancellationToken)
        => await productService.GetProductsByBrandAsync(brand.Id, pagingArguments, cancellationToken).ToConnectionAsync();

    [Query]
    public static async Task<Connection<Brand>> GetBrandsAsync(
        BrandService brandService,
        PagingArguments pagingArgs,
        CancellationToken ct)
        => await brandService.GetBrandsAsync(pagingArgs, ct);

    [Mutation]
    public static async Task<Brand> CreateBrand(
        CreateBrandInput input,
        BrandService brandService,
        CancellationToken ct)
        => await brandService.CreateBrandAsync(input, ct);
}
```

This allows for more flexibility in addition to the already established `QueryTypeAttribute`, `MutationTypeAttribute`, and `SubscriptionTypeAttribute`.

With the new version of Hot Chocolate, we are also introducing a new type extension for interfaces, which allows you to introduce base resolvers for common functionality. Think of this like base classes.

```csharp
public interface IEntity
{
    [ID] int Id { get; }
}

[InterfaceType<IEntity>]
public static partial class EntityInterface
{
    public static string SomeField([Parent] IEntity entity)
        => ...;
}
```

The field definition and the resolver are inherited by all implementing object types. So, if an object type does not declare `someField`, it will inherit the resolver from the interface declaration.

This API is also available through the fluent API, where you now have `Resolve` descriptors on interface fields.

## Relay Support

With Hot Chocolate 14, we have also improved our Relay support. We have made it easier to integrate aggregations into the connection type and to add custom data to edges. You now have more control over the shape of the connection type, allowing you to disable the `nodes` field—either to remove it as unnecessary or to replace it with a custom field.

Additionally, we have reworked the node ID serializers to be extendable and support composite identifiers.

```csharp
EXAMPLE NODE ID SERIALIZER REGISTRATION
```

The new serializer is more efficient and aligns better with the ID serialization format of other GraphQL servers, where the encoded ID has the following format: `{TypeName}:{Id}`.

The new serializer still allows for the old format to be passed in, and you can also register the legacy serializer if you prefer the way we handled it before.

Relay remains the best GraphQL client library, with others still trying to catch up by copying Relay concepts. We have always been very vocal about this and use Relay as our first choice in customer projects. Relay is a smart GraphQL client that would immensely benefit from a feature called fragment isolation, where an error in one fragment would not cause the erasure of data from a colocated fragment.

The issue here is that the GraphQL specification defines that if a non-null field either returns null or throws an error, the selection set is erased, and the error is propagated upwards. This is a problem for Relay because it would cause the erasure of data from colocated fragments.

We have been working on a solution to this problem for years now within the GraphQL foundation, and Hot Chocolate has implemented, in past versions, a proposal called CCN (Client-Controlled-Nullability) where the user could change the nullability of fields.

However, there is now a new push called the true-nullability proposal, which allows smart clients to simply disable null bubbling. In this case, a smart client could create a sort of fragment isolation on the client side by only deleting the fragment affected by an error or non-null violation.

With Hot Chocolate 14, we have decided to remove CCN and add a new HTTP header `hc-disable-null-bubbling` that allows you to disable null bubbling for a request. This is a first step towards true-nullability, which would also introduce a new semantic nullability kind.

We have prefixed the header with `hc-` to signal that this is a Hot Chocolate-specific header and to avoid collision with the eventual GraphQL specification.

## Data

- Executable / Cosmos Driver / EF Driver

## Query Errors

## Transport

- Semantic Routes
- Variable Batching
- Transport Layer Changes
- GraphQL over HTTP Spec
- Fixed NotAuthenticated
- Variable and Request Batching

## Security

- Cost Analysis
- Introspection

## Fusion

- Source Schema Package
- Composite Schema Specification

## Client

- HotChocolate.Transport

## GraphQL Cockpit

- OpenTelemetry/BCP
- Schema Registry

## Community

  * Further optimize filter expressions by @nikolai-mb in https://github.com/ChilliCream/graphql-platform/pull/7311
  * DevContainer
  * Azure Data API Builder

## Documentation and Courses

  - DomeTrain Course

## Hot Chocolate 15

  - Focus
  - .NET 8 / 9
  - DataLoader
  - Projections Engine
