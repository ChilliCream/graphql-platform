---
title: Sort types
---

A sort type defines the public input contract for an `order` argument. While Hot Chocolate can infer this contract from your .NET model, production schemas often require a more focused and stable sort language than the full model shape provides.

Create a custom sort type when you need to control which fields are sortable, which nested paths are available, which sort directions are permitted, or which fields a provider should translate.

This page covers sort input types in Hot Chocolate v16. For information on package installation and basic resolver setup, see [Sorting](/docs/hotchocolate/v16/build/filtering-sorting-projections/sort-types). To configure global defaults like argument names, operation names, runtime type bindings, and providers, use sort conventions.

# Understanding sort types

A sort request passes through several stages:

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

This input corresponds to the runtime paths `Product.Brand.Name` and `Product.Price`. The active sort provider translates these accepted paths and operations to the underlying data source.

Key terms:

| Term            | Meaning                                                                           |
| --------------- | --------------------------------------------------------------------------------- |
| Sort input type | The input object used by an `order` argument for an entity shape.                 |
| Sort field      | A named field that maps to a runtime property, member, or nested object path.     |
| Sort operation  | An enum value such as `ASC` or `DESC`.                                            |
| Sort enum type  | The enum type used by scalar sort fields. The default is `SortEnumType`.          |
| Sort convention | The configuration source for default names, runtime type bindings, and providers. |
| Sort provider   | The component that translates accepted input to the backing data source.          |

# How Hot Chocolate infers sort input types

Consider the following model:

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

And a resolver with sorting enabled:

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UseSorting]
    public static IQueryable<Product> GetProducts(CatalogContext db)
        => db.Products;
}
```

Hot Chocolate will infer a sort input shape similar to this abbreviated SDL:

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

This inferred shape becomes part of your public API. If, for example, `description` is expensive to sort by, or you want to expose only `brand.name` rather than every compatible brand field, define the sort type explicitly.

By default, Hot Chocolate uses implicit binding. It infers compatible readable properties, skips indexers, omits `IFieldResult`, and does not include arrays or lists as direct sort fields. Object properties can become nested sort input types if their runtime type has compatible sort fields. The provider ultimately determines whether the accepted input can be translated at execution time.

# Defining a custom sort type

Create a `SortInputType<T>` when you want the sort input to be an intentional API contract rather than relying on inference.

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

This results in a smaller SDL shape:

```graphql
"Sorts products in the public catalog."
input ProductSortInput {
  "Sort by the public product name."
  name: SortEnumType
  price: SortEnumType
}
```

`Description`, `Brand`, and `Reviews` are omitted because explicit binding is used.

## Applying the custom sort type

Apply the custom type where sorting is enabled:

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UseSorting(typeof(ProductSortInputType))]
    public static IQueryable<Product> GetProducts(CatalogContext db)
        => db.Products;
}
```

You can also use the generic attribute:

```csharp
[UseSorting<ProductSortInputType>]
public static IQueryable<Product> GetProducts(CatalogContext db)
    => db.Products;
```

For descriptor-based types:

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

For small, local changes, configure the generated sort type inline:

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

To make a sort type the convention default for a runtime type:

```csharp
builder
    .AddGraphQL()
    .AddSorting(c => c
        .AddDefaults()
        .BindRuntimeType<Product, ProductSortInputType>());
```

Applying a sort type to a field is sufficient for that field. You can also register the type with `.AddType<ProductSortInputType>()` if explicit registration improves clarity or if your application uses a generated registration pattern.

# Choosing binding behavior

By default, binding is implicit: all compatible public properties become sort fields unless you specify otherwise.

| Option           | When to use                                                                  | Example                                  |
| ---------------- | ---------------------------------------------------------------------------- | ---------------------------------------- |
| Implicit binding | All compatible fields should be part of the public sort API.                 | `descriptor.BindFieldsImplicitly();`     |
| Explicit binding | The sort API requires an allowlist, a stable contract, or provider-safe set. | `descriptor.BindFieldsExplicitly();`     |
| Ignore fields    | Most inferred fields are fine, but a few should be hidden.                   | `descriptor.Ignore(p => p.Description);` |

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
descriptor.Ignore(p => p.Description);
descriptor.Field(p => p.Brand).Ignore();
```

The `[GraphQLIgnore]` attribute affects the broader schema. Prefer the sort-specific `Ignore(...)` when you want the object field to remain visible but not sortable.

# Configuring fields

Use `ISortFieldDescriptor` to configure individual sort fields.

| Goal                          | API                                                                              |
| ----------------------------- | -------------------------------------------------------------------------------- |
| Rename a sort field           | `.Field(p => p.Name).Name("title")`                                              |
| Add introspection text        | `.Field(p => p.Name).Description("Sort by public name.")`                        |
| Override the field input type | `.Field(p => p.Name).Type<AscOnlySortEnumType>()`                                |
| Use a nested sort input type  | `.Field(p => p.Brand).Type<BrandSortInputType>()`                                |
| Hide a field                  | `.Field(p => p.Description).Ignore()` or `descriptor.Ignore(p => p.Description)` |
| Attach a directive            | `.Field(p => p.Name).Directive("tag")`                                           |

Field names and descriptions are part of the public schema. Always test the generated SDL when making changes.

## Advanced member expressions

The `Field(...)` method accepts member expressions. Hot Chocolate supports expression fields, such as using a string length member and renaming it to a sort field. This is an advanced API design pattern. Before exposing such fields in a public schema, verify that the active provider can translate the resulting expression.

# Configuring nested sorting

Nested sorting allows clients to order results by fields on related objects:

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

Explicitly model nested sort types when related entities have fields that should not be exposed as sort options.

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

This produces the following SDL:

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

Nested sorting must be supported by both the provider and the backing data source. If the provider cannot translate a nested path, remove the field from the sort type or add provider support.

# Restricting sort directions

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

Apply this custom enum to a specific field:

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

Now, only `Product.name` uses the restricted enum. Other scalar sort fields use the default enum.

```graphql
input ProductSortInput {
  name: AscOnlySortEnum
  price: SortEnumType
}

enum AscOnlySortEnum {
  ASC
}
```

# Customizing operation names and descriptions

Operation names are set at the convention level. Use the sort convention to change names or descriptions for the entire schema or for a named convention scope.

```csharp
builder
    .AddGraphQL()
    .AddSorting(c => c
        .AddDefaults()
        .Operation(DefaultSortOperations.Ascending)
        .Name("ASCENDING")
        .Description("Sort from low to high or A to Z."));
```

The generated enum will reflect the convention:

```graphql
enum SortEnumType {
  "Sort from low to high or A to Z."
  ASCENDING
  DESC
}
```

Avoid giving the same operation name different meanings on different fields. Adding a custom operation name or ID is not enough for execution; the active provider must be able to translate the operation.

# Provider constraints and scopes

A sort type defines the accepted input shape, while the sort provider controls execution. Design your sort type based on what the active provider can translate server-side.

| Provider setup         | When to use                                                                          | Notes                                                                         |
| ---------------------- | ------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------- |
| `.AddSorting()`        | LINQ-style sorting for `IQueryable<T>`, `IEnumerable<T>`, and queryable executables. | Uses the default queryable provider.                                          |
| `.AddMongoDbSorting()` | For MongoDB executable queries.                                                      | Use scoped conventions if MongoDB sorting is used alongside another provider. |
| `.AddMartenSorting()`  | For Marten-backed queries.                                                           | Keep provider-specific behavior on the integration page.                      |
| `.AddRavenSorting()`   | For RavenDB-backed queries.                                                          | Use the integration package registration for the active Raven provider.       |

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

Common provider limitations:

- A field may appear in the SDL but still fail at runtime if the provider cannot translate the member, expression, nested path, or operation.
- Sorting collection-valued properties directly is not part of the inferred sort shape.
- Nested sort paths should match indexed or otherwise acceptable database access patterns.
- Computed members and advanced expressions require provider verification before being exposed as public sort fields.

# Keeping sorting stable with pagination

Cursor pagination requires deterministic ordering. If many rows share the same value for the requested sort field, pages can repeat or skip rows unless the final ordering includes a unique tie-breaker.

The sort type controls what clients can request, but stable tie-breakers are often enforced in query or service code so clients do not need to specify them.

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

Always use a unique key such as `Id` as the last ordering instruction. If the public sort type exposes low-cardinality fields like `status`, `category`, or `createdDate`, add a deterministic tie-breaker before applying cursor pagination.

# When attributes or conventions are sufficient

In many cases, you do not need a custom `SortInputType<T>`.

| Use this                                                      | When                                                                                                                         |
| ------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| `[UseSorting]` or `.UseSorting()`                             | The inferred sort shape is safe, useful, and provider-translatable.                                                          |
| `[GraphQLName]` and XML documentation                         | The same name or description should apply to both the object field and the sort field.                                       |
| `descriptor.Ignore(...)` in a small inline sort configuration | Only one resolver needs a local exclusion.                                                                                   |
| Sort conventions                                              | The policy is global, such as argument name, operation names, default enum binding, runtime type binding, or provider setup. |
| A custom sort type                                            | The public sort contract needs an allowlist, nested shape, field-specific enum type, or field-specific documentation.        |

# Testing sort types

Sort types are part of your schema contract. Test both the SDL and execution.

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

Verify the returned order and cover provider-specific behavior, especially for nested paths and custom enum types.

# Troubleshooting

| Symptom                                                      | Likely cause                                                                                                  | Solution                                                                                           |
| ------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| `No default sorting convention found` during schema creation | Sorting was used on a field without registering a convention.                                                 | Call `.AddSorting()` or use the provider-specific sorting registration.                            |
| `No sorting convention found for scope`                      | A field references a sorting scope that was not registered.                                                   | Register the scoped convention and match `Scope` on `[UseSorting]` or `.UseSorting(...)`.          |
| Field is missing from the sort input                         | The property is a collection, unreadable, unsupported by convention, ignored, or omitted by explicit binding. | Add it explicitly if supported, remove the ignore rule, or expose a different provider-safe field. |
| Field appears in SDL but sorting fails                       | The provider cannot translate the field, nested path, expression, or operation.                               | Remove the field from the sort type or add provider support.                                       |
| Nested sorting is slow                                       | The nested path lacks a suitable database translation or index.                                               | Expose a smaller nested sort type, add indexes, or move the sort to provider-specific code.        |
| Cursor pages are unstable                                    | The requested sort is not unique.                                                                             | Append a unique tie-breaker such as the primary key in service or query code.                      |

# Design checklist

Before exposing a sort field, consider the following:

- Is the field useful to API consumers?
- Is it safe to reveal as a sort option?
- Can the provider translate it server-side?
- Does the backing store have acceptable performance for this sort?
- Is the sort meaningful for users?
- Does pagination require a hidden or appended tie-breaker?
- Should nested sort types expose a small allowlist?
- Does the generated SDL match the intended public contract?

# Next steps

- See [Sorting](/docs/hotchocolate/v16/build/filtering-sorting-projections/sort-types) for setup, middleware order, and query syntax.
- See [Pagination](/docs/hotchocolate/v16/build/pagination) when using sorted fields with cursor pagination.
- See [MongoDB](/docs/hotchocolate/v16/_leagcy/integrations/mongodb) or [Marten](/docs/hotchocolate/v16/_leagcy/integrations/marten) for provider-specific sorting registration.
- Use sort conventions when you want the same naming, operation, provider, or runtime binding policy to apply across many sort types.
