# Transform_NestedListKey

## Apollo Federation SDL

```graphql
schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
  query: Query
}

type ProductList @key(fields: "products { id }") {
  products: [Product!]!
}

type Product @key(fields: "id") {
  id: ID!
}

type Query {
  topProducts: ProductList!
  _service: _Service!
  _entities(representations: [_Any!]!): [_Entity]!
}

type _Service { sdl: String! }

union _Entity = ProductList | Product

scalar FieldSet
scalar _Any

directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
directive @link(url: String! import: [String!]) repeatable on SCHEMA
```

## Transformed SDL

```graphql
schema @fusion__connector(kind: "Apollo") {
  query: Query
}

type Query {
  productById(id: ID!): Product @internal @lookup
  productListByProductsAndId(
    key: ProductListByProductsAndIdInput! @is(field: "{ products: products[{ id }] }")
  ): ProductList @internal @lookup
  topProducts: ProductList!
}

type Product @key(fields: "id") {
  id: ID!
}

type ProductList {
  products: [Product!]! @shareable
}

input ProductListByProductsAndIdInput {
  products: [ProductListByProductsAndIdInput_Products!]!
}

input ProductListByProductsAndIdInput_Products {
  id: ID!
}

"The @fusion__connector directive declares which connector kind handles this source schema."
directive @fusion__connector(
  "The kind of connector that backs this source schema."
  kind: String!
) on SCHEMA
```
