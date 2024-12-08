# Skipped_Root_Selections_From_Same_Subgraph_Same_Variable

## Request

```graphql
query($slug: String!, $skip: Boolean!) {
  productBySlug(slug: $slug) @skip(if: $skip) {
    name
  }
  products @skip(if: $skip) {
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
      "kind": "Condition",
      "variableName": "skip",
      "passingValue": false,
      "nodes": [
        {
          "kind": "Operation",
          "schema": "PRODUCTS",
          "document": "query($slug: String!) { productBySlug(slug: $slug) { name } products { nodes { name } } }"
        }
      ]
    }
  ]
}
```

