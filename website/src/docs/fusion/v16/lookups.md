---
title: "Lookups"
---

Lookups are essential for resolving entities in Fusion. They inform the gateway how to obtain an instance of a type based on certain identifiers.

# Defining Lookups

A lookup is defined by any field or operation that returns an entity and takes arguments that can uniquely identify it. For example:

```graphql
# Products Subgraph
type Query {
  productById(id: ID!): Product @lookup
  productBySku(sku: String!): Product @lookup
}
```

In this case, `productById` and `productBySku` are lookups for the `Product` entity, using `id` and `sku` as keys, respectively.

## How Lookups Work

When the gateway needs to resolve an entity, it uses the available lookups to fetch the required data. The process is as follows:

1. **Identify the Required Entity Fields**

   The gateway determines which fields of the entity are requested by the client and which subgraphs can provide them.

2. **Select the Appropriate Lookup**

   Based on the available lookups and the data at hand, the gateway chooses the most suitable lookup to retrieve the entity.

3. **Fetch Data Across Subgraphs**

   The gateway orchestrates calls to the relevant subgraphs, using the lookups to fetch and assemble the entity's data.

# Implicit vs. Explicit Lookups

By default in Fusion, lookups are implicit, meaning you don't need to explicitly annotate fields as lookups. Instead, the gateway infers lookups based on the arguments of root fields.

It's good practice to explicitly mark fields as lookups using the `@lookup` directive to make your schema more readable and maintainable. In the future, explicitly defining lookups will be the standard.

# Internal Lookups

By default all lookups are public, meaning they can not only be used to resolve additional fields by the gateway, but also used as entry points in a query. Sometimes this is not what you want.

Let say you have a Product and a Review service. The Review service has a lookup `productById` to resolve the reviews for a product. If a product does not have any reviews, this subgraph does not know this product. If we execute the query `productById(id: 1) {review {id}}` the gateway will plan the request in the most efficient way and will call the Review service with the lookup `productById` with the argument `id: 1`. The Review service will not find any reviews and will return null. This is not what we want.

To prevent this, you can mark the lookup as internal. This means that the lookup can only be used by the gateway to resolve additional fields, but not as an entry point in a query. To mark a lookup as internal, you can use the `@internal` directive.

```graphql
# Reviews Subgraph
type Query {
  productById(id: ID!): Product @lookup @internal
}
```

To annotate lookups in your source code you need the `HotChocolate.Fusion.SourceSchema` NuGet package. This package provides you with the `[Lookup]` and `[Internal]` attributes to mark your lookups as internal.

```csharp
public static class ProductOperations
{
    [Query]
    [Lookup]
    [Internal]
    public static Product GetProductById(int id)
    {
        return new Product
        {
            Id = id,
            Name = $"Product {id}",
            Sku = $"SKU{id}",
            Description = $"Description {id}",
            Price = id
        };
    }
}
```

# The `@is` Directive

The `@is` directive is used on lookup fields to define how arguments map to the fields of the entity type that the lookup field resolves. This mapping creates semantic equivalence between different members of the type system across source schemas, particularly in cases where arguments do not directly align with the fields on the entity type.

In the example below, the `@is` directive specifies that the `id` argument on the field `Query.personById` is semantically equivalent to the `id` field on the `Person` type returned by the field:

```graphql
extend type Query {
  personById(id: ID! @is(field: "id")): Person @lookup
}
```

Here, the `@is` directive maps the `id` argument of `Query.personById` to the `id` field on the `Person` type. This ensures that the lookup resolves correctly by establishing the equivalence.

## Optional Use of `@is`

In cases where the argument name and the field name match (as in this example), the `@is` directive can be omitted, as the mapping is implied. However, the directive is essential when the argument name does not directly correspond to a field on the entity type.
