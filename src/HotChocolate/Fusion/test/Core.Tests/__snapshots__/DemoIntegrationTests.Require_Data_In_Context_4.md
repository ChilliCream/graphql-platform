# Require_Data_In_Context_4

## User Request

```graphql
query Requires {
  productById(id: "UHJvZHVjdDox") {
    deliveryEstimate(zip: "12345") {
      min
      max
    }
  }
}
```

## Result

```json
{
  "errors": [
    {
      "message": "The argument `weight` is required.",
      "locations": [
        {
          "line": 1,
          "column": 54
        }
      ],
      "path": [
        "productById"
      ],
      "extensions": {
        "type": "Product",
        "field": "deliveryEstimate",
        "argument": "weight",
        "specifiedBy": "https://spec.graphql.org/October2021/#sec-Required-Arguments"
      }
    },
    {
      "message": "The argument `size` is required.",
      "locations": [
        {
          "line": 1,
          "column": 54
        }
      ],
      "path": [
        "productById"
      ],
      "extensions": {
        "type": "Product",
        "field": "deliveryEstimate",
        "argument": "size",
        "specifiedBy": "https://spec.graphql.org/October2021/#sec-Required-Arguments"
      }
    }
  ],
  "data": {
    "productById": null
  }
}
```

## QueryPlan

```json
{
  "document": "query Requires { productById(id: \u0022UHJvZHVjdDox\u0022) { deliveryEstimate(zip: \u002212345\u0022) { min max } } }",
  "operation": "Requires",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Shipping",
        "document": "query Requires_1 { productById(id: \u0022UHJvZHVjdDox\u0022) { deliveryEstimate(zip: \u002212345\u0022) { min max } } }",
        "selectionSetId": 0
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          0
        ]
      }
    ]
  }
}
```

## QueryPlan Hash

```text
9234666A9AAB9875F9172451BA52D23F6FB9D270
```

## Fusion Graph

```graphql
schema
  @fusion(version: 1)
  @transport(subgraph: "Products", location: "http:\/\/localhost:5000\/graphql", kind: "HTTP")
  @transport(subgraph: "Products", location: "ws:\/\/localhost:5000\/graphql", kind: "WebSocket")
  @transport(subgraph: "Shipping", location: "http:\/\/localhost:5000\/graphql", kind: "HTTP")
  @transport(subgraph: "Shipping", location: "ws:\/\/localhost:5000\/graphql", kind: "WebSocket")
  @node(subgraph: "Products", types: [ "Product" ]) {
  query: Query
  mutation: Mutation
}

type Query {
  "Fetches an object given its ID."
  node("ID of the object." id: ID!): Node
    @variable(subgraph: "Products", name: "id", argument: "id")
    @resolver(subgraph: "Products", select: "{ node(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
  "Lookup nodes by a list of IDs."
  nodes("The list of node IDs." ids: [ID!]!): [Node]!
    @variable(subgraph: "Products", name: "ids", argument: "ids")
    @resolver(subgraph: "Products", select: "{ nodes(ids: $ids) }", arguments: [ { name: "ids", type: "[ID!]!" } ])
  productById(id: ID!): Product
    @variable(subgraph: "Products", name: "id", argument: "id")
    @resolver(subgraph: "Products", select: "{ productById(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
    @variable(subgraph: "Shipping", name: "id", argument: "id")
    @resolver(subgraph: "Shipping", select: "{ productById(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
  topProducts(first: Int!): [Product!]!
    @variable(subgraph: "Products", name: "first", argument: "first")
    @resolver(subgraph: "Products", select: "{ topProducts(first: $first) }", arguments: [ { name: "first", type: "Int!" } ])
}

type Mutation {
  uploadMultipleProductPictures(input: UploadMultipleProductPicturesInput!): UploadMultipleProductPicturesPayload!
    @variable(subgraph: "Products", name: "input", argument: "input")
    @resolver(subgraph: "Products", select: "{ uploadMultipleProductPictures(input: $input) }", arguments: [ { name: "input", type: "UploadMultipleProductPicturesInput!" } ])
  uploadProductPicture(input: UploadProductPictureInput!): UploadProductPicturePayload!
    @variable(subgraph: "Products", name: "input", argument: "input")
    @resolver(subgraph: "Products", select: "{ uploadProductPicture(input: $input) }", arguments: [ { name: "input", type: "UploadProductPictureInput!" } ])
}

type DeliveryEstimate {
  max: Int!
    @source(subgraph: "Shipping")
  min: Int!
    @source(subgraph: "Shipping")
}

type Product implements Node
  @variable(subgraph: "Products", name: "Product_id", select: "id")
  @variable(subgraph: "Shipping", name: "Product_id", select: "id")
  @resolver(subgraph: "Products", select: "{ productById(id: $Product_id) }", arguments: [ { name: "Product_id", type: "ID!" } ])
  @resolver(subgraph: "Shipping", select: "{ productById(id: $Product_id) }", arguments: [ { name: "Product_id", type: "ID!" } ])
  @resolver(subgraph: "Products", select: "{ nodes(ids: $Product_id) { ... on Product { ... Product } } }", arguments: [ { name: "Product_id", type: "[ID!]!" } ], kind: "BATCH") {
  deliveryEstimate(zip: String!): DeliveryEstimate!
    @source(subgraph: "Shipping")
    @variable(subgraph: "Shipping", name: "zip", argument: "zip")
    @variable(subgraph: "Products", name: "Product_dimension_size", select: "dimension { size }")
    @variable(subgraph: "Products", name: "Product_dimension_weight", select: "dimension { weight }")
    @resolver(subgraph: "Shipping", select: "{ deliveryEstimate(size: $Product_dimension_size, weight: $Product_dimension_weight, zip: $zip) }", arguments: [ { name: "Product_dimension_size", type: "Int!" }, { name: "Product_dimension_weight", type: "Int!" }, { name: "zip", type: "String!" } ])
  dimension: ProductDimension!
    @source(subgraph: "Products")
  id: ID!
    @source(subgraph: "Products")
    @source(subgraph: "Shipping")
  name: String!
    @source(subgraph: "Products")
  price: Int!
    @source(subgraph: "Products")
  repeat(num: Int!): Int!
    @source(subgraph: "Products")
    @variable(subgraph: "Products", name: "num", argument: "num")
  repeatData(data: SomeDataInput!): SomeData!
    @source(subgraph: "Products")
    @variable(subgraph: "Products", name: "data", argument: "data")
  weight: Int!
    @source(subgraph: "Products")
}

type ProductDimension {
  size: Int!
    @source(subgraph: "Products")
  weight: Int!
    @source(subgraph: "Products")
}

type ProductNotFoundError implements Error {
  message: String!
    @source(subgraph: "Products")
  productId: Int!
    @source(subgraph: "Products")
}

type SomeData {
  data: SomeData
    @source(subgraph: "Products")
  num: Int
    @source(subgraph: "Products")
}

type UploadMultipleProductPicturesPayload {
  boolean: Boolean
    @source(subgraph: "Products")
  errors: [UploadMultipleProductPicturesError!]
    @source(subgraph: "Products")
}

type UploadProductPicturePayload {
  boolean: Boolean
    @source(subgraph: "Products")
  errors: [UploadProductPictureError!]
    @source(subgraph: "Products")
}

interface Error {
  message: String!
}

"The node interface is implemented by entities that have a global unique identifier."
interface Node {
  id: ID!
}

union UploadMultipleProductPicturesError = ProductNotFoundError

union UploadProductPictureError = ProductNotFoundError

input ProductIdWithUploadInput {
  file: Upload!
  productId: Int!
}

input SomeDataInput {
  data: SomeDataInput
  num: Int
}

input UploadMultipleProductPicturesInput {
  products: [ProductIdWithUploadInput!]!
}

input UploadProductPictureInput {
  file: Upload!
  productId: Int!
}

"The `Upload` scalar type represents a file upload."
scalar Upload
```

