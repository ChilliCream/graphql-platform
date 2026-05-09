---
title: "Parent access"
---

# Access parent values in resolvers

Use the parent value when a field needs data from the object that contains it. In Hot Chocolate v16, you can read that value with `this`, `[Parent]`, or `context.Parent<T>()`, depending on where you define the resolver.

This page focuses on parent value access. For full resolver signatures, service injection, and DataLoader setup, use the links in [Go next](#go-next).

## Start with the parent value mental model

A nested query resolves from parent fields to child fields:

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

The `product` resolver runs first. The `Product` object it returns becomes the parent value for `Product.name` and `Product.brand`. The `brand` resolver can then read `Product.BrandId` from that parent value and load the matching `Brand`.

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

Expected response:

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

The parent value is not a client argument. It is also not an automatically loaded navigation property. It is the .NET value returned by the previous resolver in the GraphQL selection tree. Sibling fields can run concurrently, so do not depend on execution order between sibling child fields.

## Choose the parent access API

| Resolver style                   | Parent access API                                  | Use when                                                                  | Example shape                                               |
| -------------------------------- | -------------------------------------------------- | ------------------------------------------------------------------------- | ----------------------------------------------------------- |
| Instance member on the CLR model | `this`                                             | The resolver method lives on the model type.                              | `GetLabel() => $"{Name} ({Id})"`                            |
| Source-generated resolver method | `[Parent] T parent`                                | The resolver lives in `[ObjectType<T>]` or `[ExtendObjectType<T>]`.       | `GetBrandAsync([Parent] Product product, ...)`              |
| Descriptor resolver delegate     | `context.Parent<T>()`                              | You configure the field with `ObjectType<T>` or `ObjectTypeExtension<T>`. | `.Resolve(ctx => ctx.Parent<Product>())`                    |
| Batch resolver                   | `[Parent] List<T>` or another supported list shape | One method resolves a field for many parent objects.                      | `[BatchResolver] GetDisplayName([Parent] List<User> users)` |

`[Parent]` is `HotChocolate.ParentAttribute`. It applies to resolver method parameters. In descriptor-based code, `context.Parent<T>()` reads the same value.

## Use `[Parent]` in a source-generated resolver

Start with model types that expose the key you need for the child field:

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

The examples below use source-generated DataLoader interfaces named `IBrandByIdDataLoader` and `IProductsByBrandIdDataLoader`. The [DataLoader documentation](/docs/hotchocolate/v16/resolvers-and-data/dataloader) shows how those interfaces are generated.

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

What happens:

- Hot Chocolate supplies `[Parent] Product product` from the current `Product` object.
- The resolver reads `product.BrandId` from that parent value.
- `IBrandByIdDataLoader` batches and caches brand lookups. This page uses it only as the lookup mechanism.
- `GetBrandAsync` becomes the GraphQL field `brand` because `Get` and `Async` are removed by resolver naming conventions.

Expected SDL excerpt:

```graphql
type Product {
  id: Int!
  name: String!
  brand: Brand
}
```

If you expose the model as-is, `brandId` may also appear as a property field. [Replace foreign key fields with resolver fields](#replace-foreign-key-fields-with-resolver-fields) shows the replacement pattern.

## Add parent-based fields with type extensions

Use `[ExtendObjectType<T>]` when you want to place the field outside the CLR model or outside the main object type class:

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

When you use source-generated registration, include the generated `AddTypes()` call in your GraphQL builder:

```csharp
builder
    .AddGraphQL()
    .AddTypes();
```

The parent access rule is the same as `[ObjectType<Product>]`: the `Product` object being resolved is injected into the `[Parent]` parameter. For broader type extension patterns, see [Type extensions](/docs/hotchocolate/v16/build2/schema-elements/extending-types).

## Use `context.Parent<T>()` in descriptor fields

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

`context.Parent<Product>()` should match the CLR value for the field's object type, or a valid base class or interface. If the type does not match, the cast can fail at runtime or an analyzer can report the mismatch for supported source-generated patterns.

## Resolve child collections from parent keys

Use the parent key when a child field returns a collection. Do not assume the parent object already contains a loaded navigation collection.

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

Return an empty collection when no child rows exist and the schema represents an empty list. If the collection can grow large, add paging and follow the [pagination documentation](/docs/hotchocolate/v16/resolvers-and-data/pagination).

## Replace foreign key fields with resolver fields

A common catalog model stores a foreign key, while the GraphQL schema should expose the related object:

| CLR model member  | Public GraphQL field |
| ----------------- | -------------------- |
| `Product.BrandId` | `Product.brand`      |

Bind the resolver to the existing member when you want the resolver-backed field to replace the property-backed field:

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

The optional `requires` argument declares which parent member the resolver reads. It records source-generated field requirement metadata for the parent value. Keep the needed member available on the object returned by your data layer, and verify projection behavior for your query path. For projection details, see [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections).

## Use `this` when the resolver lives on the model

If the resolver is an instance method on the runtime type, read the parent value through `this`:

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

Use this shape when your domain model can own the resolver method. If you want to keep GraphQL fields outside the model, use `[ObjectType<T>]`, `[ExtendObjectType<T>]`, or descriptor-based types.

## Avoid per-parent data access

A normal child resolver runs once for each selected parent object. If that resolver queries the database directly, a list of parents can create an N+1 pattern.

```csharp
public static async Task<Brand?> GetBrandAsync(
    [Parent] Product product,
    CatalogContext db,
    CancellationToken ct)
    => await db.Brands
        .Where(brand => brand.Id == product.BrandId)
        .SingleOrDefaultAsync(ct);
```

Keep parent access focused on reading the key from the current object. Put repeated lookup batching behind a DataLoader-backed lookup or a batch resolver. For those patterns, see [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader).

## Use parent values in batch resolvers

A `[BatchResolver]` receives many parents at once. The parent parameter changes from one object to a list-shaped value:

```csharp
[ObjectType<User>]
public static partial class UserNode
{
    [BatchResolver]
    public static List<string> GetDisplayName([Parent] List<User> users)
        => users.Select(user => $"{user.FirstName} {user.LastName}").ToList();
}
```

The returned list must have the same count and order as the parent list. Source-generated analyzer coverage includes `List<T>`, `T[]`, and `ImmutableArray<T>` parent shapes. Keep full batch resolver design in the [DataLoader documentation](/docs/hotchocolate/v16/resolvers-and-data/dataloader).

## Handle nulls, type checks, and projections

| Situation                                            | What to expect                                                                          | What to do                                                                     |
| ---------------------------------------------------- | --------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------ |
| A parent field returns `null`                        | Hot Chocolate does not execute child fields for that null object.                       | Model the parent field as nullable when null is valid.                         |
| A child resolver returns `null` for a non-null field | GraphQL null propagation applies.                                                       | Match C# nullability and GraphQL nullability to real data.                     |
| A list contains null items                           | Child fields run only for non-null list items.                                          | Use nullable list item types only when null items are valid.                   |
| `[Parent]` type does not match                       | The source generator analyzer can report a mismatch, or `context.Parent<T>()` can fail. | Use the object type's CLR type, a valid base class, or a valid interface.      |
| A custom resolver needs a hidden or projected member | The parent object may not contain the needed value.                                     | Keep required keys on the parent object and validate projection configuration. |

For null propagation rules, see [Non-null](/docs/hotchocolate/v16/building-a-schema/non-null). For custom resolver projection behavior, see [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections).

## Troubleshoot parent value issues

| Symptom                                                 | Likely cause                                                          | Fix                                                                                     |
| ------------------------------------------------------- | --------------------------------------------------------------------- | --------------------------------------------------------------------------------------- |
| A parent object appears as a GraphQL argument           | A source-generated resolver parameter is missing `[Parent]`.          | Add `[Parent]` or move descriptor code to `context.Parent<T>()`.                        |
| `context.Parent<T>()` fails                             | `T` does not match the runtime parent value.                          | Use the object type's CLR type, a valid base class, or a valid interface.               |
| The analyzer reports a parent type mismatch             | The `[Parent]` parameter type is incompatible with `[ObjectType<T>]`. | Change the parameter type or move the field to the matching object type.                |
| A relationship field makes one database call per parent | The resolver queries the database directly for each parent.           | Load by parent key through a DataLoader or use a batch resolver.                        |
| A resolver cannot read a foreign key                    | The root resolver, projection, or DTO did not populate the key.       | Return a parent object that contains the key, or declare and verify field requirements. |
| Child fields are missing when the parent is null        | GraphQL skips child selections for null parent values.                | Return a value for non-null parent fields or make the parent field nullable.            |

Missing `[Parent]` example:

```csharp
public static async Task<Brand?> GetBrandAsync(
    Product product,
    IBrandByIdDataLoader brandById,
    CancellationToken ct)
    => await brandById.LoadAsync(product.BrandId, ct);
```

Hot Chocolate can treat `product` as a field argument because no attribute marks it as the parent value. Add `[Parent]`:

```csharp
public static async Task<Brand?> GetBrandAsync(
    [Parent] Product product,
    IBrandByIdDataLoader brandById,
    CancellationToken ct)
    => await brandById.LoadAsync(product.BrandId, ct);
```

## Use the quick reference

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

## Go next

| Goal                                                           | Page                                                                             |
| -------------------------------------------------------------- | -------------------------------------------------------------------------------- |
| Review resolver naming, return types, and supported parameters | [Resolver Signature](./resolver-signature)                                       |
| Inject application services and understand resolver scopes     | [Service Injection](./service-injection)                                         |
| Batch relationship loading and generated loader interfaces     | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader)               |
| Organize fields outside CLR models                             | [Type extensions](/docs/hotchocolate/v16/build2/schema-elements/extending-types) |
| Understand custom resolver projection behavior                 | [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections)             |
| Review GraphQL null propagation and C# nullability             | [Non-null](/docs/hotchocolate/v16/building-a-schema/non-null)                    |
