---
title: "Parent access"
---

# Accessing Parent Values in Resolvers

When a field needs information from the object that contains it, you access the parent value. You can access this value using `this`, `[Parent]`, or `context.Parent<T>()`, depending on how your resolver is defined.

This page explains how to access parent values. For details on resolver signatures, service injection, and DataLoader setup, see the links in [Go next](#go-next).

## Understanding the Parent Value

Nested queries resolve from parent fields to child fields. For example:

```graphql
query GetProduct {
  product(id: 1) {
    name
    brand {
      name
    }
  }
}
```

The `product` resolver runs first. The `Product` object it returns becomes the parent value for `Product.name` and `Product.brand`. The `brand` resolver then reads `Product.BrandId` from that parent value to load the matching `Brand`.

```text
Query.product(id: 1)
  returns Product { Id = 1, BrandId = 42, Name = "Trail Shoe" }

Product.brand
  receives the Product as its parent value
  reads BrandId = 42
  returns Brand { Id = 42, Name = "Contoso" }

Brand.name
  reads Name from the Brand returned by Product.brand
```

The expected response is:

```json
{
  "data": {
    "product": {
      "name": "Trail Shoe",
      "brand": {
        "name": "Contoso"
      }
    }
  }
}
```

The parent value is not a client argument or an automatically loaded navigation property. It is the .NET value returned by the previous resolver in the GraphQL selection tree. Sibling fields may run concurrently, so do not rely on execution order between sibling child fields.

## Choosing a Parent Access API

| Resolver style                   | Parent access API                                  | When to use                                                               | Example shape                                               |
| -------------------------------- | -------------------------------------------------- | ------------------------------------------------------------------------- | ----------------------------------------------------------- |
| Instance member on the CLR model | `this`                                             | The resolver method is on the model type.                                 | `GetLabel() => $"{Name} ({Id})"`                            |
| Source-generated resolver method | `[Parent] T parent`                                | The resolver is in `[ObjectType<T>]` or `[ExtendObjectType<T>]`.          | `GetBrandAsync([Parent] Product product, ...)`              |
| Descriptor resolver delegate     | `context.Parent<T>()`                              | The field is configured with `ObjectType<T>` or `ObjectTypeExtension<T>`. | `.Resolve(ctx => ctx.Parent<Product>())`                    |
| Batch resolver                   | `[Parent] List<T>` or another supported list shape | One method resolves a field for many parent objects.                      | `[BatchResolver] GetDisplayName([Parent] List<User> users)` |

`[Parent]` refers to `HotChocolate.ParentAttribute` and is used on resolver method parameters. In descriptor-based code, use `context.Parent<T>()` to access the same value.

## Using `[Parent]` in a Source-Generated Resolver

Begin with model types that expose the key needed for the child field:

```csharp
public sealed class Product
{
    public int Id { get; set; }
    public int BrandId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class Brand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

The following examples use source-generated DataLoader interfaces named `IBrandByIdDataLoader` and `IProductsByBrandIdDataLoader`. See the [DataLoader documentation](/docs/hotchocolate/v16/build/dataloader) for details on generating these interfaces.

Add a resolver for the `brand` field on `Product`:

```csharp
[ObjectType<Product>]
public static partial class ProductNode
{
    public static async Task<Brand?> GetBrandAsync(
        [Parent] Product product,
        IBrandByIdDataLoader brandById,
        CancellationToken ct)
        => await brandById.LoadAsync(product.BrandId, ct);
}
```

How this works:

- Hot Chocolate injects `[Parent] Product product` from the current `Product` object.
- The resolver reads `product.BrandId` from the parent value.
- `IBrandByIdDataLoader` batches and caches brand lookups. Here, it is used only for lookup.
- `GetBrandAsync` becomes the GraphQL field `brand` because `Get` and `Async` are removed by resolver naming conventions.

Expected SDL excerpt:

```graphql
type Product {
  id: Int!
  name: String!
  brand: Brand
}
```

If you expose the model as-is, `brandId` may also appear as a property field. See [Replace foreign key fields with resolver fields](#replace-foreign-key-fields-with-resolver-fields) for how to replace it.

## Adding Parent-Based Fields with Type Extensions

Use `[ExtendObjectType<T>]` to place the field outside the CLR model or the main object type class:

```csharp
[ExtendObjectType<Product>]
public static partial class ProductExtensions
{
    public static async Task<Brand?> GetBrandAsync(
        [Parent] Product product,
        IBrandByIdDataLoader brandById,
        CancellationToken ct)
        => await brandById.LoadAsync(product.BrandId, ct);
}
```

When using source-generated registration, include the generated `AddTypes()` call in your GraphQL builder:

```csharp
builder
    .AddGraphQL()
    .AddTypes();
```

The parent access rule is the same as for `[ObjectType<Product>]`: the `Product` object being resolved is injected into the `[Parent]` parameter. For more on type extension patterns, see [Type extensions](/docs/hotchocolate/v16/build/schema-elements/extending-types).

## Using `context.Parent<T>()` in Descriptor Fields

In descriptor-based code, call `context.Parent<T>()` inside the resolver delegate:

```csharp
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Field("brand")
            .Resolve(async context =>
            {
                var product = context.Parent<Product>();
                var brandById = context.Service<IBrandByIdDataLoader>();

                return await brandById.LoadAsync(
                    product.BrandId,
                    context.RequestAborted);
            });
    }
}
```

`context.Parent<Product>()` should match the CLR value for the field's object type, or a valid base class or interface. If the type does not match, the cast can fail at runtime or an analyzer may report the mismatch for supported source-generated patterns.

## Resolving Child Collections from Parent Keys

When a child field returns a collection, use the parent key. Do not assume the parent object already contains a loaded navigation collection.

```csharp
[ObjectType<Brand>]
public static partial class BrandNode
{
    public static async Task<Product[]> GetProductsAsync(
        [Parent] Brand brand,
        IProductsByBrandIdDataLoader productsByBrandId,
        CancellationToken ct)
        => await productsByBrandId.LoadAsync(brand.Id, ct) ?? [];
}
```

Expected SDL excerpt:

```graphql
type Brand {
  id: Int!
  name: String!
  products: [Product!]!
}
```

Return an empty collection when no child rows exist and the schema represents an empty list. If the collection can be large, add paging and follow the [pagination documentation](/docs/hotchocolate/v16/build/pagination).

## Replacing Foreign Key Fields with Resolver Fields

A typical catalog model stores a foreign key, but the GraphQL schema should expose the related object:

| CLR model member  | Public GraphQL field |
| ----------------- | -------------------- |
| `Product.BrandId` | `Product.brand`      |

Bind the resolver to the existing member if you want the resolver-backed field to replace the property-backed field:

```csharp
[ObjectType<Product>]
public static partial class ProductNode
{
    [BindMember(nameof(Product.BrandId))]
    public static async Task<Brand?> GetBrandAsync(
        [Parent(requires: nameof(Product.BrandId))] Product product,
        IBrandByIdDataLoader brandById,
        CancellationToken ct)
        => await brandById.LoadAsync(product.BrandId, ct);
}
```

`[BindMember(nameof(Product.BrandId))]` binds this resolver to the original `BrandId` member. The generated field name still comes from the resolver method, so `GetBrandAsync` produces `brand`.

The optional `requires` argument declares which parent member the resolver reads. It records source-generated field requirement metadata for the parent value. Ensure the needed member is available on the object returned by your data layer, and verify projection behavior for your query path. For projection details, see [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options).

## Using `this` When the Resolver Is on the Model

If the resolver is an instance method on the runtime type, access the parent value through `this`:

```csharp
public sealed class Product
{
    public int Id { get; set; }
    public int BrandId { get; set; }
    public string Name { get; set; } = string.Empty;

    public string GetDisplayLabel()
        => $"{Name} ({Id})";

    public async Task<Brand?> GetBrandAsync(
        IBrandByIdDataLoader brandById,
        CancellationToken ct)
        => await brandById.LoadAsync(this.BrandId, ct);
}
```

Use this approach when your domain model can own the resolver method. If you want to keep GraphQL fields outside the model, use `[ObjectType<T>]`, `[ExtendObjectType<T>]`, or descriptor-based types.

## Avoiding Per-Parent Data Access

A typical child resolver runs once for each selected parent object. If that resolver queries the database directly, a list of parents can create an N+1 pattern.

```csharp
public static async Task<Brand?> GetBrandAsync(
    [Parent] Product product,
    CatalogContext db,
    CancellationToken ct)
    => await db.Brands
        .Where(brand => brand.Id == product.BrandId)
        .SingleOrDefaultAsync(ct);
```

Keep parent access focused on reading the key from the current object. Use a DataLoader-backed lookup or a batch resolver for repeated lookups. For these patterns, see [DataLoader](/docs/hotchocolate/v16/build/dataloader).

## Using Parent Values in Batch Resolvers

A `[BatchResolver]` receives many parents at once. The parent parameter changes from a single object to a list-shaped value:

```csharp
[ObjectType<User>]
public static partial class UserNode
{
    [BatchResolver]
    public static List<string> GetDisplayName([Parent] List<User> users)
        => users.Select(user => $"{user.FirstName} {user.LastName}").ToList();
}
```

The returned list must have the same count and order as the parent list. Source-generated analyzer coverage includes `List<T>`, `T[]`, and `ImmutableArray<T>` parent shapes. For full batch resolver design, see the [DataLoader documentation](/docs/hotchocolate/v16/build/dataloader).

## Handling Nulls, Type Checks, and Projections

| Situation                                            | What to expect                                                                          | What to do                                                                     |
| ---------------------------------------------------- | --------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------ |
| A parent field returns `null`                        | Hot Chocolate does not execute child fields for that null object.                       | Model the parent field as nullable when null is valid.                         |
| A child resolver returns `null` for a non-null field | GraphQL null propagation applies.                                                       | Match C# nullability and GraphQL nullability to real data.                     |
| A list contains null items                           | Child fields run only for non-null list items.                                          | Use nullable list item types only when null items are valid.                   |
| `[Parent]` type does not match                       | The source generator analyzer can report a mismatch, or `context.Parent<T>()` can fail. | Use the object type's CLR type, a valid base class, or a valid interface.      |
| A custom resolver needs a hidden or projected member | The parent object may not contain the needed value.                                     | Keep required keys on the parent object and validate projection configuration. |

For null propagation rules, see [Non-null](/docs/hotchocolate/v16/build/schema-elements/lists-and-non-null). For custom resolver projection behavior, see [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options).

## Troubleshooting Parent Value Issues

| Symptom                                                 | Likely cause                                                          | Fix                                                                                     |
| ------------------------------------------------------- | --------------------------------------------------------------------- | --------------------------------------------------------------------------------------- |
| A parent object appears as a GraphQL argument           | A source-generated resolver parameter is missing `[Parent]`.          | Add `[Parent]` or move descriptor code to `context.Parent<T>()`.                        |
| `context.Parent<T>()` fails                             | `T` does not match the runtime parent value.                          | Use the object type's CLR type, a valid base class, or a valid interface.               |
| The analyzer reports a parent type mismatch             | The `[Parent]` parameter type is incompatible with `[ObjectType<T>]`. | Change the parameter type or move the field to the matching object type.                |
| A relationship field makes one database call per parent | The resolver queries the database directly for each parent.           | Load by parent key through a DataLoader or use a batch resolver.                        |
| A resolver cannot read a foreign key                    | The root resolver, projection, or DTO did not populate the key.       | Return a parent object that contains the key, or declare and verify field requirements. |
| Child fields are missing when the parent is null        | GraphQL skips child selections for null parent values.                | Return a value for non-null parent fields or make the parent field nullable.            |

**Missing `[Parent]` example:**

```csharp
public static async Task<Brand?> GetBrandAsync(
    Product product,
    IBrandByIdDataLoader brandById,
    CancellationToken ct)
    => await brandById.LoadAsync(product.BrandId, ct);
```

Hot Chocolate may treat `product` as a field argument if no attribute marks it as the parent value. Add `[Parent]`:

```csharp
public static async Task<Brand?> GetBrandAsync(
    [Parent] Product product,
    IBrandByIdDataLoader brandById,
    CancellationToken ct)
    => await brandById.LoadAsync(product.BrandId, ct);
```

## Quick Reference

| Item                          | Value                                                                          |
| ----------------------------- | ------------------------------------------------------------------------------ |
| Attribute namespace           | `HotChocolate`                                                                 |
| Attribute type                | `ParentAttribute`                                                              |
| Applies to                    | Resolver method parameters                                                     |
| Normal parameter shape        | `[Parent] T parent`                                                            |
| Descriptor equivalent         | `context.Parent<T>()`                                                          |
| Optional constructor argument | `requires: string?`                                                            |
| Batch resolver shapes         | List-shaped parent parameter, such as `List<T>`, `T[]`, or `ImmutableArray<T>` |
| Common parent key pattern     | `[Parent] Product product` then `product.BrandId`                              |

## Go Next

| Goal                                                           | Page                                                                                         |
| -------------------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| Review resolver naming, return types, and supported parameters | [Resolver Signature](./resolver-signature)                                                   |
| Inject application services and understand resolver scopes     | [Service Injection](./service-injection)                                                     |
| Batch relationship loading and generated loader interfaces     | [DataLoader](/docs/hotchocolate/v16/build/dataloader)                                        |
| Organize fields outside CLR models                             | [Type extensions](/docs/hotchocolate/v16/build/schema-elements/extending-types)              |
| Understand custom resolver projection behavior                 | [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options) |
| Review GraphQL null propagation and C# nullability             | [Non-null](/docs/hotchocolate/v16/build/schema-elements/lists-and-non-null)                  |
