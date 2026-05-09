---
title: Filter types
---

A filter type is the public predicate contract behind a `where` argument. Hot Chocolate can infer this contract from your .NET model, but production schemas often need a smaller and more stable filter language than the full model shape.

Use a custom filter type when you want to decide which fields are filterable, which operations are available, how fields are named, or which nested paths a provider is expected to translate.

This page focuses on Hot Chocolate v16 filter input types. For package installation and basic resolver setup, see [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering). For custom providers and field handlers, see [Extending Filtering](/docs/hotchocolate/v16/api-reference/extending-filtering).

# The filter type mental model

A filter request moves through these parts:

```text
GraphQL where input
  -> FilterInputType<T> field and operation tree
  -> filter provider visitor
  -> IQueryable expression or provider-native filter definition
```

For example:

```graphql
query {
  products(where: { brand: { name: { eq: "ChilliCream" } } }) {
    name
  }
}
```

This input maps to the runtime path `Product.Brand.Name` and the equals operation. The filter provider translates that accepted path and operation to the backing data source.

Filter vocabulary:

| Term              | Meaning                                                                       |
| ----------------- | ----------------------------------------------------------------------------- |
| Filter input type | The input object used by a `where` argument for an entity or operation shape. |
| Filter field      | A named field that maps to a runtime property, member, or nested object path. |
| Operation field   | A predicate such as `eq`, `contains`, `gt`, or `some`.                        |
| Combinator        | `and` and `or` fields that compose filter objects.                            |
| Provider          | The component that translates accepted input to the backing data source.      |

# How Hot Chocolate infers filter input types

Given this model:

```csharp
public sealed class Product
{
    public string Name { get; set; } = default!;

    public decimal Price { get; set; }

    public bool InStock { get; set; }

    public Brand Brand { get; set; } = default!;

    public IReadOnlyList<Order> Orders { get; set; } = [];

    public string InternalSku { get; set; } = default!;
}

public sealed class Brand
{
    public string Name { get; set; } = default!;
}

public sealed class Order
{
    public decimal Total { get; set; }
}
```

and a resolver with filtering enabled:

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UseFiltering]
    public static IQueryable<Product> GetProducts(CatalogContext db)
        => db.Products;
}
```

Hot Chocolate infers a filter input shape similar to this abbreviated SDL:

```graphql
input ProductFilterInput {
  and: [ProductFilterInput!]
  or: [ProductFilterInput!]
  name: StringOperationFilterInput
  price: DecimalOperationFilterInput
  inStock: BooleanOperationFilterInput
  brand: BrandFilterInput
  orders: ListFilterInputTypeOfOrderFilterInput
  internalSku: StringOperationFilterInput
}

input BrandFilterInput {
  and: [BrandFilterInput!]
  or: [BrandFilterInput!]
  name: StringOperationFilterInput
}

input ListFilterInputTypeOfOrderFilterInput {
  all: OrderFilterInput
  none: OrderFilterInput
  some: OrderFilterInput
  any: Boolean
}
```

The inferred shape is useful for development, but it is also public API. If `internalSku` should not be filterable, or if `contains` should not be exposed on every string field, define the filter type explicitly.

# Built-in operation input shapes

Hot Chocolate chooses operation input types from the active filter convention. The default convention includes these common shapes:

| Runtime shape                                                                                 | Default operations                                                                                      |
| --------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| `string`                                                                                      | `eq`, `neq`, `contains`, `ncontains`, `in`, `nin`, `startsWith`, `nstartsWith`, `endsWith`, `nendsWith` |
| `bool`                                                                                        | `eq`, `neq`                                                                                             |
| Comparable values such as numeric types, `Guid`, `DateTime`, `DateTimeOffset`, and `TimeSpan` | `eq`, `neq`, `in`, `nin`, `gt`, `ngt`, `gte`, `ngte`, `lt`, `nlt`, `lte`, `nlte`                        |
| Enum values                                                                                   | `eq`, `neq`, `in`, `nin`                                                                                |
| Object properties                                                                             | A nested filter input type                                                                              |
| Collection properties                                                                         | `all`, `none`, `some`, `any`                                                                            |

Integrations and custom conventions can bind runtime types to different filter input types. For example, spatial filtering adds integration-specific operation shapes.

# Design a custom filter type

Create a `FilterInputType<T>` when the inferred filter should become an intentional API contract.

```csharp
public sealed class ProductFilterInputType : FilterInputType<Product>
{
    protected override void Configure(IFilterInputTypeDescriptor<Product> descriptor)
    {
        descriptor.Name("ProductFilterInput");
        descriptor.Description("Filters products in the public catalog.");

        descriptor.BindFieldsExplicitly();

        descriptor
            .Field(p => p.Name)
            .Description("Filters by the public product name.");

        descriptor.Field(p => p.Brand);
        descriptor.Field(p => p.Price);
        descriptor.Field(p => p.InStock).Name("available");
    }
}
```

This produces a smaller SDL shape:

```graphql
input ProductFilterInput {
  and: [ProductFilterInput!]
  or: [ProductFilterInput!]
  "Filters by the public product name."
  name: StringOperationFilterInput
  brand: BrandFilterInput
  price: DecimalOperationFilterInput
  available: BooleanOperationFilterInput
}
```

`InternalSku` no longer appears because the type uses explicit binding.

## Apply the custom filter type

You can apply the type where filtering is enabled:

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UseFiltering(typeof(ProductFilterInputType))]
    public static IQueryable<Product> GetProducts(CatalogContext db)
        => db.Products;
}
```

With descriptor-based types:

```csharp
public sealed class ProductQueries
{
    public IQueryable<Product> GetProducts(CatalogContext db)
        => db.Products;
}

public sealed class ProductQueriesType : ObjectType<ProductQueries>
{
    protected override void Configure(IObjectTypeDescriptor<ProductQueries> descriptor)
    {
        descriptor
            .Field(t => t.GetProducts(default!))
            .UseFiltering<ProductFilterInputType>();
    }
}
```

For small local changes, configure the generated filter inline:

```csharp
public sealed class ProductQueriesType : ObjectType<ProductQueries>
{
    protected override void Configure(IObjectTypeDescriptor<ProductQueries> descriptor)
    {
        descriptor
            .Field(t => t.GetProducts(default!))
            .UseFiltering<Product>(d => d
                .BindFieldsExplicitly()
                .Field(p => p.Name));
    }
}
```

To make one filter type the convention default for a runtime type:

```csharp
builder
    .AddGraphQL()
    .AddFiltering(c => c
        .AddDefaults()
        .BindRuntimeType<Product, ProductFilterInputType>());
```

Applying a filter type to a field is enough for that field. You can also register the type with `.AddType<ProductFilterInputType>()` when explicit registration makes reuse clearer.

# Choose binding behavior

The default binding behavior is implicit. Compatible public properties become filter fields unless you configure otherwise.

| Option           | Use when                                                                        | Example                                  |
| ---------------- | ------------------------------------------------------------------------------- | ---------------------------------------- |
| Implicit binding | All compatible fields can be part of the public filter API.                     | `descriptor.BindFieldsImplicitly();`     |
| Explicit binding | The filter API needs an allowlist, stable contract, or provider-safe field set. | `descriptor.BindFieldsExplicitly();`     |
| Ignore fields    | Most inferred fields are acceptable and a small number should be hidden.        | `descriptor.Ignore(p => p.InternalSku);` |

Explicit allowlist:

```csharp
descriptor
    .BindFieldsExplicitly()
    .Field(p => p.Name)
    .Field(p => p.Price);
```

Implicit binding with exclusions:

```csharp
descriptor.BindFieldsImplicitly();
descriptor.Ignore(p => p.InternalSku);
descriptor.Field(p => p.Brand).Ignore();
```

`[GraphQLIgnore]` affects broader schema binding. Prefer filter-specific `Ignore(...)` when the object field should remain visible but should not be filterable.

# Configure fields

`IFilterFieldDescriptor` configures one filter field.

| Goal                          | API                                                                              |
| ----------------------------- | -------------------------------------------------------------------------------- |
| Rename a filter field         | `.Field(p => p.InStock).Name("available")`                                       |
| Add introspection text        | `.Field(p => p.Name).Description("Filters by public name.")`                     |
| Override operation input type | `.Field(p => p.Name).Type<SearchStringOperationFilterInputType>()`               |
| Hide a field                  | `.Field(p => p.InternalSku).Ignore()` or `descriptor.Ignore(p => p.InternalSku)` |
| Set a default input value     | `.Field(p => p.InStock).DefaultValue(true)`                                      |
| Attach a directive            | `.Field(p => p.Name).Directive("tag")`                                           |

Default values and directives affect the public schema contract. Use them when that contract is deliberate and tested.

## Advanced member expressions

`Field(...)` accepts member expressions. Hot Chocolate tests cover expression fields such as a string length member renamed to a filter field. Treat this as advanced API design. Verify that the active provider can translate the resulting expression before exposing it in a public schema.

# Restrict operations for one field

Use a custom operation filter input type when one field should expose a narrower or different operation set.

```csharp
public sealed class SearchStringOperationFilterInputType
    : StringOperationFilterInputType
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Operation(DefaultFilterOperations.Equals).Type<StringType>();
        descriptor.Operation(DefaultFilterOperations.StartsWith).Type<StringType>();
        descriptor.Operation(DefaultFilterOperations.Contains).Type<StringType>();
    }
}
```

Apply it to a selected field:

```csharp
public sealed class ProductFilterInputType : FilterInputType<Product>
{
    protected override void Configure(IFilterInputTypeDescriptor<Product> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor
            .Field(p => p.Name)
            .Type<SearchStringOperationFilterInputType>();

        descriptor.Field(p => p.Brand);
        descriptor.Field(p => p.Price);
    }
}
```

Only `Product.name` uses this operation shape. Other string fields keep the convention-bound string operation type.

```graphql
input ProductFilterInput {
  and: [ProductFilterInput!]
  or: [ProductFilterInput!]
  name: SearchStringOperationFilterInput
  brand: BrandFilterInput
  price: DecimalOperationFilterInput
}

input SearchStringOperationFilterInput {
  eq: String
  startsWith: String
  contains: String
}
```

Do not give the same operation name a different meaning on different fields. Operation names are convention-level semantics.

# Restrict operations globally for a runtime type

Use convention runtime binding when every field of a runtime type should share a policy.

```csharp
public sealed class DefaultStringOperationFilterInputType
    : StringOperationFilterInputType
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Operation(DefaultFilterOperations.Equals).Type<StringType>();
        descriptor.Operation(DefaultFilterOperations.StartsWith).Type<StringType>();
    }
}
```

Register the binding with the default convention:

```csharp
builder
    .AddGraphQL()
    .AddFiltering(c => c
        .AddDefaults()
        .BindRuntimeType<string, DefaultStringOperationFilterInputType>());
```

All string fields in that convention now use `DefaultStringOperationFilterInputType`, unless a field overrides its type.

Use this when a team wants a default string policy, such as equality and prefix search, and selected fields can opt into broader search operations through field-level overrides.

# Customize operation names and descriptions

Operations are identified by operation ID and named through the filter convention.

```csharp
builder
    .AddGraphQL()
    .AddFiltering(c => c
        .AddDefaults()
        .Operation(DefaultFilterOperations.Equals)
        .Name("equals")
        .Description("Matches values that are equal."));
```

Every equals operation in that convention now uses `equals` instead of `eq`.

```graphql
input StringOperationFilterInput {
  equals: String
  neq: String
  contains: String
}
```

For custom operations, choose an operation ID higher than `1024` to avoid collisions with built-in operations. A custom operation usually also needs provider or handler support. See [Extending Filtering](/docs/hotchocolate/v16/api-reference/extending-filtering) before adding custom operations to a public filter type.

# Control `and` and `or`

Entity filter input types include `and` and `or` by default:

```graphql
query {
  products(
    where: {
      or: [{ name: { contains: "Cloud" } }, { name: { contains: "GraphQL" } }]
    }
  ) {
    name
  }
}
```

Disable them on one filter type:

```csharp
public sealed class ProductFilterInputType : FilterInputType<Product>
{
    protected override void Configure(IFilterInputTypeDescriptor<Product> descriptor)
    {
        descriptor.AllowAnd(false).AllowOr(false);
        descriptor.BindFieldsExplicitly();
        descriptor.Field(p => p.Name);
    }
}
```

Disable them for a convention:

```csharp
builder
    .AddGraphQL()
    .AddFiltering(c => c
        .AddDefaults()
        .AllowAnd(false)
        .AllowOr(false));
```

`or` combines filter objects. It does not belong inside scalar operation input objects such as `StringOperationFilterInput`.

# Nested and collection filters

Object properties become nested filter input types. Collection properties become list filter input types.

Nested object query:

```graphql
query {
  products(where: { brand: { name: { eq: "ChilliCream" } } }) {
    name
  }
}
```

Collection query with a nested element filter:

```graphql
query {
  products(where: { orders: { some: { total: { gt: 100 } } } }) {
    name
  }
}
```

Collection existence query:

```graphql
query {
  products(where: { orders: { any: true } }) {
    name
  }
}
```

List operation semantics:

| Operation | Meaning                                                                   |
| --------- | ------------------------------------------------------------------------- |
| `some`    | At least one element matches the nested filter.                           |
| `all`     | All elements match the nested filter.                                     |
| `none`    | No element matches the nested filter.                                     |
| `any`     | The collection has any elements when `true`, or no elements when `false`. |

Expose nested paths when they are useful to clients and supported by the provider. For relationship-heavy models, apply explicit binding to nested filter types as well.

```csharp
public sealed class BrandFilterInputType : FilterInputType<Brand>
{
    protected override void Configure(IFilterInputTypeDescriptor<Brand> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(b => b.Name);
    }
}
```

# Provider limits and safe filter design

The schema shape and execution provider must agree. Adding a field or operation to SDL does not guarantee that every provider can translate it efficiently.

Common provider outputs:

| Provider family                | Output shape                                  |
| ------------------------------ | --------------------------------------------- |
| Default `IQueryable` filtering | Expression trees                              |
| MongoDB filtering integration  | Provider-native MongoDB filters               |
| Marten filtering integration   | Expressions shaped for Marten's LINQ support  |
| Spatial filtering integration  | Spatial operation shapes and provider support |

Practical guidance:

- Prefer filter fields backed by indexed data when the data source supports indexes.
- Keep relationship paths bounded and intentional.
- Treat broad string `contains`, `startsWith`, and `endsWith` operations as search and cost decisions.
- Test representative filters against generated SQL, MongoDB filters, provider diagnostics, or query plans.
- Use convention scopes when two providers need different filter behavior in one schema.

# When conventions or attributes are enough

You do not need a named `FilterInputType<T>` for every field.

| Requirement                                         | Use                                                                        |
| --------------------------------------------------- | -------------------------------------------------------------------------- |
| Enable default filtering on a resolver              | `[UseFiltering]` or `.UseFiltering()`                                      |
| Apply one local field allowlist                     | `.UseFiltering<Product>(d => d.BindFieldsExplicitly().Field(p => p.Name))` |
| Rename the `where` argument globally                | A filter convention with `ArgumentName(...)`                               |
| Apply one operation policy to all strings           | A convention with `BindRuntimeType<string, TFilter>()`                     |
| Share a stable public filter contract across fields | A named `FilterInputType<T>`                                               |
| Add provider-specific operations                    | A convention plus provider or handler support                              |

# Testing filter types

Filter type changes are schema changes. Test both the SDL shape and the provider behavior you rely on.

Recommended checks:

1. Build the schema and snapshot the relevant SDL.
2. Execute representative GraphQL queries for scalar, nested object, and collection filters.
3. Verify hidden fields are absent from introspection and validation rejects them.
4. Verify restricted operations are absent from SDL and validation rejects them.
5. For database-backed fields, inspect the generated query or provider diagnostics for common filters.

Example validation query:

```graphql
query ProductsByBrand {
  products(where: { brand: { name: { eq: "ChilliCream" } } }) {
    name
  }
}
```

Example operation rejection to test:

```graphql
query InvalidContains {
  products(where: { name: { ncontains: "test" } }) {
    name
  }
}
```

If `name` uses the restricted `SearchStringOperationFilterInputType` shown above, `ncontains` should fail GraphQL validation because it is not part of the input type.

# Troubleshooting

| Symptom                                                         | Check                                                                                                                                                                                                       |
| --------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `where` does not appear on a field                              | Confirm `.AddFiltering()`, `[UseFiltering]` or `.UseFiltering()`, the resolver return type, and middleware order. The recommended order is `UsePaging`, `UseProjection`, `UseFiltering`, then `UseSorting`. |
| A field appears in filters that should be hidden                | Use explicit binding or `Ignore(...)`. Also check nested filter types and convention runtime bindings.                                                                                                      |
| A custom filter type is ignored                                 | Confirm the type is applied with `[UseFiltering(typeof(...))]`, `.UseFiltering<TFilter>()`, or `BindRuntimeType<TRuntime, TFilter>()`.                                                                      |
| `.UseFiltering<Product>()` did not use `ProductFilterInputType` | The generic overload accepts either a runtime type or a filter input type. Use `.UseFiltering<ProductFilterInputType>()` to select the filter type.                                                         |
| An operation is missing                                         | Check custom operation input types and convention configuration such as `Configure<TFilterType>(...)` or ignored operations.                                                                                |
| A query validates but fails during execution                    | The provider may not translate the exposed path or operation. Inspect provider logs, generated queries, and integration docs.                                                                               |
| `and` or `or` is missing                                        | Check `AllowAnd(false)` or `AllowOr(false)` on the filter type or convention. Built-in operation and list input types may also disable combinators internally.                                              |
| Operation names differ from examples                            | Check convention-level `.Operation(...).Name(...)` and convention scopes.                                                                                                                                   |

# API reference summary

| API                                                       | Purpose                                                                                                        |
| --------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| `FilterInputType<T>`                                      | Runtime-bound custom filter input type for `T`.                                                                |
| `FilterInputType`                                         | Base type for operation input shapes without a runtime entity type.                                            |
| `IFilterInputTypeDescriptor<T>`                           | Configures entity filter type names, fields, binding behavior, ignored fields, combinators, and directives.    |
| `IFilterInputTypeDescriptor`                              | Configures operation fields and string-named fields.                                                           |
| `IFilterFieldDescriptor`                                  | Configures field name, description, type, ignore state, default value, and directives.                         |
| `IFilterOperationFieldDescriptor`                         | Configures operation field name, description, type, operation ID, ignore state, default value, and directives. |
| `DefaultFilterOperations`                                 | Built-in operation IDs such as `Equals`, `Contains`, `GreaterThan`, `Some`, `And`, and `Or`.                   |
| `StringOperationFilterInputType`                          | Default string operation input type.                                                                           |
| `ComparableOperationFilterInputType<T>`                   | Base comparable operation input type.                                                                          |
| `ListFilterInputType<T>`                                  | List operation input type with `all`, `none`, `some`, and `any`.                                               |
| `[UseFiltering]`                                          | Attribute that adds a filter argument and middleware to a resolver.                                            |
| `.UseFiltering()`                                         | Descriptor API that adds a filter argument and middleware to a field.                                          |
| `.UseFiltering<T>()`                                      | Uses `T` as either a runtime type or filter input type.                                                        |
| `.UseFiltering<T>(Action<IFilterInputTypeDescriptor<T>>)` | Inline filter type configuration for a field.                                                                  |
| `BindRuntimeType<TRuntime, TFilter>()`                    | Binds a runtime type to a filter input type in a convention.                                                   |
| `AllowAnd(...)`, `AllowOr(...)`                           | Enables or disables logical combinators on a filter type or convention.                                        |

# Next steps

- [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering) for setup, middleware order, and resolver usage.
- [Extending Filtering](/docs/hotchocolate/v16/api-reference/extending-filtering) for custom operations, providers, and handlers.
- [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections) and [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting) for related data middleware.
- [MongoDB integration](/docs/hotchocolate/v16/integrations/mongodb), [Marten integration](/docs/hotchocolate/v16/integrations/marten), and [Spatial data](/docs/hotchocolate/v16/integrations/spatial-data) for provider-specific filter behavior.
