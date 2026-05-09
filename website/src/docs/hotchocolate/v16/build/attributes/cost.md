---
title: Cost attribute
---

The `[Cost]` attribute lets you describe how expensive a field, type, or argument is for Hot Chocolate's cost analysis. When a custom list field can multiply the work of nested selections, use `[ListSize]` alongside it.

A GraphQL field may appear simple in the schema, but its resolver could call a database, invoke a remote service, or return many objects. Hot Chocolate v16 reads cost metadata before execution, calculates the operation cost, and can reject operations that exceed your configured budget.

This page explains the attribute entry point. For details on the cost algorithm, request headers, global limits, filtering, sorting, and enforcement, see [Cost Analysis](/docs/hotchocolate/v16/build/security/cost-analysis).

# How cost attributes affect an operation

`[Cost]` and `[ListSize]` add type-system directive metadata to your schema.

```text
[Cost] / [ListSize]
        |
        v
@cost / @listSize schema metadata
        |
        v
Static operation analysis
        |
        v
extensions.operationCost and configured limits
```

The `[Cost]` attribute adds `@cost(weight: "...")`. This weight contributes to the field or type cost when the annotated schema element is used in an operation.

The `[ListSize]` attribute adds `@listSize(...)`. This metadata tells the analyzer how many list items to assume, or which argument controls the list length. That size multiplies nested selections.

Attributes affect the calculated metrics, but do not set the budget. Configure budgets separately using cost options.

# Assigning a cost weight to an expensive field

Apply `[Cost]` to a resolver method or property when the default model underestimates the work involved.

```csharp
#nullable enable

using HotChocolate;
using HotChocolate.CostAnalysis.Types;
using HotChocolate.Types;

[QueryType]
public static partial class ProductQueries
{
    [Cost(50)]
    public static async Task<Product?> GetProductByIdAsync(
        int id,
        ProductService productService,
        CancellationToken cancellationToken)
    {
        return await productService.GetProductByIdAsync(id, cancellationToken);
    }
}

public sealed record Product(int Id, string Name);

public sealed class ProductService
{
    public Task<Product?> GetProductByIdAsync(int id, CancellationToken cancellationToken)
    {
        return Task.FromResult<Product?>(new Product(id, "Coffee"));
    }
}
```

Expected SDL shape:

```graphql
type Query {
  productById(id: Int!): Product @cost(weight: "50")
}
```

Hot Chocolate formats the SDL directive argument as a string, such as `weight: "50"`, even though the C# attribute constructor accepts a `double`.

When choosing weights, start from the defaults. With cost defaults enabled, an async resolver pipeline defaults to `10`, a composite field or type defaults to `1`, and scalar fields default to `0`. Use higher weights for remote calls, expensive database work, aggregations, CPU-intensive transformations, large file access, or relationship fields that fan out.

# Adding cost to a relationship resolver

Relationship fields often look like object navigation in GraphQL, but the resolver may still perform backend work. Assign a weight to such fields when the real cost is higher than a scalar property.

```csharp
using HotChocolate;
using HotChocolate.CostAnalysis.Types;
using HotChocolate.Types;

[ExtendObjectType<Product>]
public sealed class ProductNode
{
    [Cost(25)]
    public async Task<Brand?> GetBrandAsync(
        [Parent] Product product,
        ProductService productService,
        CancellationToken cancellationToken)
    {
        return await productService.GetBrandByIdAsync(product.BrandId, cancellationToken);
    }
}

public sealed record Product(int Id, int BrandId, string Name);

public sealed record Brand(int Id, string Name);
```

DataLoader batching can reduce the actual backend cost. However, the field may still deserve a higher weight than scalar properties because it adds work for every selected `Product`.

# Adding list-size metadata to a custom list field

Use `[ListSize]` when a field returns a list and does not use Hot Chocolate's paging middleware. The analyzer needs a size estimate to multiply nested selections.

```csharp
#nullable enable

using HotChocolate;
using HotChocolate.CostAnalysis.Types;
using HotChocolate.Types;

[QueryType]
public static partial class ProductQueries
{
    [ListSize(
        AssumedSize = 20,
        SlicingArguments = ["limit"],
        RequireOneSlicingArgument = true)]
    public static async Task<IReadOnlyList<Product>> GetRelatedProductsAsync(
        int productId,
        int limit,
        ProductService productService,
        CancellationToken cancellationToken)
    {
        return await productService.GetRelatedProductsAsync(productId, limit, cancellationToken);
    }
}
```

Expected SDL shape:

```graphql
type Query {
  relatedProducts(productId: Int!, limit: Int!): [Product!]
    @listSize(
      assumedSize: 20
      slicingArguments: ["limit"]
      requireOneSlicingArgument: true
    )
}
```

If the client sends a literal value, such as `limit: 5`, the analyzer can use that value. If the client uses a variable, the analyzer falls back to `AssumedSize` because it cannot know the runtime value during static analysis.

Setting `RequireOneSlicingArgument = true` instructs the analyzer to require exactly one configured slicing argument. If your field has a server-side default, use `SlicingArgumentDefaultValue` or set `RequireOneSlicingArgument = false`.

# Prefer paging options before adding `[ListSize]`

You usually do not need `[ListSize]` on fields that use `[UsePaging]` or `[UseOffsetPaging]`. Hot Chocolate adds cost metadata for standard paging fields automatically.

```csharp
using HotChocolate;
using HotChocolate.Types;

[QueryType]
public static partial class ProductQueries
{
    [UsePaging(MaxPageSize = 50, DefaultPageSize = 20, RequirePagingBoundaries = true)]
    public static IQueryable<Product> GetProducts(ProductStore store)
    {
        return store.Products.OrderBy(p => p.Id);
    }
}
```

For cursor paging, Hot Chocolate uses `first` and, when available, `last` as slicing arguments. It applies the size to `edges` and `nodes`, uses `MaxPageSize` as the assumed size, and can use `DefaultPageSize` as the slicing argument default value.

For offset paging, Hot Chocolate uses `take` as the slicing argument and applies the size to `items`.

Tune paging options first: `MaxPageSize`, `DefaultPageSize`, and `RequirePagingBoundaries`. Add `[ListSize]` only for non-standard list semantics or when you need an intentional override. If you override a paged field, ensure `SizedFields` matches the generated result shape, such as `edges` and `nodes` for connections or `items` for collection segments.

# Adding cost to arguments, input fields, and types

The `[Cost]` attribute is not limited to object fields. Use it on arguments and input fields when a client option increases backend work.

```csharp
using HotChocolate.CostAnalysis.Types;

public static Task<IReadOnlyList<Product>> SearchProductsAsync(
    [Cost(15)] ProductSearchInput filter,
    ProductService productService,
    CancellationToken cancellationToken)
{
    return productService.SearchAsync(filter, cancellationToken);
}

public sealed class ProductSearchInput
{
    public string? Text { get; init; }

    [Cost(25)]
    public bool IncludeArchived { get; init; }
}
```

Supported `[Cost]` targets in v16:

| CLR target         | GraphQL metadata target    | Common use                              |
| ------------------ | -------------------------- | --------------------------------------- |
| Method or property | Field definition           | Expensive resolver or property field    |
| Parameter          | Argument definition        | Expensive option selected by the client |
| Class or struct    | Object type or scalar type | Advanced type-wide cost model           |
| Enum               | Enum type                  | Advanced enum cost model                |
| Input property     | Input field definition     | Expensive input option                  |

Start with field-level weights. Type, enum, and scalar weights are advanced schema-wide choices because they affect every operation that reaches those schema elements.

# Verifying the schema and measured cost

Inspect the schema output or use the Banana Cake Pop schema view to check for the applied directives on fields, arguments, or types.

```graphql
type Query {
  productById(id: Int!): Product @cost(weight: "50")

  relatedProducts(productId: Int!, limit: Int!): [Product!]
    @listSize(
      assumedSize: 20
      slicingArguments: ["limit"]
      requireOneSlicingArgument: true
    )
}
```

Next, measure representative operations. Send `GraphQL-Cost: report` to execute the operation and include metrics in the response, or `GraphQL-Cost: validate` to calculate cost without executing resolvers.

Example response extension:

```json
{
  "extensions": {
    "operationCost": {
      "fieldCost": 120,
      "typeCost": 42
    }
  }
}
```

Compare the before and after values to confirm that the metadata changed the operation cost as expected.

# Tuning limits separately

Attributes describe cost, while cost options set the limits.

```csharp
builder
    .AddGraphQL()
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 2_000;
        options.MaxTypeCost = 2_000;
    });
```

Common v16 options include:

| Option                             | Default | Use it to                                                     |
| ---------------------------------- | ------- | ------------------------------------------------------------- |
| `MaxFieldCost`                     | `1000`  | Set the maximum execution-impact budget.                      |
| `MaxTypeCost`                      | `1000`  | Set the maximum response-size budget.                         |
| `EnforceCostLimits`                | `true`  | Control whether limits reject operations.                     |
| `ApplyCostDefaults`                | `true`  | Control whether Hot Chocolate adds default cost metadata.     |
| `ApplySlicingArgumentDefaultValue` | `true`  | Control whether paging default sizes feed list-size metadata. |
| `DefaultResolverCost`              | `10.0`  | Set the default weight for async resolver pipelines.          |

If you have disabled default security or built a lower-level executor, register the analyzer explicitly with `.AddCostAnalyzer()` before relying on cost limits.

Do not lower accurate weights to make legitimate rejections pass. Instead, prefer smaller page sizes, less nesting, split operations, corrected list-size metadata, or adjusted `MaxFieldCost` and `MaxTypeCost` values.

# Using descriptor configuration for shared conventions

Use attributes when the cost belongs next to the resolver or CLR model. Use descriptor-based configuration when you cannot edit the CLR type, when a central schema module owns the rule, or when you need to compute settings from configuration.

```csharp
using HotChocolate.Types;

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("productById")
            .Argument("id", a => a.Type<NonNullType<IntType>>())
            .Resolve(ctx => new Product(ctx.ArgumentValue<int>("id"), "Coffee"))
            .Cost(50);

        descriptor
            .Field("relatedProducts")
            .Argument("productId", a => a.Type<NonNullType<IntType>>())
            .Argument("limit", a => a.Type<NonNullType<IntType>>())
            .Resolve(ctx => Array.Empty<Product>())
            .ListSize(assumedSize: 20, slicingArguments: ["limit"]);
    }
}
```

Use cost options, not attributes or descriptors, for global budgets and defaults.

# Troubleshooting cost attributes

## My query is rejected after adding `[Cost]`

The operation exceeded `MaxFieldCost` or `MaxTypeCost`. The server rejects it before resolvers run.

```json
{
  "errors": [
    {
      "message": "The maximum allowed field cost was exceeded.",
      "extensions": {
        "code": "HC0081",
        "fieldCost": 1200,
        "maxFieldCost": 1000
      }
    }
  ]
}
```

To resolve this, reduce client page sizes, reduce nesting, split the operation, correct inaccurate metadata, or adjust `MaxFieldCost` and `MaxTypeCost`.

## I get `Exactly one slicing argument must be defined.`

A `[ListSize]` rule required exactly one configured slicing argument, but the operation sent none or more than one.

```json
{
  "errors": [
    {
      "message": "Exactly one slicing argument must be defined.",
      "extensions": {
        "code": "HC0082"
      }
    }
  ]
}
```

Pass exactly one configured size argument, set `RequireOneSlicingArgument = false` if ambiguity is acceptable, or provide `SlicingArgumentDefaultValue` when the field has a server-side default.

## The cost did not change

Check the following:

- Import `HotChocolate.CostAnalysis.Types`.
- Confirm the attribute target becomes a schema descriptor. `[ListSize]` is for object fields.
- Inspect the SDL for `@cost` or `@listSize` on the expected schema element.
- Compare `GraphQL-Cost: report` results before and after the change.
- If the new value matches a default, `ApplyCostDefaults` may make the difference hard to see.

## A paged field reports unexpected cost

Paging can already add `@listSize` metadata. Prefer paging options for standard paged fields. If you override with `[ListSize]`, verify `SizedFields`, `SlicingArguments`, and `RequireOneSlicingArgument` against the generated connection or collection segment shape.

# Attribute reference

| Attribute    | Namespace                         | Generated directive    | Main members                                                                                                 |
| ------------ | --------------------------------- | ---------------------- | ------------------------------------------------------------------------------------------------------------ |
| `[Cost]`     | `HotChocolate.CostAnalysis.Types` | `@cost(weight: "...")` | `CostAttribute(double weight)`                                                                               |
| `[ListSize]` | `HotChocolate.CostAnalysis.Types` | `@listSize(...)`       | `AssumedSize`, `SlicingArguments`, `SlicingArgumentDefaultValue`, `SizedFields`, `RequireOneSlicingArgument` |

`AssumedSize` and `SlicingArgumentDefaultValue` must be non-negative values. `RequireOneSlicingArgument` defaults to `true` on `[ListSize]`.

# Next steps

- Read [Cost Analysis](/docs/hotchocolate/v16/build/security/cost-analysis) for the full algorithm, reporting headers, validation mode, and enforcement options.
- Review [Request Limits](/docs/hotchocolate/v16/build/security/execution-depth-and-limits) for parser, validation, depth, timeout, and batching protections.
- Review [Pagination](/docs/hotchocolate/v16/build/pagination) before overriding list-size metadata on paged fields.
- Use [Custom Attributes](/docs/hotchocolate/v16/build/attributes/custom-descriptor-attributes) when you want a reusable attribute that bundles repeated descriptor rules.
