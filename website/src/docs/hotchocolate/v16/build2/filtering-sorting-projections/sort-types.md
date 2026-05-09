---
title: Sort types
---

A sort type is the public input contract behind an `order` argument. Hot Chocolate can infer this contract from your .NET model, but production schemas often need a smaller and more stable sort language than the full model shape.

Use a custom sort type when you want to decide which fields are sortable, which nested paths are exposed, which sort directions are allowed, or which fields a provider is expected to translate.

This page focuses on Hot Chocolate v16 sort input types. For package installation and basic resolver setup, see [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting). For global defaults such as argument names, operation names, runtime type bindings, and providers, use sort conventions.

# The sort type mental model

A sort request moves through these parts:

```text
GraphQL order input
  -> SortInputType<T> field and enum shape
  -> sort provider
  -> IQueryable expression or provider-native sort definition
```

For example:

```graphql
query {
  products(order: [{ brand: { name: ASC } }, { price: DESC }]) {
    name
    price
    brand {
      name
    }
  }
}
```

This input maps to the runtime paths `Product.Brand.Name` and `Product.Price`. The active sort provider translates the accepted paths and operations to the backing data source.

Sort vocabulary:

| Term            | Meaning                                                                           |
| --------------- | --------------------------------------------------------------------------------- |
| Sort input type | The input object used by an `order` argument for an entity shape.                 |
| Sort field      | A named field that maps to a runtime property, member, or nested object path.     |
| Sort operation  | An enum value such as `ASC` or `DESC`.                                            |
| Sort enum type  | The enum type used by scalar sort fields. The default is `SortEnumType`.          |
| Sort convention | The configuration source for default names, runtime type bindings, and providers. |
| Sort provider   | The component that translates accepted input to the backing data source.          |

# How Hot Chocolate infers sort input types

Given this model:

```csharp
public sealed class Product
{
    public string Name { get; set; } = default!;

    public decimal Price { get; set; }

    public string Description { get; set; } = default!;

    public Brand Brand { get; set; } = default!;

    public IReadOnlyList<Review> Reviews { get; set; } = [];
}

public sealed class Brand
{
    public string Name { get; set; } = default!;
}
```

and a resolver with sorting enabled:

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UseSorting]
    public static IQueryable<Product> GetProducts(CatalogContext db)
        => db.Products;
}
```

Hot Chocolate infers a sort input shape similar to this abbreviated SDL:

```graphql
type Query {
  products(order: [ProductSortInput!]): [Product!]
}

input ProductSortInput {
  name: SortEnumType
  price: SortEnumType
  description: SortEnumType
  brand: BrandSortInput
}

input BrandSortInput {
  name: SortEnumType
}

enum SortEnumType {
  ASC
  DESC
}
```

The inferred shape is public API. If `description` is expensive to sort by, or if the schema should only expose `brand.name` instead of every compatible brand field, define the sort type explicitly.

The default binding behavior is implicit. Hot Chocolate infers compatible readable properties, skips indexers, skips `IFieldResult`, and skips arrays and lists as direct sort fields. Object properties can become nested sort input types when their runtime type has compatible sort fields. The provider still decides whether the accepted input can be translated at execution time.

# Define a custom sort type

Create a `SortInputType<T>` when the inferred sort input should become an intentional API contract.

```csharp
public sealed class ProductSortInputType : SortInputType<Product>
{
    protected override void Configure(ISortInputTypeDescriptor<Product> descriptor)
    {
        descriptor.Name("ProductSortInput");
        descriptor.Description("Sorts products in the public catalog.");

        descriptor.BindFieldsExplicitly();

        descriptor
            .Field(p => p.Name)
            .Description("Sort by the public product name.");

        descriptor.Field(p => p.Price);
    }
}
```

This produces a smaller SDL shape:

```graphql
"Sorts products in the public catalog."
input ProductSortInput {
  "Sort by the public product name."
  name: SortEnumType
  price: SortEnumType
}
```

`Description`, `Brand`, and `Reviews` no longer appear because the type uses explicit binding.

## Apply the custom sort type

You can apply the type where sorting is enabled:

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UseSorting(typeof(ProductSortInputType))]
    public static IQueryable<Product> GetProducts(CatalogContext db)
        => db.Products;
}
```

The generic attribute is also supported:

```csharp
[UseSorting<ProductSortInputType>]
public static IQueryable<Product> GetProducts(CatalogContext db)
    => db.Products;
```

With descriptor-based types:

```csharp
public sealed class ProductQueriesType : ObjectType<ProductQueries>
{
    protected override void Configure(IObjectTypeDescriptor<ProductQueries> descriptor)
    {
        descriptor
            .Field(t => t.GetProducts(default!))
            .UseSorting<ProductSortInputType>();
    }
}
```

For small local changes, configure the generated sort type inline:

```csharp
public sealed class ProductQueriesType : ObjectType<ProductQueries>
{
    protected override void Configure(IObjectTypeDescriptor<ProductQueries> descriptor)
    {
        descriptor
            .Field(t => t.GetProducts(default!))
            .UseSorting<Product>(d => d
                .BindFieldsExplicitly()
                .Field(p => p.Name));
    }
}
```

To make one sort type the convention default for a runtime type:

```csharp
builder
    .AddGraphQL()
    .AddSorting(c => c
        .AddDefaults()
        .BindRuntimeType<Product, ProductSortInputType>());
```

Applying a sort type to a field is enough for that field. You can also register the type with `.AddType<ProductSortInputType>()` when explicit registration makes reuse clearer or when your application uses a generated registration pattern.

# Choose binding behavior

The default binding behavior is implicit. Compatible public properties become sort fields unless you configure otherwise.

| Option           | Use when                                                                      | Example                                  |
| ---------------- | ----------------------------------------------------------------------------- | ---------------------------------------- |
| Implicit binding | All compatible fields can be part of the public sort API.                     | `descriptor.BindFieldsImplicitly();`     |
| Explicit binding | The sort API needs an allowlist, stable contract, or provider-safe field set. | `descriptor.BindFieldsExplicitly();`     |
| Ignore fields    | Most inferred fields are acceptable and a small number should be hidden.      | `descriptor.Ignore(p => p.Description);` |

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
descriptor.Ignore(p => p.Description);
descriptor.Field(p => p.Brand).Ignore();
```

`[GraphQLIgnore]` affects broader schema binding. Prefer sort-specific `Ignore(...)` when the object field should remain visible but should not be sortable.

# Configure fields

`ISortFieldDescriptor` configures one sort field.

| Goal                          | API                                                                              |
| ----------------------------- | -------------------------------------------------------------------------------- |
| Rename a sort field           | `.Field(p => p.Name).Name("title")`                                              |
| Add introspection text        | `.Field(p => p.Name).Description("Sort by public name.")`                        |
| Override the field input type | `.Field(p => p.Name).Type<AscOnlySortEnumType>()`                                |
| Use a nested sort input type  | `.Field(p => p.Brand).Type<BrandSortInputType>()`                                |
| Hide a field                  | `.Field(p => p.Description).Ignore()` or `descriptor.Ignore(p => p.Description)` |
| Attach a directive            | `.Field(p => p.Name).Directive("tag")`                                           |

Field names and descriptions are part of the public schema. Test the generated SDL when changing them.

## Advanced member expressions

`Field(...)` accepts member expressions. Hot Chocolate tests cover expression fields such as a string length member renamed to a sort field. Treat this as advanced API design. Verify that the active provider can translate the resulting expression before exposing it in a public schema.

# Configure nested sorting

Nested sorting lets clients order by fields on a related object:

```graphql
query GetProducts {
  products(order: [{ brand: { name: ASC } }, { price: DESC }]) {
    name
    price
    brand {
      name
    }
  }
}
```

Model nested sort types explicitly when related entities contain fields that should not become sort options.

```csharp
public sealed class ProductSortInputType : SortInputType<Product>
{
    protected override void Configure(ISortInputTypeDescriptor<Product> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor.Field(p => p.Name);
        descriptor.Field(p => p.Price);
        descriptor.Field(p => p.Brand).Type<BrandSortInputType>();
        descriptor.Field(p => p.Type).Type<ProductTypeSortInputType>();
    }
}

public sealed class BrandSortInputType : SortInputType<Brand>
{
    protected override void Configure(ISortInputTypeDescriptor<Brand> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(b => b.Name);
    }
}

public sealed class ProductTypeSortInputType : SortInputType<ProductType>
{
    protected override void Configure(ISortInputTypeDescriptor<ProductType> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(t => t.Name);
    }
}
```

Generated SDL:

```graphql
input ProductSortInput {
  name: SortEnumType
  price: SortEnumType
  brand: BrandSortInput
  type: ProductTypeSortInput
}

input BrandSortInput {
  name: SortEnumType
}

input ProductTypeSortInput {
  name: SortEnumType
}
```

Nested sorting must be supported by the provider and backing data source. If the provider cannot translate a nested path, remove the field from the sort type or add provider support.

# Restrict sort directions

Scalar sort fields use the default `SortEnumType` with `ASC` and `DESC`. Use a custom enum type when one field should expose a narrower set of operations.

```csharp
public sealed class AscOnlySortEnumType : DefaultSortEnumType
{
    protected override void Configure(ISortEnumTypeDescriptor descriptor)
    {
        descriptor.Name("AscOnlySortEnum");
        descriptor.Operation(DefaultSortOperations.Ascending);
    }
}
```

Apply it to a selected field:

```csharp
public sealed class ProductSortInputType : SortInputType<Product>
{
    protected override void Configure(ISortInputTypeDescriptor<Product> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor
            .Field(p => p.Name)
            .Type<AscOnlySortEnumType>();

        descriptor.Field(p => p.Price);
    }
}
```

Only `Product.name` uses this enum shape. Other scalar sort fields keep the convention-bound enum.

```graphql
input ProductSortInput {
  name: AscOnlySortEnum
  price: SortEnumType
}

enum AscOnlySortEnum {
  ASC
}
```

# Customize operation names and descriptions

Operation names are convention-level semantics. Configure them through the sort convention when the whole schema, or a named convention scope, should use different names or descriptions.

```csharp
builder
    .AddGraphQL()
    .AddSorting(c => c
        .AddDefaults()
        .Operation(DefaultSortOperations.Ascending)
        .Name("ASCENDING")
        .Description("Sort from low to high or A to Z."));
```

The generated enum reflects the convention:

```graphql
enum SortEnumType {
  "Sort from low to high or A to Z."
  ASCENDING
  DESC
}
```

Do not give the same operation name different meanings on different fields. Adding a custom operation name or operation ID is not enough for execution. The active provider must know how to translate that operation.

# Provider constraints and scopes

A sort type controls the accepted input shape. The sort provider controls execution. Design the sort type around what the active provider can translate server-side.

| Provider setup         | Use when                                                                             | Notes                                                                        |
| ---------------------- | ------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------- |
| `.AddSorting()`        | LINQ-style sorting for `IQueryable<T>`, `IEnumerable<T>`, and queryable executables. | Uses the default queryable provider.                                         |
| `.AddMongoDbSorting()` | MongoDB executable queries.                                                          | Use scoped conventions when MongoDB sorting is used beside another provider. |
| `.AddMartenSorting()`  | Marten-backed queries.                                                               | Keep provider-specific behavior on the integration page.                     |
| `.AddRavenSorting()`   | RavenDB-backed queries.                                                              | Use the integration package registration for the active Raven provider.      |

Scoped convention example:

```csharp
builder
    .AddGraphQL()
    .AddSorting()
    .AddMongoDbSorting("mongo");

[UseSorting(Scope = "mongo")]
public static IExecutable<Product> GetProducts(ProductCollection collection)
    => collection.AsExecutable();
```

Common provider limits:

- A field can appear in SDL but still fail at runtime if the provider cannot translate the member, expression, nested path, or operation.
- Sorting collection-valued properties directly is not part of the inferred sort shape.
- Nested sort paths should match indexed or otherwise acceptable database access patterns.
- Computed members and advanced expressions need provider verification before they become public sort fields.

# Keep sorting stable with pagination

Cursor pagination needs deterministic ordering. If many rows have the same value for the requested sort field, pages can repeat or skip rows unless the final ordering includes a unique tie-breaker.

The sort type controls what clients can request. Stable tie-breakers are often enforced in query or service code so clients do not need to remember them.

LINQ example:

```csharp
var query = db.Products
    .OrderBy(p => p.Name)
    .ThenBy(p => p.Id);
```

Service-layer sort definition example:

```csharp
private static SortDefinition<Product> DefaultSortDefinition(
    SortDefinition<Product> definition)
    => definition
        .IfEmpty(d => d.AddAscending(p => p.Name))
        .AddAscending(p => p.Id);
```

Use a unique key such as `Id` as the last ordering instruction. If the public sort type exposes low-cardinality fields such as `status`, `category`, or `createdDate`, add a deterministic tie-breaker before applying cursor pagination.

# When attributes or conventions are enough

You may not need a custom `SortInputType<T>`.

| Use this                                                      | When                                                                                                                         |
| ------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| `[UseSorting]` or `.UseSorting()`                             | The inferred sort shape is safe, useful, and provider-translatable.                                                          |
| `[GraphQLName]` and XML documentation                         | The same name or description should apply to the object field and the sort field.                                            |
| `descriptor.Ignore(...)` in a small inline sort configuration | One resolver needs a local exclusion.                                                                                        |
| Sort conventions                                              | The policy is global, such as argument name, operation names, default enum binding, runtime type binding, or provider setup. |
| A custom sort type                                            | The public sort contract needs an allowlist, nested shape, field-specific enum type, or field-specific documentation.        |

# Test sort types

Sort types are schema contract. Test both SDL and execution.

## Snapshot the generated SDL

Use schema snapshots to verify the `order` argument and input fields.

```csharp
[Fact]
public async Task ProductSortInput_Should_MatchSchema_When_ExplicitlyConfigured()
{
    // arrange
    var schema = await new ServiceCollection()
        .AddGraphQL()
        .AddQueryType<ProductQueries>()
        .AddSorting()
        .Services
        .BuildServiceProvider()
        .GetRequiredService<IRequestExecutorResolver>()
        .GetRequestExecutorAsync();

    // act
    var sdl = schema.Schema.ToString();

    // assert
    sdl.MatchSnapshot();
}
```

Check that ignored fields are absent, renamed fields have the intended names, nested sort types are present, and restricted enum types expose the intended operations.

## Execute representative queries

Add execution tests for the sort shapes you expose:

```graphql
query {
  products(order: [{ brand: { name: ASC } }, { price: DESC }]) {
    name
    price
    brand {
      name
    }
  }
}
```

Verify the returned order and cover provider-specific behavior, especially nested paths and custom enum types.

# Troubleshooting

| Symptom                                                      | Likely cause                                                                                                  | Fix                                                                                                  |
| ------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------- |
| `No default sorting convention found` during schema creation | Sorting was used on a field without registering a convention.                                                 | Call `.AddSorting()` or the provider-specific sorting registration.                                  |
| `No sorting convention found for scope`                      | A field references a sorting scope that was not registered.                                                   | Register the scoped convention and match `Scope` on `[UseSorting]` or `.UseSorting(...)`.            |
| Field is missing from the sort input                         | The property is a collection, unreadable, unsupported by convention, ignored, or omitted by explicit binding. | Add it explicitly when supported, remove the ignore rule, or expose a different provider-safe field. |
| Field appears in SDL but sorting fails                       | The provider cannot translate the field, nested path, expression, or operation.                               | Remove the field from the sort type or add provider support.                                         |
| Nested sorting is slow                                       | The nested path lacks a suitable database translation or index.                                               | Expose a smaller nested sort type, add indexes, or move the sort to provider-specific code.          |
| Cursor pages are unstable                                    | The requested sort is not unique.                                                                             | Append a unique tie-breaker such as the primary key in service or query code.                        |

# Design checklist

Before exposing a sort field, ask:

- Is the field useful to API consumers?
- Is it safe to reveal as a sort option?
- Can the provider translate it server-side?
- Does the backing store have acceptable performance characteristics for this sort?
- Is the sort meaningful for users?
- Does pagination need a hidden or appended tie-breaker?
- Should nested sort types expose a small allowlist?
- Does the generated SDL match the intended public contract?

# Next steps

- Review [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting) for setup, middleware order, and query syntax.
- Review [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination) when sorted fields are used with cursor pagination.
- Review [MongoDB](/docs/hotchocolate/v16/integrations/mongodb) or [Marten](/docs/hotchocolate/v16/integrations/marten) for provider-specific sorting registration.
- Use sort conventions when the same naming, operation, provider, or runtime binding policy should apply across many sort types.
