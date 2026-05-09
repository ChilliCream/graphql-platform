---
title: Apollo Federation attributes
---

Use Apollo Federation attributes when a Hot Chocolate v16 subgraph must emit Apollo Federation compatible SDL. The attributes in this page come from `HotChocolate.ApolloFederation` and configure the SDL that a router or composition tool reads.

For new Fusion native subgraphs, prefer the Fusion attributes in `HotChocolate.Types.Composite`, such as `[Lookup]`, `[EntityKey]`, `[Is]`, and `[Require]`. Use the Apollo Federation attributes on this page when you are building a Federation compatible subgraph, migrating an existing Federation schema, or interoperating with Apollo Federation composition.

## Configure Apollo Federation support

Install `HotChocolate.ApolloFederation`, then register Federation support on the GraphQL builder.

```csharp
using HotChocolate.ApolloFederation;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddApolloFederation();
```

`AddApolloFederation()` defaults to `FederationVersion.Federation26`. Use an explicit version when you need a directive that is gated by a newer Federation release.

```csharp
builder
    .AddGraphQL()
    .AddApolloFederation(FederationVersion.Federation27);
```

Hot Chocolate v16 supports `Federation10` and Federation v2.0 through v2.7. Federation v2 output uses `@link` imports for the Federation directives that appear in the schema. If you select a version that is too low for a directive, schema creation fails.

## When to use attributes

Attributes work well when the Federation directive belongs with the C# type, property, or resolver method.

```csharp
using HotChocolate.ApolloFederation.Types;

[Key("id")]
public sealed class Product
{
    public int Id { get; init; }
}
```

The descriptor API is clearer when configuration is centralized, conditional, generated, or cannot be placed on the runtime type.

```csharp
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Key("id");
    }
}
```

Both examples emit the same entity key.

```graphql
type Product @key(fields: "id") {
  id: Int!
}
```

Field set strings use GraphQL schema field names, not C# member names. If a field is renamed with `[GraphQLName]` or naming conventions, write the field set with the final GraphQL name.

## Attribute quick reference

| Attribute             | Namespace                                 | C# targets                                                         | Generated SDL                                   | Important arguments                    | Min Federation version               | Fluent equivalent            |
| --------------------- | ----------------------------------------- | ------------------------------------------------------------------ | ----------------------------------------------- | -------------------------------------- | ------------------------------------ | ---------------------------- |
| `[Key]`               | `HotChocolate.ApolloFederation.Types`     | Class, interface, property                                         | `@key`                                          | `fieldSet`, `resolvable`               | v1, v2                               | `descriptor.Key(...)`        |
| `[ReferenceResolver]` | `HotChocolate.ApolloFederation.Resolvers` | Class, interface, struct, method                                   | No SDL directive. Registers an entity resolver. | `EntityResolver`, `EntityResolverType` | v1, v2                               | `.ResolveReferenceWith(...)` |
| `[Map]`               | `HotChocolate.ApolloFederation.Resolvers` | Parameter                                                          | No SDL directive. Maps representation data.     | `path`                                 | v1, v2                               | Resolver parameter binding   |
| `[ExtendServiceType]` | `HotChocolate.ApolloFederation.Types`     | Class, struct, interface                                           | `@extends` in Federation v1 output              | None                                   | v1 compatibility                     | `.ExtendServiceType()`       |
| `[External]`          | `HotChocolate.ApolloFederation.Types`     | Class, struct, method, property                                    | `@external`                                     | None                                   | v1 field use, v2 object or field use | `.External()`                |
| `[Requires]`          | `HotChocolate.ApolloFederation.Types`     | Method, property                                                   | `@requires`                                     | `fieldSet`                             | v1, v2                               | `.Requires(...)`             |
| `[Provides]`          | `HotChocolate.ApolloFederation.Types`     | Method, property                                                   | `@provides`                                     | `fieldSet`                             | v1, v2                               | `.Provides(...)`             |
| `[Shareable]`         | `HotChocolate.ApolloFederation.Types`     | Class, struct, method, property                                    | `@shareable`                                    | None                                   | v2.0                                 | `.Shareable()`               |
| `[Inaccessible]`      | `HotChocolate.ApolloFederation.Types`     | Class, enum, field, interface, method, parameter, property, struct | `@inaccessible`                                 | None                                   | v2.0                                 | `.Inaccessible()`            |
| `[Override]`          | `HotChocolate.ApolloFederation.Types`     | Method, property                                                   | `@override`                                     | `from`, optional `label`               | v2.0. `label` requires v2.7.         | `.Override(...)`             |
| `[InterfaceObject]`   | `HotChocolate.ApolloFederation.Types`     | Class                                                              | `@interfaceObject`                              | None                                   | v2.3                                 | `.InterfaceObject()`         |
| `[Authenticated]`     | `HotChocolate.ApolloFederation.Types`     | Class, enum, interface, method, property, struct                   | `@authenticated`                                | None                                   | v2.5                                 | `.Authenticated()`           |
| `[RequiresScopes]`    | `HotChocolate.ApolloFederation.Types`     | Class, enum, interface, method, property, struct                   | `@requiresScopes`                               | `scopes`                               | v2.5                                 | `.RequiresScopes(...)`       |
| `[Policy]`            | `HotChocolate.ApolloFederation.Types`     | Class, enum, interface, method, property, struct                   | `@policy`                                       | `policies`                             | v2.6                                 | `.Policy(...)`               |
| `[Tag]`               | `HotChocolate.Types`                      | Class, struct, interface, enum, property, method, field, parameter | `@tag`                                          | `name`                                 | v2.0 import in Federation output     | `.Tag(...)`                  |

`@composeDirective`, custom directive export, router deployment, and supergraph publishing are outside this attribute reference.

## Define entity keys with `[Key]`

Use `[Key]` to tell Federation how an entity is identified.

### Single field key

Apply `[Key]` to a property when the property itself is the key field. Do not pass a field set to property level `[Key]`.

```csharp
using HotChocolate;
using HotChocolate.ApolloFederation.Types;

public sealed class Product
{
    [ID]
    [Key]
    public string Id { get; init; } = default!;

    public string Name { get; init; } = default!;
}
```

Generated SDL:

```graphql
type Product @key(fields: "id") {
  id: ID!
  name: String!
}
```

### Composite and nested keys

Apply `[Key("...")]` to the type when the key uses multiple fields or a nested selection.

```csharp
using HotChocolate.ApolloFederation.Types;

[Key("sku package")]
[Key("sku variation { id }")]
public sealed class Product
{
    public string Sku { get; init; } = default!;

    public string Package { get; init; } = default!;

    public ProductVariation Variation { get; init; } = default!;
}

public sealed class ProductVariation
{
    public string Id { get; init; } = default!;
}
```

Generated SDL:

```graphql
type Product @key(fields: "sku package") @key(fields: "sku variation { id }") {
  sku: String!
  package: String!
  variation: ProductVariation!
}
```

A field set is a GraphQL selection set without the outer braces. Multiple `[Key]` attributes emit repeatable `@key` directives. If the subgraph resolves the entity by each key, provide a matching reference resolver for each key shape.

### Non-resolvable key

Use `resolvable: false` when the subgraph can reference an entity but should not resolve it through `_entities`.

```csharp
using HotChocolate.ApolloFederation.Types;

[Key("id", resolvable: false)]
public sealed class ProductReference
{
    public int Id { get; init; }
}
```

Generated SDL:

```graphql
type ProductReference @key(fields: "id", resolvable: false) {
  id: Int!
}
```

## Resolve entities with `[ReferenceResolver]`

A reference resolver turns an `_entities` representation into the local entity object. It does not emit a directive, but Federation uses it when this subgraph contributes fields to an entity.

### Resolve by key on the entity type

Place `[ReferenceResolver]` on a static method of the entity type.

```csharp
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;

[Key("id")]
public sealed class Product
{
    public string Id { get; init; } = default!;

    public string Name { get; init; } = default!;

    [ReferenceResolver]
    public static Task<Product?> ResolveReferenceAsync(
        string id,
        ProductByIdDataLoader products,
        CancellationToken cancellationToken)
        => products.LoadAsync(id, cancellationToken);
}
```

Resolver rules:

- The resolver method must be discoverable by Hot Chocolate. A public static method on the entity type is the common pattern.
- Key parameter names and CLR types must match the GraphQL key fields.
- Return `T?` or `Task<T?>` when a representation may not resolve.
- Use DataLoader for database access so batches across `_entities` can be grouped.
- Add one resolver per key shape that this subgraph resolves.

### Resolve with a named method

Put `[ReferenceResolver]` on the type when you want the resolver method name to be explicit.

```csharp
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;

[Key("id")]
[ReferenceResolver(EntityResolver = nameof(ResolveByIdAsync))]
public sealed class Product
{
    public string Id { get; init; } = default!;

    public string Name { get; init; } = default!;

    public static Task<Product?> ResolveByIdAsync(
        string id,
        ProductByIdDataLoader products,
        CancellationToken cancellationToken)
        => products.LoadAsync(id, cancellationToken);
}
```

### Resolve with an external resolver type

Use `EntityResolverType` when you keep resolver methods outside the entity class.

```csharp
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;

[Key("id")]
[ReferenceResolver(
    EntityResolverType = typeof(ProductReferenceResolvers),
    EntityResolver = nameof(ProductReferenceResolvers.GetByIdAsync))]
public sealed class Product
{
    public string Id { get; init; } = default!;

    public string Name { get; init; } = default!;
}

public static class ProductReferenceResolvers
{
    public static Task<Product?> GetByIdAsync(
        string id,
        ProductByIdDataLoader products,
        CancellationToken cancellationToken)
        => products.LoadAsync(id, cancellationToken);
}
```

### Map nested representation values with `[Map]`

Use `[Map]` when the representation contains a nested object and the resolver parameter should read a nested value.

```csharp
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;

[Key("sku variation { id }")]
public sealed class Product
{
    public string Sku { get; init; } = default!;

    public ProductVariation Variation { get; init; } = default!;

    [ReferenceResolver]
    public static Product? ResolveByVariation(
        string sku,
        [Map("variation.id")] string variationId,
        ProductRepository products)
        => products.FindByVariation(sku, variationId);
}
```

The representation passed to `_entities` contains the nested value.

```json
{
  "__typename": "Product",
  "sku": "apollo-federation-shirt",
  "variation": { "id": "red-xl" }
}
```

### Test a reference resolver through `_entities`

Use `_entities` to validate Federation metadata during tests. Normal API clients should query the public schema, not `_entities`.

```graphql
query {
  _entities(representations: [{ __typename: "Product", id: "1" }]) {
    ... on Product {
      id
      name
    }
  }
}
```

Expected result shape:

```json
{
  "data": {
    "_entities": [
      {
        "id": "1",
        "name": "Chilli Sauce"
      }
    ]
  }
}
```

In automated tests, build the schema with `.AddApolloFederation()`, execute an `_entities` query, and snapshot the result or the exported SDL.

## Reference fields owned by another subgraph

A subgraph can return or extend an entity that another subgraph owns. The type name and key must match the composed entity.

```csharp
using HotChocolate.ApolloFederation.Types;

[Key("email")]
[ExtendServiceType]
public sealed class User
{
    [External]
    public string Email { get; init; } = default!;

    [External]
    public string DisplayName { get; init; } = default!;
}
```

Federation v1 output uses `@extends`.

```graphql
type User @extends @key(fields: "email") {
  email: String! @external
  displayName: String! @external
}
```

In Federation v2, smart entity merging usually means `@extends` is not needed. Key fields do not need `@external` only because they are keys. Mark a field external when the field is owned by another subgraph and this subgraph references it from `@requires` or `@provides`.

## Compute a field with `[Requires]`

Use `[Requires]` when a field in this subgraph needs external entity fields to compute its value.

```csharp
using HotChocolate.ApolloFederation.Types;

[Key("id")]
public sealed class User
{
    public int Id { get; init; }

    [External]
    public string? FirstName { get; init; }

    [External]
    public string? LastName { get; init; }

    [Requires("firstName lastName")]
    public string? FullName =>
        FirstName is null && LastName is null
            ? null
            : $"{FirstName} {LastName}".Trim();
}
```

Generated SDL:

```graphql
type User @key(fields: "id") {
  id: Int!
  firstName: String @external
  lastName: String @external
  fullName: String @requires(fields: "firstName lastName")
}
```

The router includes the required external fields in the representation before resolving `fullName`. The field set must use GraphQL field names.

## Provide fields on a returned entity with `[Provides]`

Use `[Provides]` on a field that returns an entity when this subgraph can supply selected fields of that returned entity along this query path.

```csharp
using HotChocolate.ApolloFederation.Types;

[Key("id")]
public sealed class Review
{
    public int Id { get; init; }

    [Provides("name")]
    public Product Product { get; init; } = default!;
}

[Key("id")]
public sealed class Product
{
    public int Id { get; init; }

    [External]
    public string Name { get; init; } = default!;
}
```

Generated SDL:

```graphql
type Review @key(fields: "id") {
  id: Int!
  product: Product! @provides(fields: "name")
}

type Product @key(fields: "id") {
  id: Int!
  name: String! @external
}
```

`[Provides]` is valid on fields that return entities. The provided fields must exist on the returned entity and the field set must use GraphQL names.

## Share ownership with `[Shareable]`

Use `[Shareable]` when more than one subgraph intentionally resolves the same object or field with the same meaning.

```csharp
using HotChocolate.ApolloFederation.Types;

[Key("id")]
public sealed class Product
{
    public int Id { get; init; }

    [Shareable]
    public string Name { get; init; } = default!;
}
```

Generated SDL:

```graphql
type Product @key(fields: "id") {
  id: Int!
  name: String! @shareable
}
```

You can also mark the object type shareable.

```csharp
[Shareable]
public sealed class Money
{
    public decimal Amount { get; init; }

    public string Currency { get; init; } = default!;
}
```

Generated SDL:

```graphql
type Money @shareable {
  amount: Decimal!
  currency: String!
}
```

Federation v2 treats key fields as shareable. Use explicit `[Shareable]` for non-key fields or types that multiple subgraphs resolve intentionally.

## Hide schema elements with `[Inaccessible]`

Use `[Inaccessible]` when a field or type is needed for composition or query planning but should be hidden from the client-facing supergraph API.

```csharp
using HotChocolate.ApolloFederation.Types;

[Key("internalId")]
public sealed class Product
{
    [Inaccessible]
    public string InternalId { get; init; } = default!;

    public string Name { get; init; } = default!;
}
```

Generated SDL:

```graphql
type Product @key(fields: "internalId") {
  internalId: String! @inaccessible
  name: String!
}
```

`[Inaccessible]` is an Apollo Federation directive. Fusion native subgraphs use `[Internal]` for Fusion-specific helper fields and lookups.

## Move field ownership with `[Override]`

Use `[Override]` when this subgraph takes responsibility for resolving a field from another published subgraph. The `from` value must match the source subgraph name used during publishing or composition.

```csharp
using HotChocolate.ApolloFederation.Types;

[Key("id")]
public sealed class Product
{
    public int Id { get; init; }

    [Override("Products")]
    public string Description { get; init; } = default!;
}
```

Generated SDL:

```graphql
type Product @key(fields: "id") {
  id: Int!
  description: String! @override(from: "Products")
}
```

Progressive override labels require Federation v2.7.

```csharp
builder
    .AddGraphQL()
    .AddApolloFederation(FederationVersion.Federation27);

public sealed class Product
{
    public int Id { get; init; }

    [Override("Products", "percent(10)")]
    public string Description { get; init; } = default!;
}
```

Generated SDL:

```graphql
description: String! @override(from: "Products", label: "percent(10)")
```

## Add interface entity fields with `[InterfaceObject]`

Use `[InterfaceObject]` for the Federation v2.3 interface object pattern. It marks an object in this subgraph as contributing fields to an entity interface in the supergraph.

```csharp
using HotChocolate.ApolloFederation.Types;

[Key("id")]
[InterfaceObject]
public sealed class Product
{
    public int Id { get; init; }

    public int ReviewCount { get; init; }
}
```

Generated SDL:

```graphql
type Product @key(fields: "id") @interfaceObject {
  id: Int!
  reviewCount: Int!
}
```

Use this only when your composed Federation schema models an entity interface. For ordinary object entities, use `[Key]` and reference resolvers.

## Add Apollo Router authorization metadata

`[Authenticated]`, `[RequiresScopes]`, and `[Policy]` emit Apollo Federation directives for Apollo Router composition and routing decisions. They do not enforce Hot Chocolate server-side authorization by themselves. Use Hot Chocolate authentication and `[Authorize]` when the subgraph must enforce access at execution time.

### Require authentication with `[Authenticated]`

```csharp
using HotChocolate.ApolloFederation.Types;

[Key("id")]
public sealed class Product
{
    public int Id { get; init; }

    [Authenticated]
    public string SupplierNotes { get; init; } = default!;
}
```

Generated SDL:

```graphql
type Product @key(fields: "id") {
  id: Int!
  supplierNotes: String! @authenticated
}
```

`[Authenticated]` requires Federation v2.5 or later.

### Require scopes with `[RequiresScopes]`

```csharp
using HotChocolate.ApolloFederation.Types;

public sealed class Query
{
    [RequiresScopes(["read:products"])]
    public Product GetProduct(int id) => new() { Id = id };
}
```

Generated SDL:

```graphql
type Query {
  product(id: Int!): Product! @requiresScopes(scopes: [["read:products"]])
}
```

Each string in the array is one alternative. Comma-separated values inside one string become one required group.

```csharp
[RequiresScopes(["read:products, read:inventory", "admin"])]
public Product GetInventoryProduct(int id) => new() { Id = id };
```

Generated SDL:

```graphql
inventoryProduct(id: Int!): Product!
  @requiresScopes(scopes: [["read:products", "read:inventory"], ["admin"]])
```

`[RequiresScopes]` can be applied multiple times and requires Federation v2.5 or later.

### Require router policies with `[Policy]`

```csharp
using HotChocolate.ApolloFederation.Types;

public sealed class Query
{
    [Policy(["inventory-admin"])]
    public Product GetInventoryProduct(int id) => new() { Id = id };
}
```

Generated SDL:

```graphql
type Query {
  inventoryProduct(id: Int!): Product! @policy(policies: [["inventory-admin"]])
}
```

Policies are evaluated by Apollo Router configuration, Rhai scripts, or coprocessors. Hot Chocolate does not evaluate these Federation router policies by default. `[Policy]` requires Federation v2.6 or later.

## Add metadata with `[Tag]`

Use `[Tag]` from `HotChocolate.Types` to add repeatable metadata.

```csharp
using HotChocolate.Types;

[Tag("team:catalog")]
[Tag("domain:products")]
public sealed class Product
{
    public int Id { get; init; }
}
```

Generated SDL:

```graphql
type Product @tag(name: "team:catalog") @tag(name: "domain:products") {
  id: Int!
}
```

When Apollo Federation v2 output uses `[Tag]`, Hot Chocolate imports `@tag` through the Federation `@link` directive.

## Descriptor equivalents

| Attribute                     | Descriptor equivalent        | Notes                                                           |
| ----------------------------- | ---------------------------- | --------------------------------------------------------------- |
| `[Key("id")]`                 | `descriptor.Key("id")`       | Entity identity.                                                |
| `[ReferenceResolver]`         | `.ResolveReferenceWith(...)` | Registers an entity resolver and emits no directive.            |
| `[External]`                  | `.External()`                | Commonly field-level.                                           |
| `[Requires("firstName")]`     | `.Requires("firstName")`     | Field set uses GraphQL names.                                   |
| `[Provides("name")]`          | `.Provides("name")`          | Field must return an entity.                                    |
| `[Shareable]`                 | `.Shareable()`               | Federation v2.                                                  |
| `[Inaccessible]`              | `.Inaccessible()`            | Federation v2.                                                  |
| `[Override("Products")]`      | `.Override("Products")`      | Field migration.                                                |
| `[InterfaceObject]`           | `.InterfaceObject()`         | Federation v2.3.                                                |
| `[Authenticated]`             | `.Authenticated()`           | Router metadata.                                                |
| `[RequiresScopes(["scope"])]` | `.RequiresScopes(["scope"])` | Router metadata.                                                |
| `[Policy(["policy"])]`        | `.Policy(["policy"])`        | Router metadata.                                                |
| `[Tag("name")]`               | `.Tag("name")`               | General Hot Chocolate tag directive, imported by Federation v2. |

## Test the generated SDL

Validate the subgraph before composing it. A test can build the schema and snapshot the SDL.

```csharp
using Microsoft.Extensions.DependencyInjection;

var schema = await new ServiceCollection()
    .AddGraphQL()
    .AddApolloFederation(FederationVersion.Federation27)
    .AddType<Product>()
    .BuildSchemaAsync();

var sdl = schema.ToString();
```

Check the SDL for:

- The expected `@key`, `@external`, `@requires`, `@provides`, or other directive.
- Federation v2 `@link` imports for the directives in use.
- GraphQL field names in every field set.
- The expected `_Entity` union and `_entities` field when the schema contains resolvable entities.

Also execute an `_entities` query for each key shape that the subgraph resolves. This catches parameter name mismatches and missing `[Map]` paths earlier than composition.

## Troubleshooting

| Symptom                                                               | What to check                                                                                                                                                                                                                                  |
| --------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| A Federation directive is missing from SDL                            | Confirm `.AddApolloFederation()` is registered, the attribute namespace is correct, the attributed member is part of the GraphQL schema, and the configured Federation version supports the directive.                                         |
| A field set does not match the schema                                 | Use GraphQL names, not C# names. Composite and nested field sets use selection syntax without outer braces, for example `sku variation { id }`.                                                                                                |
| The reference resolver is not found                                   | Check `EntityResolver`, `EntityResolverType`, method visibility, static usage, and parameter names.                                                                                                                                            |
| `_entities` returns `null`                                            | Verify `__typename`, key fields, CLR key conversions, DataLoader lookups, and nullable return handling.                                                                                                                                        |
| `[Map]` does not bind a value                                         | Use a dot-separated path that matches the representation shape, such as `variation.id`.                                                                                                                                                        |
| `@external`, `@requires`, or `@provides` causes composition errors    | Verify every referenced field exists, fields owned elsewhere are external where needed, `[Provides]` is on a field returning an entity, and field sets use GraphQL names.                                                                      |
| A directive is not supported by the selected Federation version       | Select a higher version with `.AddApolloFederation(FederationVersion.Federation27)`. `[InterfaceObject]` needs v2.3, `[Authenticated]` and `[RequiresScopes]` need v2.5, `[Policy]` needs v2.6, and progressive `[Override]` labels need v2.7. |
| Router authorization metadata does not block direct subgraph requests | Add Hot Chocolate authentication and authorization in the subgraph. Federation router directives are not a replacement for server-side enforcement.                                                                                            |

## Next steps

- Review general Apollo Federation setup in [Apollo Federation Subgraph Support](/docs/hotchocolate/v16/api-reference/apollo-federation).
- Learn general attribute mechanics in [Attribute Reference](/docs/hotchocolate/v16/api-reference/custom-attributes).
- Read [Object Types](/docs/hotchocolate/v16/build2/schema-elements/object-types), [Resolvers](/docs/hotchocolate/v16/build2/resolvers), and [DataLoader](/docs/hotchocolate/v16/build2/dataloader) for schema and resolver patterns.
- For Fusion native entity modeling, use [Entities and Lookups](/docs/fusion/v16/entities-and-lookups) and [Coming from Apollo Federation](/docs/fusion/v16/migration/coming-from-apollo-federation).
