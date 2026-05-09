---
title: "Object Types"
---

Object types define the values returned by your GraphQL API. They represent the client-facing structure of your output graph, rather than a direct mapping of every public C# member or database column.

```graphql
type Product {
  id: ID!
  name: String!
  description: String
  price: Decimal!
  brand: Brand
}

type Brand {
  id: ID!
  name: String!
}
```

Use object types for data clients read. For structured input from clients, use [input object types](/docs/hotchocolate/v16/build/schema-elements/input-object-types). To share output fields, use [interfaces](/docs/hotchocolate/v16/build/schema-elements/interfaces). For polymorphic output without shared fields, use [unions](/docs/hotchocolate/v16/build/schema-elements/unions). To add fields from another module, use [type extensions](/docs/hotchocolate/v16/build/schema-elements/extending-types).

Key terms:

| Term         | Meaning                                                                                           |
| ------------ | ------------------------------------------------------------------------------------------------- |
| Object type  | A GraphQL output type with named fields, such as `Product`.                                       |
| Field        | A selectable value on an object type, for example `Product.name`.                                 |
| Resolver     | The C# member, delegate, or method that produces a field value.                                   |
| Parent value | The object returned by the previous resolver. For `Product.brand`, the parent value is `Product`. |
| Runtime type | The CLR type Hot Chocolate receives at execution time, for example `Product`.                     |

```text
C# runtime value (Product)
        |
        | conventions, attributes, [ObjectType<T>] classes, descriptors
        v
GraphQL object type (type Product)
        |
        | selected field resolver
        v
field result value (scalar, object, list, or null)
```

The catalog scenario in this page draws inspiration from the workshop catalog examples, but every code snippet below is self-contained.

# Start with the shape you want

Begin schema design from the client’s perspective. While your data model might store `BrandId` in the product, clients typically want to query `product { brand { name } }`.

```graphql
type Product {
  id: ID!
  name: String!
  description: String
  price: Decimal!
  attributes: [KeyValuePairOfStringAndString!]!
  brand: Brand
}

type Brand {
  id: ID!
  name: String!
}
```

Your C# model can include more members than the public schema reveals.

```csharp
#nullable enable

namespace Catalog.Models;

public sealed class Product
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal Price { get; init; }

    public int BrandId { get; init; }

    public Dictionary<string, string> Attributes { get; init; } = new();

    public string LegacyName => Name;

    public string GetInternalStatus() => "internal";
}

public sealed class Brand
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;
}
```

# Let Hot Chocolate infer object types

Hot Chocolate v16 can infer object types from public C# members.

```csharp
using Catalog.Models;

namespace Catalog.Types;

public sealed class Query
{
    public Product GetProduct()
        => new()
        {
            Id = 1,
            Name = "Trail Backpack",
            Description = "A weather resistant backpack.",
            Price = 129.95m,
            BrandId = 7,
            Attributes = new Dictionary<string, string>
            {
                ["Color"] = "Black",
                ["Volume"] = "35L"
            }
        };
}
```

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddQueryType<Query>();
```

With nullable reference types enabled, the inferred schema contains fields for public properties and public methods.

```graphql
type Query {
  product: Product!
}

type Product {
  id: Int!
  name: String!
  description: String
  price: Decimal!
  brandId: Int!
  attributes: [KeyValuePairOfStringAndString!]!
  legacyName: String!
  internalStatus: String!
}

type KeyValuePairOfStringAndString {
  key: String!
  value: String!
}
```

Default object type conventions:

| C# shape                        | GraphQL result                                   |
| ------------------------------- | ------------------------------------------------ |
| Public property with a getter   | Field with the same name in camelCase.           |
| Public method returning a value | Resolver field.                                  |
| `GetProduct`                    | `product`, because `Get` is stripped.            |
| `GetBrandAsync`                 | `brand`, because `Get` and `Async` are stripped. |
| Registered service parameter    | Resolver infrastructure, not a GraphQL argument. |
| `[Parent]` parameter            | Parent value injection, not a GraphQL argument.  |
| `CancellationToken` parameter   | Request cancellation, not a GraphQL argument.    |
| Nullable reference type         | GraphQL nullable field.                          |
| Non-nullable value type         | GraphQL non-null field.                          |

Inference is a good starting point. Configure the type when public members expose persistence details, helper methods, or unstable names.

# Choose a configuration style

| Style                      | Use when                                                                            | Example APIs                                                                                           | Trade-off                                                   |
| -------------------------- | ----------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------- |
| Conventions                | The inferred schema already matches your public API.                                | Public properties and methods.                                                                         | Public members can enter the schema.                        |
| Attributes                 | You need local metadata near the CLR member.                                        | `[GraphQLName]`, `[GraphQLDescription]`, `[GraphQLIgnore]`, `[GraphQLType<T>]`, `[GraphQLDeprecated]`. | GraphQL metadata lives on the CLR type.                     |
| `[ObjectType<T>]` class    | You use the v16 source generator and want type configuration near resolver methods. | `[ObjectType<Product>]`, `static partial void Configure(...)`.                                         | Requires generated type registration, such as `AddTypes()`. |
| `ObjectType<T>` descriptor | You want central code-first schema modules.                                         | `ObjectType<Product>`, `IObjectTypeDescriptor<Product>`, `.AddType<ProductType>()`.                    | More code, with clear schema boundaries.                    |

A practical flow:

```text
Is the inferred schema correct?
  yes -> keep conventions
  no, small local tweak -> use attributes
  no, source generator module fits -> use [ObjectType<T>]
  no, central schema module preferred -> use ObjectType<T>
  need fields from another module -> use a type extension
```

## Configure a type with attributes

Use attributes for compact local changes when GraphQL metadata belongs next to the CLR model. Attribute-based configuration is often useful for names, descriptions, ignored members, explicit field types, and deprecations.

Namespace guidance:

- Use `HotChocolate` for `[GraphQLName]`, `[GraphQLDescription]`, `[GraphQLIgnore]`, `[GraphQLType<T>]`, and `[GraphQLDeprecated]`.
- Use `HotChocolate.Types` when `[GraphQLType<T>]` references GraphQL type classes such as `NonNullType`, `IdType`, `ListType`, or scalar types.

```csharp
using HotChocolate;
using HotChocolate.Types;

namespace Catalog.Models;

[GraphQLName("CatalogProduct")]
[GraphQLDescription("A sellable catalog item.")]
public sealed class Product
{
    [GraphQLType<NonNullType<IdType>>]
    public int Id { get; init; }

    [GraphQLName("displayName")]
    [GraphQLDescription("The display name shown to customers.")]
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal Price { get; init; }

    [GraphQLIgnore]
    public int BrandId { get; init; }

    public Dictionary<string, string> Attributes { get; init; } = new();

    [GraphQLDeprecated("Use displayName instead.")]
    public string LegacyName => Name;

    [GraphQLIgnore]
    public string GetInternalStatus() => "internal";
}
```

Use descriptor APIs instead when you want the schema configuration in a dedicated type module.

# Customize names, descriptions, and deprecations

GraphQL names and nullability are public contracts. Rename a field in the schema when the API contract should change for every client. If one client needs a different response name, use a GraphQL query alias.

```graphql
query ProductCard {
  product {
    displayName: name
  }
}
```

The same contract can be configured with descriptors.

```csharp
protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
{
    descriptor.Name("CatalogProduct");
    descriptor.Description("A sellable catalog item.");

    descriptor.Ignore(t => t.BrandId);
    descriptor.Ignore(t => t.GetInternalStatus());

    descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();

    descriptor
        .Field(t => t.Name)
        .Name("displayName")
        .Description("The display name shown to customers.");

    descriptor
        .Field(t => t.LegacyName)
        .Deprecated("Use displayName instead.");
}
```

Relevant SDL:

```graphql
"""
A sellable catalog item.
"""
type CatalogProduct {
  id: ID!

  """
  The display name shown to customers.
  """
  displayName: String!

  description: String
  price: Decimal!
  attributes: [KeyValuePairOfStringAndString!]!
  legacyName: String! @deprecated(reason: "Use displayName instead.")
}
```

# Hide members and control field binding

Implicit binding includes public members unless you ignore them. That can expose foreign keys and helper methods.

```graphql
type Product {
  id: Int!
  name: String!
  brandId: Int!
  internalStatus: String!
}
```

Ignore individual members when most inferred fields are correct. Use `[GraphQLIgnore]` near the CLR member as shown in [Configure a type with attributes](#configure-a-type-with-attributes), or keep the rule in a descriptor module.

```csharp
protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
{
    descriptor.Ignore(t => t.BrandId);
    descriptor.Ignore(t => t.GetInternalStatus());
}
```

Use explicit binding when you want every object field to be opt-in.

```csharp
using HotChocolate.Types;

public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
        descriptor.Field(t => t.Name);
        descriptor.Field(t => t.Description);
        descriptor.Field(t => t.Price);
    }
}
```

After explicit binding, `brandId` and `internalStatus` do not appear unless you add them.

| Binding API                                                 | What it does                               | Use when                                               |
| ----------------------------------------------------------- | ------------------------------------------ | ------------------------------------------------------ |
| `descriptor.BindFieldsImplicitly()`                         | Includes public members unless ignored.    | Simple DTOs.                                           |
| `descriptor.BindFieldsExplicitly()`                         | Includes configured fields.                | Domain models with helper members or sensitive fields. |
| `descriptor.BindFields(BindingBehavior.Implicit)`           | Sets the binding mode with an enum.        | You choose the mode from shared configuration.         |
| `options.DefaultBindingBehavior = BindingBehavior.Explicit` | Makes explicit binding the schema default. | Your team wants object fields to be opt-in.            |

```csharp
using HotChocolate.Types;

builder
    .AddGraphQL()
    .ModifyOptions(options =>
    {
        options.DefaultBindingBehavior = BindingBehavior.Explicit;
    });
```

# Override field types and nullability

Hot Chocolate infers field types from CLR types and nullable annotations. Override the GraphQL type when the public contract needs a specific scalar, list shape, or nullability.

| CLR shape                                      | Typical GraphQL shape               | Notes                                                 |
| ---------------------------------------------- | ----------------------------------- | ----------------------------------------------------- |
| `string` with nullable reference types enabled | `String!`                           | Non-null output.                                      |
| `string?`                                      | `String`                            | Nullable output.                                      |
| `int`                                          | `Int!`                              | Non-null value type.                                  |
| `int?`                                         | `Int`                               | Nullable value type.                                  |
| `IReadOnlyList<Product>`                       | `[Product!]!`                       | List and item nullability are separate.               |
| `Dictionary<string, string>`                   | `[KeyValuePairOfStringAndString!]!` | v16 maps dictionaries to key-value pair object lists. |

Use `[GraphQLType<T>]` near the CLR member when you configure the type with attributes, as shown in [Configure a type with attributes](#configure-a-type-with-attributes). Use `.Type<T>()` in a descriptor module.

```csharp
descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
descriptor.Field(t => t.Tags).Type<NonNullType<ListType<NonNullType<StringType>>>>();
```

For full rules, see [Non-Null](/docs/hotchocolate/v16/build/schema-elements/lists-and-non-null), [Lists](/docs/hotchocolate/v16/build/schema-elements/lists-and-non-null), and [Scalars](/docs/hotchocolate/v16/build/schema-elements/scalars).

# Add fields with resolvers

Add a resolver field when the value is related data, computed data, or data loaded through a service.

```csharp
using Catalog.Models;

namespace Catalog.Services;

public sealed class BrandService
{
    private readonly Dictionary<int, Brand> _brands = new()
    {
        [7] = new Brand { Id = 7, Name = "Contoso" }
    };

    public Task<Brand?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        _brands.TryGetValue(id, out var brand);
        return Task.FromResult(brand);
    }
}
```

Descriptor resolver:

```csharp
descriptor
    .Field("brand")
    .Type<BrandType>()
    .Resolve(async (context, ct) =>
    {
        var product = context.Parent<Product>();
        var brands = context.Service<BrandService>();
        return await brands.GetByIdAsync(product.BrandId, ct);
    });
```

This field uses `BrandId` internally while exposing `brand` in the schema.

```graphql
type Product {
  id: ID!
  name: String!
  brand: Brand
}
```

If a resolver field loads data per parent object, use batching with [DataLoader](/docs/hotchocolate/v16/build/dataloader). The object type configuration can be correct while the resolver still needs batching.

# Configure explicit type modules

## Register explicit object type configurations

Registration decides whether Hot Chocolate uses conventions or your explicit configuration.

Root operation types define schema entry points. Register the operations your schema exposes:

```csharp
using Catalog.Types;

builder
    .AddGraphQL()
    .AddQueryType<Query>();
```

Use `.AddMutationType<Mutation>()` and `.AddSubscriptionType<Subscription>()` for mutation and subscription root operation types when those classes exist.

Named object type configurations shape regular object types. Returning `Product` from a resolver lets Hot Chocolate infer an object type, but it does not select `ProductType : ObjectType<Product>` or a source-generated `[ObjectType<Product>]` module unless that configuration is registered.

Register descriptor-based object type configurations with `.AddType<T>()`.

```csharp
builder.Services.AddSingleton<BrandService>();

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddType<ProductType>()
    .AddType<BrandType>();
```

Register source-generated `[ObjectType<T>]` modules with the generated registration method for your project. The default v16 template uses `AddTypes()`.

```csharp
builder
    .AddGraphQL()
    .AddTypes();
```

## Configure a type with a v16 ObjectType class

Use `[ObjectType<T>]` classes when your project uses the source generator and you want a type module with resolver methods.

```csharp
using Catalog.Models;
using Catalog.Services;
using HotChocolate;
using HotChocolate.Types;

namespace Catalog.Types;

[ObjectType<Product>]
public static partial class ProductObject
{
    static partial void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Name("Product");
        descriptor.Description("A sellable catalog item.");

        descriptor.Ignore(t => t.BrandId);
        descriptor.Ignore(t => t.GetInternalStatus());

        descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
        descriptor.Field(t => t.Name).Description("The display name shown to customers.");
        descriptor.Field(t => t.LegacyName).Deprecated("Use name instead.");
    }

    public static async Task<Brand?> GetBrandAsync(
        [Parent] Product product,
        BrandService brands,
        CancellationToken ct)
        => await brands.GetByIdAsync(product.BrandId, ct);
}
```

The `[ObjectType<T>]` attribute is a marker for generated code. Put descriptor configuration in the `Configure` partial method, then register generated type modules with `AddTypes()` or your project's generated registration method.

## Configure a type with ObjectType<T>

Use `ObjectType<T>` when you prefer explicit code-first modules.

```csharp
using Catalog.Models;
using Catalog.Services;
using HotChocolate.Types;

namespace Catalog.Types;

public sealed class BrandType : ObjectType<Brand>
{
    protected override void Configure(IObjectTypeDescriptor<Brand> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
        descriptor.Field(t => t.Name);
    }
}

public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Name("Product");
        descriptor.Description("A sellable catalog item.");
        descriptor.BindFieldsExplicitly();

        descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
        descriptor.Field(t => t.Name).Description("The display name shown to customers.");
        descriptor.Field(t => t.Description);
        descriptor.Field(t => t.Price);
        descriptor.Field(t => t.Attributes);
        descriptor.Field(t => t.LegacyName).Deprecated("Use name instead.");

        descriptor
            .Field("brand")
            .Type<BrandType>()
            .Resolve(async (context, ct) =>
            {
                var product = context.Parent<Product>();
                var brands = context.Service<BrandService>();
                return await brands.GetByIdAsync(product.BrandId, ct);
            });
    }
}
```

Register `ProductType` and `BrandType` with `.AddType<T>()` as shown above.

Expected SDL:

```graphql
type Query {
  product: Product!
}

"""
A sellable catalog item.
"""
type Product {
  id: ID!

  """
  The display name shown to customers.
  """
  name: String!

  description: String
  price: Decimal!
  attributes: [KeyValuePairOfStringAndString!]!
  legacyName: String! @deprecated(reason: "Use name instead.")
  brand: Brand
}

type Brand {
  id: ID!
  name: String!
}

type KeyValuePairOfStringAndString {
  key: String!
  value: String!
}
```

Client query:

```graphql
{
  product {
    name
    brand {
      name
    }
  }
}
```

Example result:

```json
{
  "data": {
    "product": {
      "name": "Trail Backpack",
      "brand": {
        "name": "Contoso"
      }
    }
  }
}
```

# Model relationships without leaking persistence details

Keep database keys in the C# model when your storage needs them, but expose object fields when clients navigate the graph.

| Storage detail     | Public GraphQL field                    | Why                                                        |
| ------------------ | --------------------------------------- | ---------------------------------------------------------- |
| `Product.BrandId`  | `Product.brand`                         | Clients can select brand fields in one graph shape.        |
| Join table IDs     | List field such as `Product.categories` | Clients see domain relationships, not relational plumbing. |
| Computed file name | `Product.imageUrl`                      | Clients get a usable value.                                |

Implementation choices:

- Add a resolver method in a `[ObjectType<T>]` class.
- Add a descriptor field with `.Field("brand").Resolve(...)`.
- Use a [type extension](/docs/hotchocolate/v16/build/schema-elements/extending-types) when another module owns the added field.

For resolver execution details, see [Resolvers](/docs/hotchocolate/v16/build/resolvers). For efficient related data fetching, see [DataLoader](/docs/hotchocolate/v16/build/dataloader). For global IDs and node lookups, see [Relay](/docs/hotchocolate/v16/build/schema-elements/relay).

# Use dictionary properties in v16

Hot Chocolate v16 maps `Dictionary<TKey, TValue>` properties to lists of generated key-value pair object types. For `Dictionary<string, string>`, the generated SDL is:

```graphql
type Product {
  attributes: [KeyValuePairOfStringAndString!]!
}

type KeyValuePairOfStringAndString {
  key: String!
  value: String!
}
```

Clients query dictionary data as a list.

```graphql
{
  product {
    attributes {
      key
      value
    }
  }
}
```

Generated key-value pair type names depend on the key and value types. Include expected SDL in tests or schema review when dictionary fields are part of your public API.

# Connect object types to related schema elements

Object types are central, but nearby schema elements have their own design rules.

| Need                                        | Use                           | Learn more                                                                             |
| ------------------------------------------- | ----------------------------- | -------------------------------------------------------------------------------------- |
| Structured input for mutations or arguments | Input object type             | [Input Object Types](/docs/hotchocolate/v16/build/schema-elements/input-object-types)  |
| Shared output fields across types           | Interface                     | [Interfaces](/docs/hotchocolate/v16/build/schema-elements/interfaces)                  |
| Polymorphic output without shared fields    | Union                         | [Unions](/docs/hotchocolate/v16/build/schema-elements/unions)                          |
| Add fields from another module              | Type extension                | [Extending Types](/docs/hotchocolate/v16/build/schema-elements/extending-types)        |
| Query entry points                          | Query root object type        | [Queries](/docs/hotchocolate/v16/build/schema-elements/operations-queries)             |
| Writes                                      | Mutation root object type     | [Mutations](/docs/hotchocolate/v16/build/schema-elements/operations-mutations)         |
| Event streams                               | Subscription root object type | [Subscriptions](/docs/hotchocolate/v16/build/schema-elements/operations-subscriptions) |

### Link object types to interfaces

Object types can implement interfaces with descriptor APIs such as `descriptor.Implements<NodeType>()`. Register implementing object types when they are not otherwise discovered. See [Interfaces](/docs/hotchocolate/v16/build/schema-elements/interfaces) for C# interface modeling and interface type registration.

### Link object types to type extensions

Type extensions merge fields into the final object type at schema build time. Use `[ExtendObjectType<T>]`, `ObjectTypeExtension<T>`, or `.AddTypeExtension<T>()`. See [Extending Types](/docs/hotchocolate/v16/build/schema-elements/extending-types) for adding, replacing, and removing fields.

# Troubleshoot object types

## My ObjectType<T> configuration is ignored

- Register it with `.AddType<ProductType>()`.
- Check that the resolver returns the runtime type configured by the descriptor.
- Look for another descriptor or generated `[ObjectType<T>]` class configuring the same runtime type.

## A public method appeared as a GraphQL field

- Public methods can be inferred as resolver fields.
- Use `[GraphQLIgnore]`, descriptor ignore, or explicit binding.
- Apply the naming rules: `GetInternalStatus` becomes `internalStatus`, and `GetBrandAsync` becomes `brand`.

## A field has the wrong GraphQL name

- Check camelCase conversion, stripped `Get` prefixes, and stripped `Async` suffixes.
- Use `[GraphQLName]` or `.Name(...)` for schema-level renames.
- Use a query alias for one client response shape.

## A field has the wrong nullability or type

- Check nullable reference type settings in the project.
- Check list nullability and item nullability separately.
- Use `[GraphQLType<T>]`, `[GraphQLType("...")]`, or `.Type<T>()` for explicit overrides.
- See [Non-Null](/docs/hotchocolate/v16/build/schema-elements/lists-and-non-null), [Lists](/docs/hotchocolate/v16/build/schema-elements/lists-and-non-null), and [Scalars](/docs/hotchocolate/v16/build/schema-elements/scalars).

## A resolver field causes repeated database calls

- Keep the object type configuration.
- Move data access to a batched resolver or DataLoader.
- See [DataLoader](/docs/hotchocolate/v16/build/dataloader).

## A dictionary field has an unexpected generated type name

- Check the key and value CLR types.
- Compare your schema SDL with the expected generated key-value pair type.

# Reference

| Need                | Attribute or source generator style          | Descriptor API                                     |
| ------------------- | -------------------------------------------- | -------------------------------------------------- |
| Rename type         | `[GraphQLName("CatalogProduct")]`            | `descriptor.Name("CatalogProduct")`                |
| Rename field        | `[GraphQLName("displayName")]`               | `descriptor.Field(...).Name("displayName")`        |
| Add description     | `[GraphQLDescription("...")]` or XML docs    | `.Description("...")`                              |
| Deprecate field     | `[GraphQLDeprecated("Use name instead.")]`   | `.Deprecated("Use name instead.")`                 |
| Hide member         | `[GraphQLIgnore]`                            | `descriptor.Ignore(...)` or `.Field(...).Ignore()` |
| Override field type | `[GraphQLType<T>]` or `[GraphQLType("...")]` | `.Type<T>()`                                       |
| Add resolver field  | Resolver method in `[ObjectType<T>]` class   | `.Field("name").Resolve(...)`                      |
| Implement interface | Register an implementing object type         | `.Implements<TInterfaceType>()`                    |
| Explicit binding    | Configure in `[ObjectType<T>]` class         | `.BindFieldsExplicitly()`                          |

Common descriptor APIs:

| Level        | APIs                                                                                                                    |
| ------------ | ----------------------------------------------------------------------------------------------------------------------- |
| Object type  | `Name`, `Description`, `BindFields`, `BindFieldsExplicitly`, `BindFieldsImplicitly`, `Implements`, `Field`, `Directive` |
| Object field | `Name`, `Description`, `Deprecated`, `Type`, `Argument`, `Ignore`, `Resolve`, `ResolveWith`, `StreamResult`             |

# Next steps

- Define entry points with [Queries](/docs/hotchocolate/v16/build/schema-elements/operations-queries), [Mutations](/docs/hotchocolate/v16/build/schema-elements/operations-mutations), and [Subscriptions](/docs/hotchocolate/v16/build/schema-elements/operations-subscriptions).
- Define input payloads with [Input Object Types](/docs/hotchocolate/v16/build/schema-elements/input-object-types).
- Share output fields with [Interfaces](/docs/hotchocolate/v16/build/schema-elements/interfaces) or choose polymorphic results with [Unions](/docs/hotchocolate/v16/build/schema-elements/unions).
- Add fields from another module with [Extending Types](/docs/hotchocolate/v16/build/schema-elements/extending-types).
- Tune nullability and collections with [Non-Null](/docs/hotchocolate/v16/build/schema-elements/lists-and-non-null) and [Lists](/docs/hotchocolate/v16/build/schema-elements/lists-and-non-null).
