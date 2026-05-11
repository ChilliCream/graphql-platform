---
title: ListSize attribute
---

The `[ListSize]` attribute adds list-size metadata to a GraphQL field in Hot Chocolate. This metadata is used during cost analysis, before resolvers run, to estimate how many items a list field might return.

Apply `[ListSize]` to fields that return lists when Hot Chocolate cannot infer a safe size from paging metadata. This attribute only affects static cost analysis. It does not page, truncate, validate, or alter the results from your resolver.

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

In this example, cost analysis estimates that `featuredProducts` returns 25 `Product` items. The actual number of products returned is still determined by your resolver, so ensure the value matches any server-side cap you enforce.

# Paging metadata and paged fields

Standard `[UsePaging]` fields typically receive `@listSize` metadata automatically when cost defaults are enabled. For cursor paging, Hot Chocolate can generate metadata like the following:

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

These generated values are based on your paging options:

| Directive argument            | Typical source                                                          |
| ----------------------------- | ----------------------------------------------------------------------- |
| `assumedSize`                 | `MaxPageSize`, default `50`                                             |
| `slicingArguments`            | `first` and `last` for cursor paging, or `take` for offset paging       |
| `slicingArgumentDefaultValue` | `DefaultPageSize`, default `10`, when enabled in cost options           |
| `sizedFields`                 | `edges` and `nodes` for connections, or `items` for collection segments |
| `requireOneSlicingArgument`   | `RequirePagingBoundaries`, default `false`                              |

Adjust your paging options if you want the generated metadata to match your paging contract. Add an explicit `[ListSize]` to a paged field only if you need to override the generated metadata.

# Assigning a size to an unpaged list

Set `AssumedSize` for lists with a known maximum size.

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

If list-size metadata is missing, Hot Chocolate estimates unknown lists as size `1`. This can understate the cost of nested selections. Use paging instead of an unpaged list if clients can request large or unbounded result sets.

# Sizing a list using field arguments

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
- If the schema argument has an integer default value, the analyzer uses that value.
- If no slicing value is available, `SlicingArgumentDefaultValue = 20` is used.
- If a query passes a variable and the static analyzer cannot determine the runtime value, `AssumedSize = 100` is the fallback.
- If multiple slicing arguments have usable values, the analyzer uses the maximum value.

Set these values to match the limits enforced by your resolver, validation rules, or service layer. An optimistic estimate may allow expensive queries, while a conservative estimate could reject valid ones.

# Applying parent size to child list fields

Connection fields return connection objects, and the lists are child fields such as `edges` and `nodes`. The `SizedFields` property tells cost analysis that the parent field's slicing arguments determine the size of these child lists.

You usually do not need to set this on standard `[UsePaging]` fields, as Hot Chocolate generates it automatically. Use it when you override generated values or create a custom connection-like field.

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

# Combining list size with cost weights

`[Cost]` and `[ListSize]` each describe a different aspect of cost calculation:

- `[Cost]` sets a weight for a field, argument, input, or type.
- `[ListSize]` sets the estimated item count, which multiplies the cost of nested selections.

```text
products(first: 20) uses @listSize
  nodes is estimated as 20 items
    selected child field cost is multiplied by 20
```

Use `[Cost]` for expensive operations and `[ListSize]` for list cardinality. For more on cost budgets and request headers, see [Cost Analysis](/docs/hotchocolate/v16/build/security/cost-analysis).

# Using descriptor-based configuration

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

Use descriptor-based configuration when the field is not backed by a method or property, when you centralize schema configuration in `ObjectType` classes, or when schema conventions should own this metadata. Schema and paging options are helpful when you want generated metadata to change across many fields.

# The generated directive

The `[ListSize]` attribute applies the `@listSize` directive to a field definition.

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
| `AssumedSize`                 | Static estimate and variable fallback          | Must be non-negative. Use a server-enforced maximum or a conservative cap.                                                 |
| `SlicingArguments`            | Numeric arguments that determine list size     | Literal values, schema defaults, and variables influence analysis differently.                                             |
| `SlicingArgumentDefaultValue` | Fallback when no slicing value is available    | Must be non-negative. Hot Chocolate emits this only when slicing arguments are present.                                    |
| `SizedFields`                 | Child list fields governed by the parent field | Common values are `edges`, `nodes`, and `items`.                                                                           |
| `RequireOneSlicingArgument`   | Require one usable slicing argument            | The attribute default is `true`. Generated paging metadata follows `RequirePagingBoundaries`, which is `false` by default. |

For attribute examples, import `HotChocolate.CostAnalysis.Types`. For descriptor examples, import `HotChocolate.Types`.

# Verifying list-size metadata

1. Inspect your schema SDL and confirm that `@listSize` appears on the expected field.
2. Send representative operations with the `GraphQL-Cost: report` HTTP header to execute the request and include cost metrics.
3. Use `GraphQL-Cost: validate` if you want cost metrics without executing the request.
4. Adjust paging options, `[ListSize]`, `[Cost]`, or cost limits based on the report.

# Troubleshooting list-size issues

## `HC0082` error: exactly one slicing argument must be defined

This error occurs when `RequireOneSlicingArgument = true` and the operation does not provide a usable slicing argument.

To resolve this, provide the expected argument, set `RequireOneSlicingArgument = false` if omitted boundaries are valid, or use `RequirePagingBoundaries` for paged fields. Variables pass this validation because their runtime values are unknown, so keep `AssumedSize` conservative.

## Costs are higher than expected

Check if `MaxPageSize` or `AssumedSize` is larger than the actual server-side cap. Also review variable slicing arguments and nested lists, as list estimates multiply through nested selections. Use `GraphQL-Cost: report` before changing enforcement limits.

## Costs are too low for a list field

Unknown list fields without `@listSize` are estimated as size `1`. Add `[ListSize]` for capped unpaged lists, or use `[UsePaging]` for large client-controlled lists so Hot Chocolate can generate paging-aware metadata.

## `@listSize` is missing from SDL

Ensure cost defaults are enabled, the field is recognized as a paged field, and the attribute is applied to the GraphQL field method or property. For descriptor schemas, call `.ListSize(...)` on the object field descriptor.

## `[ListSize]` does not limit resolver results

This is expected. `[ListSize]` provides static-analysis metadata only. Enforce result size with paging, argument validation, or logic in your resolver and service layer.

## Schema setup fails for attribute values

`AssumedSize` and `SlicingArgumentDefaultValue` must be non-negative. Treat these attribute values as initialization metadata and do not read them from attribute instances at runtime.

# Next steps

- Read [Cost Analysis](/docs/hotchocolate/v16/build/security/cost-analysis) to configure limits, inspect reports, and understand request rejection.
- Read [Pagination](/docs/hotchocolate/v16/build/pagination) to align `MaxPageSize`, `DefaultPageSize`, and `RequirePagingBoundaries` with list-size metadata.
- Review [Public API guidance](/docs/hotchocolate/v16/_leagcy/guides/public-api) when exposing a GraphQL endpoint to untrusted clients.
