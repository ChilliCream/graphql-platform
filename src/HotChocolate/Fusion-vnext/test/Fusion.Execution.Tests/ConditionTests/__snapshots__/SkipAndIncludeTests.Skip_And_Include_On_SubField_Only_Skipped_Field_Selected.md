# Skip_And_Include_On_SubField_Only_Skipped_Field_Selected

## Request

```graphql
query GetProduct($slug: String!, $skip: Boolean!, $include: Boolean!) {
  productBySlug(slug: $slug) {
    name @skip(if: $skip) @include(if: $include)
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
      "document": "query($include: Boolean!, $skip: Boolean!, $slug: String!) { productBySlug(slug: $slug) { name @skip(if: $skip) @include(if: $include) } }"
    }
  ]
}
```

