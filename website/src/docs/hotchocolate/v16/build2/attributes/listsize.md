---
title: ListSize attribute
---

`[ListSize]` adds Hot Chocolate list-size metadata to a GraphQL field. Cost analysis uses that metadata before resolvers run to estimate how many list items a selection can produce.

Use `[ListSize]` when a field returns a list and Hot Chocolate cannot infer a safe size from paging metadata. The attribute affects static cost analysis. It does not page, truncate, validate, or change resolver results.

```csharp
using HotChocolate.CostAnalysis.Types;

[QueryType]
public static partial class CatalogQueries
{
    [ListSize(AssumedSize = 25)]
    public static IReadOnlyList<Product> GetFeaturedProducts(ProductService products)
        => products.GetFeaturedProducts();
}
```

In this example, cost analysis estimates `featuredProducts` as 25 `Product` items. Your resolver still decides how many products are returned, so keep the value aligned with a server-side cap.

# Let paging metadata cover paged fields

Standard `[UsePaging]` fields usually receive `@listSize` metadata automatically when cost defaults are enabled. For cursor paging, Hot Chocolate can generate metadata like this:

```graphql
products(first: Int, after: String, last: Int, before: String): ProductsConnection
  @listSize(
    assumedSize: 50
    slicingArguments: ["first", "last"]
    slicingArgumentDefaultValue: 10
    sizedFields: ["edges", "nodes"]
    requireOneSlicingArgument: false
  )
```

The generated values come from paging options:

| Directive argument            | Typical source                                                          |
| ----------------------------- | ----------------------------------------------------------------------- |
| `assumedSize`                 | `MaxPageSize`, default `50`                                             |
| `slicingArguments`            | `first` and `last` for cursor paging, or `take` for offset paging       |
| `slicingArgumentDefaultValue` | `DefaultPageSize`, default `10`, when enabled in cost options           |
| `sizedFields`                 | `edges` and `nodes` for connections, or `items` for collection segments |
| `requireOneSlicingArgument`   | `RequirePagingBoundaries`, default `false`                              |

Prefer tuning paging options when the generated metadata should match your paging contract. Add an explicit `[ListSize]` to a paged field when you need to override the generated metadata.

# Add a size to an unpaged list

Use `AssumedSize` for a list with a known maximum size.

```csharp
using HotChocolate.CostAnalysis.Types;

[QueryType]
public static partial class CatalogQueries
{
    [ListSize(AssumedSize = 10)]
    public static IReadOnlyList<Product> GetNewestProducts(ProductService products)
        => products.GetNewestProducts(take: 10);
}
```

Without list-size metadata, an unknown list is estimated as size `1`. That can understate the cost of nested selections. Use paging instead of an unpaged list when clients can request large or unbounded result sets.

# Size a list from field arguments

Use `SlicingArguments` when a numeric field argument controls the result size.

```csharp
using HotChocolate.CostAnalysis.Types;

[QueryType]
public static partial class CatalogQueries
{
    [ListSize(
        AssumedSize = 100,
        SlicingArguments = ["limit"],
        SlicingArgumentDefaultValue = 20,
        RequireOneSlicingArgument = false)]
    public static IReadOnlyList<Product> GetTopProducts(
        int? limit,
        ProductService products)
        => products.GetTopProducts(limit ?? 20);
}
```

With this configuration:

- `topProducts(limit: 5)` is estimated as 5 items.
- If the schema argument has an integer default value, the analyzer can use that value.
- If no slicing value is available, `SlicingArgumentDefaultValue = 20` estimates 20 items.
- If a query passes a variable and the static analyzer cannot know the runtime value, `AssumedSize = 100` is the fallback.
- If several slicing arguments have usable values, the analyzer uses the maximum value.

Set these values to the same limits your resolver, validation rules, or service layer enforce. An optimistic estimate can let expensive queries pass. A conservative estimate can reject valid queries.

# Apply parent size to child list fields

Connection fields return connection objects. The lists are child fields such as `edges` and `nodes`. `SizedFields` tells cost analysis that the parent field's slicing arguments size those child lists.

You usually do not need this on standard `[UsePaging]` fields because Hot Chocolate generates it. Use it when you override generated values or build a custom connection-like field.

```csharp
using HotChocolate.CostAnalysis.Types;

[QueryType]
public static partial class CatalogQueries
{
    [UsePaging(MaxPageSize = 100, DefaultPageSize = 25)]
    [ListSize(
        AssumedSize = 100,
        SlicingArguments = ["first", "last"],
        SlicingArgumentDefaultValue = 25,
        SizedFields = ["edges", "nodes"],
        RequireOneSlicingArgument = false)]
    public static IQueryable<Product> GetProducts(CatalogContext db)
        => db.Products.OrderBy(product => product.Id);
}
```

# Combine list size with cost weights

`[Cost]` and `[ListSize]` describe different parts of the same cost calculation:

- `[Cost]` sets a weight for a field, argument, input, or type.
- `[ListSize]` sets the estimated item count that multiplies nested selections.

```text
products(first: 20) uses @listSize
  nodes is estimated as 20 items
    selected child field cost is multiplied by 20
```

Use `[Cost]` for expensive work and `[ListSize]` for list cardinality. For full cost budgets and request headers, see [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis).

# Use descriptor-based configuration when attributes do not fit

If you define schema fields with descriptors, configure list size on the field descriptor instead of using an attribute.

```csharp
using HotChocolate.Types;

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("featuredProducts")
            .Resolve(context => context.Service<ProductService>().GetFeaturedProducts())
            .ListSize(assumedSize: 25);
    }
}
```

A full descriptor configuration can set the same metadata as the attribute:

```csharp
descriptor
    .Field("topProducts")
    .Argument("limit", argument => argument.Type<IntType>())
    .Resolve(context =>
    {
        var limit = context.ArgumentValue<int?>("limit") ?? 20;
        return context.Service<ProductService>().GetTopProducts(limit);
    })
    .ListSize(
        assumedSize: 100,
        slicingArguments: ["limit"],
        sizedFields: [],
        requireOneSlicingArgument: false,
        slicingArgumentDefaultValue: 20);
```

Use descriptor-based configuration when the field is not backed by a method or property, when you centralize schema configuration in `ObjectType` classes, or when schema conventions should own this metadata. Use schema options and paging options when you want generated metadata to change across many fields.

# Reference the generated directive

`[ListSize]` applies the `@listSize` directive to a field definition.

```graphql
directive @listSize(
  assumedSize: Int
  slicingArguments: [String!]
  slicingArgumentDefaultValue: Int
  sizedFields: [String!]
  requireOneSlicingArgument: Boolean = true
) on FIELD_DEFINITION
```

| Attribute property            | Use it for                                     | Details                                                                                                                    |
| ----------------------------- | ---------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| `AssumedSize`                 | Static estimate and variable fallback          | Must be non-negative. Use a server-enforced maximum or conservative cap.                                                   |
| `SlicingArguments`            | Numeric arguments that determine list size     | Literal values, schema defaults, and variables influence analysis differently.                                             |
| `SlicingArgumentDefaultValue` | Fallback when no slicing value is available    | Must be non-negative. Hot Chocolate v16 emits it only when slicing arguments are present.                                  |
| `SizedFields`                 | Child list fields governed by the parent field | Common values are `edges`, `nodes`, and `items`.                                                                           |
| `RequireOneSlicingArgument`   | Require one usable slicing argument            | The attribute default is `true`. Generated paging metadata follows `RequirePagingBoundaries`, which is `false` by default. |

For attribute examples, import `HotChocolate.CostAnalysis.Types`. For descriptor examples, import `HotChocolate.Types`.

# Verify list-size metadata

1. Inspect your schema SDL and confirm `@listSize` appears on the expected field.
2. Send representative operations with the `GraphQL-Cost: report` HTTP header to execute the request and include cost metrics.
3. Use `GraphQL-Cost: validate` when you want cost metrics without executing the request.
4. Tune paging options, `[ListSize]`, `[Cost]`, or cost limits based on the report.

# Troubleshoot list-size issues

## `HC0082` says exactly one slicing argument must be defined

This happens when `RequireOneSlicingArgument = true` and the operation does not provide one usable slicing argument.

Fix it by providing the expected argument, setting `RequireOneSlicingArgument = false` when omitted boundaries are valid, or using `RequirePagingBoundaries` for paged fields. Variables pass this validation because their runtime values are unknown, so keep `AssumedSize` conservative.

## Costs are higher than expected

Check whether `MaxPageSize` or `AssumedSize` is larger than the real server-side cap. Also check variable slicing arguments and nested lists, because list estimates multiply through nested selections. Use `GraphQL-Cost: report` before changing enforcement limits.

## Costs are too low for a list field

Unknown list fields without `@listSize` are estimated as size `1`. Add `[ListSize]` for capped unpaged lists, or use `[UsePaging]` for large client-controlled lists so Hot Chocolate can generate paging-aware metadata.

## `@listSize` is missing from SDL

Confirm cost defaults are enabled, the field is recognized as a paged field, and the attribute is applied to the GraphQL field method or property. For descriptor schemas, call `.ListSize(...)` on the object field descriptor.

## `[ListSize]` does not limit resolver results

That is expected. `[ListSize]` is static-analysis metadata. Enforce result size with paging, argument validation, or resolver and service logic.

## Schema setup fails for attribute values

`AssumedSize` and `SlicingArgumentDefaultValue` must be non-negative. Treat those attribute values as initialization metadata and do not read them from attribute instances at runtime.

# Next steps

- Read [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) to configure limits, inspect reports, and understand request rejection.
- Read [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination) to align `MaxPageSize`, `DefaultPageSize`, and `RequirePagingBoundaries` with list-size metadata.
- Review [Public API guidance](/docs/hotchocolate/v16/guides/public-api) when you expose a GraphQL endpoint to untrusted clients.
