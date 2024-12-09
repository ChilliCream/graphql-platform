# Skipped_Root_Fragments_With_Same_Root_Selection_From_Same_Subgraph_Same_Variable

## Request

```graphql
query($slug: String!, $skip: Boolean!) {
  ... QueryFragment1 @skip(if: $skip)
  ... QueryFragment2 @skip(if: $skip)
}

fragment QueryFragment1 on Query {
  productBySlug(slug: $slug) {
    name
  }
}

fragment QueryFragment2 on Query {
  productBySlug(slug: $slug) {
    name
    description
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
          "document": "query($slug: String!) { productBySlug(slug: $slug) { name } productBySlug(slug: $slug) { name description } }"
        }
      ]
    }
  ]
}
```

