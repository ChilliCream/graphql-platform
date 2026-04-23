# Transform_DeeplyNestedListKey

## Apollo Federation SDL

```graphql
schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key", "@shareable"]) {
  query: Query
}

type ProductList
  @key(fields: "products { id pid category { id tag } } selected { id }") {
  products: [Product!]!
  first: Product @shareable
  selected: Product @shareable
}

type Product @key(fields: "id pid category { id tag }") {
  id: String!
  pid: String
  category: Category
}

type Category @key(fields: "id tag") {
  id: String!
  tag: String
}

type Query {
  topProducts: ProductList!
  _service: _Service!
  _entities(representations: [_Any!]!): [_Entity]!
}

type _Service { sdl: String! }

union _Entity = ProductList | Product | Category

scalar FieldSet
scalar _Any

directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
directive @shareable on FIELD_DEFINITION | OBJECT
directive @link(url: String! import: [String!]) repeatable on SCHEMA
```

## Transformed SDL

```graphql
schema {
  query: Query
}

type Query {
  categoryByIdAndTag(id: String!, tag: String!): Category @internal @lookup
  productByIdAndPidAndCategoryAndIdAndTag(
    key: ProductByIdAndPidAndCategoryAndIdAndTagInput! @is(field: "{ id, pid, category: category.{ id, tag } }")
  ): Product @internal @lookup
  productListByProductsAndIdAndPidAndCategoryAndIdAndTagAndSelectedAndId(
    key: ProductListByProductsAndIdAndPidAndCategoryAndIdAndTagAndSelectedAndIdInput! @is(field: "{ products: products[{ id, pid, category: category.{ id, tag } }], selected: selected.{ id } }")
  ): ProductList @internal @lookup
  topProducts: ProductList!
}

type Category @key(fields: "id tag") {
  id: String!
  tag: String
}

type Product @key(fields: "id pid category { id tag }") {
  category: Category
  id: String!
  pid: String
}

type ProductList {
  first: Product @shareable
  products: [Product!]! @shareable
  selected: Product @shareable
}

input ProductByIdAndPidAndCategoryAndIdAndTagInput {
  category: ProductByIdAndPidAndCategoryAndIdAndTagInput_Category
  id: String!
  pid: String
}

input ProductByIdAndPidAndCategoryAndIdAndTagInput_Category {
  id: String!
  tag: String
}

input ProductListByProductsAndIdAndPidAndCategoryAndIdAndTagAndSelectedAndIdInput {
  products: [ProductListByProductsAndIdAndPidAndCategoryAndIdAndTagAndSelectedAndIdInput_Products!]!
  selected: ProductListByProductsAndIdAndPidAndCategoryAndIdAndTagAndSelectedAndIdInput_Selected
}

input ProductListByProductsAndIdAndPidAndCategoryAndIdAndTagAndSelectedAndIdInput_Products {
  category: ProductListByProductsAndIdAndPidAndCategoryAndIdAndTagAndSelectedAndIdInput_Products_Category
  id: String!
  pid: String
}

input ProductListByProductsAndIdAndPidAndCategoryAndIdAndTagAndSelectedAndIdInput_Products_Category {
  id: String!
  tag: String
}

input ProductListByProductsAndIdAndPidAndCategoryAndIdAndTagAndSelectedAndIdInput_Selected {
  id: String!
}
```
