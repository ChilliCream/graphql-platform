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

// talk about the non-layered paging providers
// sorting expression interception
// default sorting / IsDefined!
// benefits of keyset pagination

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

Transport Layer Changes and GraphQL over HTTP Spec

HotChocolate 15

  - Focus
  - .NET 8 / 9
  - DataLoader
  - Projections Engine


* Add IBM cost analysis by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7146
* Fixed Resolver Compiler Issue When Only Having a Node Resolver by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7195
* Fixed Resolver Compiler Issue with Properties by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7196
* Add Cost Defaults for Offset Pagination by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7197
* Resolve issue where an Entity is required in subgraphs and remove non-resolvable from _entity output by @danielreynolds1 in https://github.com/ChilliCream/graphql-platform/pull/7165
* Removed legacy code from HotChocolate.Language by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7193
* Fixed websocket transport client by @PascalSenn in https://github.com/ChilliCream/graphql-platform/pull/7201
* Improve CostAnalyzer Error Details by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7202
* Set correct SDK for OpenAPI tests by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7191
* Updated specified-by section for OneOf errors by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7184
* Removed legacy paging support by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7206
* Allow GraphQLConfig.Documents to be an array by @GuilhermeScotti in https://github.com/ChilliCream/graphql-platform/pull/7205
* Moved InternalsVisibleTo to csproj files by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7210
* Reworked the handling of async results within the resolver task by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7152
* Made Cost Metrics Class Public by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7213
* Aligned Builder Structure by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7219
* Added NodeIdSerializer Service by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7220
* Fixed `where` argument coercion by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7212
* [Fusion] Correctly handle null items in ResolveByKey by @tobias-tengler in https://github.com/ChilliCream/graphql-platform/pull/7217
* Fixed Persisted Operation Naming by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7221
* Allow multiple slicing arguments when all of them are variables. by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7222
* Allow access from BCP services fusion to internals by @PascalSenn in https://github.com/ChilliCream/graphql-platform/pull/7225
* Return NotAuthenticated in case we're not authenticated by @huysentruitw in https://github.com/ChilliCream/graphql-platform/pull/6908
* Improved Execute_CoerceWhereArgument_MatchesSnapshot test by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7228
* Added type conversion in Apollo Federation ArgumentParser#TryGetValue<T> by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7229
* Reorganized Paging Packages by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7230
* Added XML Docs to Naming Conventions Interface by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7232
* Reintegrated DBContext Factory Support by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7233
* Resolver Task Cleanup Refinements by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7234
* Fixes issues with scalar directives. by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7241
* Added support for resolving nodes via an interface method by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7242
* Added support for input types of records configured with ignored optional properties  by @sunghwan2789 in https://github.com/ChilliCream/graphql-platform/pull/7239
* Removed requirement that relative URIs start with a slash by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7243
* Switched to central package management by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7240
* Added AddObjectTypeExtension methods to IRequestExecutorBuilder by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7235
* Excluded non-writable properties when inferring fields for input objects by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7236
* Ensure that executables are disposed by the execution engine. by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7244
* Removed reference to HotChocolate.Data.EntityFramework.Helpers by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7245
* Added Interface Field Inheritance by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7237
* Fixed LegacyNodeIdSerializer Registration by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7246
* Added Fusion Source Schema Package for Hot Chocolate by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7203
* Updated node ID serializers to allow internal IDs to be empty strings by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7249
* Updated providers to throw an exception instead of reporting errors by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7248
* Switched F# projects to central package management by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7250
* Fixed Cost Analyzer Span by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7251
* [Fusion] Add tests for various error cases by @tobias-tengler in https://github.com/ChilliCream/graphql-platform/pull/7006
* Reworked the GlobalIdInputValueFormatter by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7255
* Restructured GlobalIdInputValueFormater by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7256
* Upgraded System.Text.Json to 8.0.4 by @tobias-tengler in https://github.com/ChilliCream/graphql-platform/pull/7253
* Fixed Resolve Parallel SharedEntryField Tests by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7257
* Revert "Upgraded System.Text.Json to 8.0.4 (#7253)" by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7259
* Fixed issue with the error handling. by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7260
* Updated IdAttributeTests to test optional arguments and input fields by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7258
* Updated more code to use C# collection expressions (IDE0300) by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7265
* Enabled TreatWarningsAsErrors in CI, and disabled it elsewhere by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7263
* Make Cursor Key Serializers Configurable by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7266
* Fixed Issue with Field Deprecations on Type Extensions by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7270
* Removed CCN from Hot Chocolate by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7267
* Fixed issue with the operation compiler that duplicated selections. by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7278
* Reworked Handling of SelectionSetOptimizers by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7279
* Updated xunit (2.4.1 -> 2.9.0) by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7275
* Updated the CI configuration to run the jobs for draft PRs by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7277
* Fixed StrawberryShake snapshot by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7276
* Fixed issue with DL MaxBatchSize 0 not reusing the current batch object by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7274
* Fixed postgres subscription issues v14 by @PascalSenn in https://github.com/ChilliCream/graphql-platform/pull/7164
* Updated NameUtils.GetEnumValue to preserve leading underscore by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7272
* Added more cursor key serializers by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7281
* Updated more code to use C# collection expressions (IDE0301) by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7286
* Fixed issue with operation optimizers in fusion. by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7291
* Add ModifyPagingOptions by @tobias-tengler in https://github.com/ChilliCream/graphql-platform/pull/7285
* Updated QueryCacheMiddleware to not cache query results with errors by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7294
* Return false when not found for TryGetValue by @7rakir in https://github.com/ChilliCream/graphql-platform/pull/7293
* Support legacy strongly typed Ids by @tobias-tengler in https://github.com/ChilliCream/graphql-platform/pull/7238
* Introduce Connection Events to Interceptor by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7295
* Added DataLoader source generator snapshot tests by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7290
* Fixed warning CS0618 by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7299
* Fixed warning CS0067 by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7300
* Fixed warning CS8632 by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7297
* Fixed F# tests by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7296
* Removed `--property WarningLevel=0` in CI builds by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7298
* Fix off-by-one error in middleware cleanup tasks by @leddt in https://github.com/ChilliCream/graphql-platform/pull/7301
* Added Support colons in legacy GIDs by @tobias-tengler in https://github.com/ChilliCream/graphql-platform/pull/7304
* [Fusion] Add FusionGatewayBuilder.UseRequest by @tobias-tengler in https://github.com/ChilliCream/graphql-platform/pull/7283
* Added support for multiple Guid formats for global IDs  by @tobias-tengler in https://github.com/ChilliCream/graphql-platform/pull/7302
* Added CursorKeySerializer registration by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7305
* Fixed whitespace by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7318
* Enabled ImplicitUsings for all projects by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7319
* Fixed issue with abstract types in operation compiler. by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7321
* Further optimize filter expressions by @nikolai-mb in https://github.com/ChilliCream/graphql-platform/pull/7311
* Fixed SetPagingOptions by @tobias-tengler in https://github.com/ChilliCream/graphql-platform/pull/7312
* Added the ability to specify a Markdown language for snapshot segments by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7314
* Added support for IDictionary & IReadOnlyDictionary to ListTypeConverter by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7309
* Fixed WebSocket test by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7327
* Set Markdown language for generated source snapshots by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7328
* Updated the DateTime scalar to enforce a specific format by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7329
* Removed some unused snapshot files by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7326
* Fixed MongoDB test by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7330
* Added support for error filters to Fusion by @tobias-tengler in https://github.com/ChilliCream/graphql-platform/pull/7190
* Adds support for more complex order by keys in pagination by @PascalSenn in https://github.com/ChilliCream/graphql-platform/pull/7331
* Updated URLs in docs by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7334
* Removed Neo4J docs by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7336
* Fixed cycle detection in AnyType and ObjectToDictionaryConverter by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7262
* Removed Neo4J references from ExcludedCover by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7333
* Fixed Fusion compose of object beneath shared field in non-null violation case by @tobias-tengler in https://github.com/ChilliCream/graphql-platform/pull/7271
* Check in current Fusion error snapshots and unskip tests by @tobias-tengler in https://github.com/ChilliCream/graphql-platform/pull/7341
* Added test for DefaultNodeIdParser by @tobias-tengler in https://github.com/ChilliCream/graphql-platform/pull/7342
* Reintegrated Apollo Federation tests by @danielreynolds1 in https://github.com/ChilliCream/graphql-platform/pull/7339
* Updated Entities in Entity Resolver error tests to implement Node interface by @tobias-tengler in https://github.com/ChilliCream/graphql-platform/pull/7350
* Allow to incrementally adopt new ID format in distributed system by @tobias-tengler in https://github.com/ChilliCream/graphql-platform/pull/7343
* Updated extending-filtering.md by @den4ik124 in https://github.com/ChilliCream/graphql-platform/pull/7335
* Updated the Getting Started documentation by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7338
* Centralized Nullable configuration by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7332
* Fixed Apollo Federation v1 schema output by @danielreynolds1 in https://github.com/ChilliCream/graphql-platform/pull/7349
* Fixed subgraph error test failure by @tobias-tengler in https://github.com/ChilliCream/graphql-platform/pull/7351
* Improve activity reporting by @PascalSenn in https://github.com/ChilliCream/graphql-platform/pull/7354
* Adds logging blogpost by @PascalSenn in https://github.com/ChilliCream/graphql-platform/pull/7356
* Added DateTime scalar breaking change to migration guide by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7359
* Added Fusion tests for `@skip` and fixed some by @tobias-tengler in https://github.com/ChilliCream/graphql-platform/pull/7353
* Expose v14 migration guide in sidebar and make some amendments by @tobias-tengler in https://github.com/ChilliCream/graphql-platform/pull/7361
* Fixed ContinuousTask test by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7316
* Enabled nullable reference types in test projects by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7355
* Added inlining of total count when using cursor pagination. by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7366
* Reworked error behavior for SingleOrDefault. by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7371
* Changed source-generated DataLoaders to be scoped by default by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7370
* Fixed RavenDB tests by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7364
* Removed nodes field from fusion graph. by @tobias-tengler in https://github.com/ChilliCream/graphql-platform/pull/7284
* Included schema file for C# client generation (#7368) by @Socolin in https://github.com/ChilliCream/graphql-platform/pull/7369
* Fix typo in test by @tobias-tengler in https://github.com/ChilliCream/graphql-platform/pull/7373
* Updated Fusion snapshot by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7374
* Added configuration of result buffers by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7375
* Fixed issue with composite type detection on select by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7376
* Added DevContainer Config by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7379
* Updated Cookie Crumble to ensure that snapshot files end with a newline by @glen-84 in https://github.com/ChilliCream/graphql-platform/pull/7378
* Added DataLoader source generator improvements by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7377
* Disallow Generic DataLoader when using the source generator by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7381
* Added observable DataLoader by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7382
* Added DataLoader auto-caching by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7383
* Fixed generator issue that affected group and cache DataLoader by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7384
* Fixed DataLoader Generator Issues by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7385
* Added transient DataLoader state by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7387
* Fixed issue with GroupDataLoader using an extension method in the sourcegen by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7388
* Added DataLoader Projections by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7389
* Add support for bind attributes to source generator. by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7392
* Optimize dataloader fetching for lists by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7393
* Fixed Compiler Warning by @michaelstaib in https://github.com/ChilliCream/graphql-platform/pull/7394
* Add PagingOptions.IncludeNodesField option by @tobias-tengler in https://github.com/ChilliCream/graphql-platform/pull/7396
* Associate subgraph transport errors with fields by @tobias-tengler in https://github.com/ChilliCream/graphql-platform/pull/7347

## New Contributors
* @aokellermann made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/6664
* @timward60 made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/6481
* @tnc1997 made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/6699
* @PHILLIPS71 made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/6705
* @nikolai-mb made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/6711
* @meenakshi-dhanani made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/6715
* @jkonecki made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/6780
* @sunsided made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/5866
* @dariuszkuc made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/6864
* @thompson-tomo made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/6879
* @cmeeren made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/6883
* @Cheesebaron made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/6892
* @whirgod made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/6956
* @DaveRMaltby made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/6922
* @SeanTAllen made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/6988
* @timerplayer made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/7031
* @kiangkuang made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/7056
* @Pankraty made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/7065
* @DanielZuerrer made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/7078
* @Enterprize1 made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/7094
* @kasperk81 made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/6730
* @rowe-stamy made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/6995
* @McP4nth3r made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/7138
* @danielreynolds1 made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/7140
* @VaclavSir made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/6474
* @GuilhermeScotti made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/7205
* @7rakir made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/7293
* @leddt made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/7301
* @den4ik124 made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/7335
* @Socolin made their first contribution in https://github.com/ChilliCream/graphql-platform/pull/7369

**Full Changelog**: https://github.com/ChilliCream/graphql-platform/compare/13.7.0...14.0.0-rc.0
