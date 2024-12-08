# Skip_And_Include_On_SubField_Skip_False

## Request

```graphql
query GetProduct($slug: String!, $include: Boolean!) {
  productBySlug(slug: $slug) {
    name @include(if: $include) @skip(if: false)
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
      "kind": "Operation",
      "schema": "PRODUCTS",
      "document": "query($include: Boolean!, $slug: String!) { productBySlug(slug: $slug) { name @include(if: $include) description } }"
    }
  ]
}
```

