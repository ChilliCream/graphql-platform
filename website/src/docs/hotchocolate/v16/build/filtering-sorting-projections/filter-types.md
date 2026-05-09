---
title: Filter types
---

A filter type defines the public predicate contract for a `where` argument. While Hot Chocolate can infer this contract from your .NET model, production schemas often require a smaller, more stable filter language than the full model shape provides.

Create a custom filter type when you need control over which fields are filterable, which operations are available, how fields are named, or which nested paths a provider should translate.

This page covers filter input types in Hot Chocolate v16. For information on package installation and basic resolver setup, see [Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types). For details on custom providers and field handlers, visit [Extending Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/extending-filtering).

# Understanding filter types

A filter request passes through several stages:

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

This input targets the runtime path `Product.Brand.Name` and applies the equals operation. The filter provider then translates this path and operation to the underlying data source.

Key filter terms:

| Term              | Meaning                                                                       |
| ----------------- | ----------------------------------------------------------------------------- |
| Filter input type | The input object used by a `where` argument for an entity or operation shape. |
| Filter field      | A named field that maps to a runtime property, member, or nested object path. |
| Operation field   | A predicate such as `eq`, `contains`, `gt`, or `some`.                        |
| Combinator        | `and` and `or` fields that compose filter objects.                            |
| Provider          | The component that translates accepted input to the backing data source.      |

# How Hot Chocolate infers filter input types

Consider the following model:

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

Suppose you have a resolver with filtering enabled:

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UseFiltering]
    public static IQueryable<Product> GetProducts(CatalogContext db)
        => db.Products;
}
```

Hot Chocolate will infer a filter input shape similar to this abbreviated SDL:

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

This inferred shape is helpful during development, but it is also part of your public API. If, for example, `internalSku` should not be filterable, or if you do not want to expose `contains` on every string field, define the filter type explicitly.

# Built-in operation input shapes

Hot Chocolate selects operation input types based on the active filter convention. The default convention provides these common shapes:

| Runtime shape                                                                                 | Default operations                                                                                      |
| --------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| `string`                                                                                      | `eq`, `neq`, `contains`, `ncontains`, `in`, `nin`, `startsWith`, `nstartsWith`, `endsWith`, `nendsWith` |
| `bool`                                                                                        | `eq`, `neq`                                                                                             |
| Comparable values such as numeric types, `Guid`, `DateTime`, `DateTimeOffset`, and `TimeSpan` | `eq`, `neq`, `in`, `nin`, `gt`, `ngt`, `gte`, `ngte`, `lt`, `nlt`, `lte`, `nlte`                        |
| Enum values                                                                                   | `eq`, `neq`, `in`, `nin`                                                                                |
| Object properties                                                                             | A nested filter input type                                                                              |
| Collection properties                                                                         | `all`, `none`, `some`, `any`                                                                            |

Integrations and custom conventions can map runtime types to different filter input types. For instance, spatial filtering introduces integration-specific operation shapes.

# Designing a custom filter type

Define a `FilterInputType<T>` when you want the inferred filter to become a deliberate API contract.

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

This results in a more focused SDL shape:

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

Here, `InternalSku` is omitted because explicit binding is used.

## Applying the custom filter type

You can apply the custom type wherever filtering is enabled:

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

For small, local changes, you can configure the generated filter inline:

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

To set a filter type as the convention default for a runtime type:

```csharp
builder
    .AddGraphQL()
    .AddFiltering(c => c
        .AddDefaults()
        .BindRuntimeType<Product, ProductFilterInputType>());
```

Applying a filter type to a field is sufficient for that field. You may also register the type with `.AddType<ProductFilterInputType>()` if explicit registration improves clarity or reuse.

# Choosing binding behavior

By default, binding is implicit: all compatible public properties become filter fields unless you specify otherwise.

| Option           | When to use                                                                 | Example                                  |
| ---------------- | --------------------------------------------------------------------------- | ---------------------------------------- |
| Implicit binding | All compatible fields should be part of the public filter API.              | `descriptor.BindFieldsImplicitly();`     |
| Explicit binding | You want an allowlist, a stable contract, or a provider-safe set of fields. | `descriptor.BindFieldsExplicitly();`     |
| Ignore fields    | Most inferred fields are fine, but a few should be hidden.                  | `descriptor.Ignore(p => p.InternalSku);` |

Explicit allowlist example:

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

The `[GraphQLIgnore]` attribute affects the broader schema. Prefer filter-specific `Ignore(...)` if you want the object field to remain visible but not filterable.

# Configuring fields

Use `IFilterFieldDescriptor` to configure individual filter fields.

| Goal                          | API                                                                              |
| ----------------------------- | -------------------------------------------------------------------------------- |
| Rename a filter field         | `.Field(p => p.InStock).Name("available")`                                       |
| Add introspection text        | `.Field(p => p.Name).Description("Filters by public name.")`                     |
| Override operation input type | `.Field(p => p.Name).Type<SearchStringOperationFilterInputType>()`               |
| Hide a field                  | `.Field(p => p.InternalSku).Ignore()` or `descriptor.Ignore(p => p.InternalSku)` |
| Set a default input value     | `.Field(p => p.InStock).DefaultValue(true)`                                      |
| Attach a directive            | `.Field(p => p.Name).Directive("tag")`                                           |

Default values and directives shape the public schema contract. Use them when you intend and test for that contract.

## Advanced member expressions

The `Field(...)` method accepts member expressions. Hot Chocolate supports expression fields, such as renaming a string length member to a filter field. This is an advanced API design technique. Always verify that your provider can translate the resulting expression before exposing it in a public schema.

# Restricting operations for a single field

If you want a specific field to offer a narrower or different set of operations, define a custom operation filter input type.

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

Apply this type to the desired field:

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

Now, only `Product.name` uses this custom operation shape. Other string fields continue to use the convention-bound string operation type.

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

Avoid assigning different meanings to the same operation name on different fields. Operation names should have consistent, convention-level semantics.

# Restricting operations globally for a runtime type

To apply a consistent policy to all fields of a runtime type, use convention runtime binding.

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

Register this binding with the default convention:

```csharp
builder
    .AddGraphQL()
    .AddFiltering(c => c
        .AddDefaults()
        .BindRuntimeType<string, DefaultStringOperationFilterInputType>());
```

Now, all string fields in the convention use `DefaultStringOperationFilterInputType`, unless a field explicitly overrides its type.

This approach is useful when you want a default string policy, such as equality and prefix search, while allowing selected fields to opt into broader search operations through field-level overrides.

# Customizing operation names and descriptions

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

With this configuration, every equals operation in the convention uses `equals` instead of `eq`.

```graphql
input StringOperationFilterInput {
  equals: String
  neq: String
  contains: String
}
```

For custom operations, select an operation ID above `1024` to avoid conflicts with built-in operations. Custom operations typically require provider or handler support. Refer to [Extending Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/extending-filtering) before adding custom operations to a public filter type.

# Controlling `and` and `or`

By default, entity filter input types include `and` and `or` fields:

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

To disable these combinators for a specific filter type:

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

To disable them for an entire convention:

```csharp
builder
    .AddGraphQL()
    .AddFiltering(c => c
        .AddDefaults()
        .AllowAnd(false)
        .AllowOr(false));
```

The `or` combinator is used to combine filter objects. It should not appear inside scalar operation input objects like `StringOperationFilterInput`.

# Nested and collection filters

Object properties are represented as nested filter input types, while collection properties become list filter input types.

Example: nested object query

```graphql
query {
  products(where: { brand: { name: { eq: "ChilliCream" } } }) {
    name
  }
}
```

Example: collection query with a nested element filter

```graphql
query {
  products(where: { orders: { some: { total: { gt: 100 } } } }) {
    name
  }
}
```

Example: collection existence query

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

Expose nested paths when they are valuable to clients and supported by your provider. For models with many relationships, use explicit binding for nested filter types as well.

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

The schema and execution provider must be compatible. Adding a field or operation to the SDL does not ensure that every provider can translate it efficiently.

Common provider outputs:

| Provider family                | Output shape                                  |
| ------------------------------ | --------------------------------------------- |
| Default `IQueryable` filtering | Expression trees                              |
| MongoDB filtering integration  | Provider-native MongoDB filters               |
| Marten filtering integration   | Expressions shaped for Marten's LINQ support  |
| Spatial filtering integration  | Spatial operation shapes and provider support |

Practical guidance:

- Favor filter fields that are backed by indexed data when your data source supports indexes.
- Keep relationship paths limited and intentional.
- Consider broad string operations like `contains`, `startsWith`, and `endsWith` as search and cost decisions.
- Test representative filters against generated SQL, MongoDB filters, provider diagnostics, or query plans.
- Use convention scopes if you need different filter behavior for different providers within one schema.

# When conventions or attributes are sufficient

You do not need to define a named `FilterInputType<T>` for every field.

| Requirement                                         | Use                                                                        |
| --------------------------------------------------- | -------------------------------------------------------------------------- |
| Enable default filtering on a resolver              | `[UseFiltering]` or `.UseFiltering()`                                      |
| Apply a local field allowlist                       | `.UseFiltering<Product>(d => d.BindFieldsExplicitly().Field(p => p.Name))` |
| Rename the `where` argument globally                | A filter convention with `ArgumentName(...)`                               |
| Apply one operation policy to all strings           | A convention with `BindRuntimeType<string, TFilter>()`                     |
| Share a stable public filter contract across fields | A named `FilterInputType<T>`                                               |
| Add provider-specific operations                    | A convention plus provider or handler support                              |

# Testing filter types

Changing a filter type is a schema change. Test both the SDL shape and the provider behavior you depend on.

Recommended checks:

1. Build the schema and snapshot the relevant SDL.
2. Run representative GraphQL queries for scalar, nested object, and collection filters.
3. Confirm hidden fields are missing from introspection and that validation rejects them.
4. Confirm restricted operations are absent from the SDL and that validation rejects them.
5. For database-backed fields, inspect the generated query or provider diagnostics for common filters.

Example: validation query

```graphql
query ProductsByBrand {
  products(where: { brand: { name: { eq: "ChilliCream" } } }) {
    name
  }
}
```

Example: operation rejection to test

```graphql
query InvalidContains {
  products(where: { name: { ncontains: "test" } }) {
    name
  }
}
```

If `name` uses the restricted `SearchStringOperationFilterInputType` shown earlier, `ncontains` should fail GraphQL validation because it is not part of the input type.

# Troubleshooting

| Symptom                                                         | What to check                                                                                                                                                                                             |
| --------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `where` does not appear on a field                              | Ensure `.AddFiltering()`, `[UseFiltering]` or `.UseFiltering()` is present, check the resolver return type, and verify middleware order: `UsePaging`, `UseProjection`, `UseFiltering`, then `UseSorting`. |
| A field appears in filters that should be hidden                | Use explicit binding or `Ignore(...)`. Also review nested filter types and convention runtime bindings.                                                                                                   |
| A custom filter type is ignored                                 | Make sure the type is applied with `[UseFiltering(typeof(...))]`, `.UseFiltering<TFilter>()`, or `BindRuntimeType<TRuntime, TFilter>()`.                                                                  |
| `.UseFiltering<Product>()` did not use `ProductFilterInputType` | The generic overload accepts either a runtime type or a filter input type. Use `.UseFiltering<ProductFilterInputType>()` to specify the filter type.                                                      |
| An operation is missing                                         | Check custom operation input types and convention configuration, such as `Configure<TFilterType>(...)` or ignored operations.                                                                             |
| A query validates but fails during execution                    | The provider may not translate the exposed path or operation. Inspect provider logs, generated queries, and integration documentation.                                                                    |
| `and` or `or` is missing                                        | Check `AllowAnd(false)` or `AllowOr(false)` on the filter type or convention. Built-in operation and list input types may also disable combinators internally.                                            |
| Operation names differ from examples                            | Review convention-level `.Operation(...).Name(...)` and convention scopes.                                                                                                                                |

# API reference summary

| API                                                       | Purpose                                                                                                        |
| --------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| `FilterInputType<T>`                                      | Custom filter input type bound to a runtime type `T`.                                                          |
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

- See [Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types) for setup, middleware order, and resolver usage.
- Visit [Extending Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/extending-filtering) for custom operations, providers, and handlers.
- Explore [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options) and [Sorting](/docs/hotchocolate/v16/build/filtering-sorting-projections/sort-types) for related data middleware.
- Review [MongoDB integration](/docs/hotchocolate/v16/_leagcy/integrations/mongodb), [Marten integration](/docs/hotchocolate/v16/_leagcy/integrations/marten), and [Spatial data](/docs/hotchocolate/v16/_leagcy/integrations/spatial-data) for provider-specific filter behavior.
