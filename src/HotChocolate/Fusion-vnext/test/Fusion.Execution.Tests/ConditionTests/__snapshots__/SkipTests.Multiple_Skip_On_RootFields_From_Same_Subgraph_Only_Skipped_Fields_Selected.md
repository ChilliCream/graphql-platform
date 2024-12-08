# Multiple_Skip_On_RootFields_From_Same_Subgraph_Only_Skipped_Fields_Selected

## Request

```graphql
query GetProduct($id: ID!, $skip1: Boolean!, $skip2: Boolean!) {
  productById(id: $id) @skip(if: $skip1) {
    name
  }
  products @skip(if: $skip2) {
    nodes {
      name
    }
  }
}
```

## Plan

```json
{
  "kind": "Root",
  "nodes": [
    {
      "kind": "Operation",
      "schema": "PRODUCTS",
      "document": "query($id: ID!, $skip1: Boolean!, $skip2: Boolean!) { productById(id: $id) @skip(if: $skip1) { name } products @skip(if: $skip2) { nodes { name } } }"
    }
  ]
}
```

