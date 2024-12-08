# Skip_On_RootFragment_Only_Skipped_Fragment_Selected_If_True

## Request

```graphql
query GetProduct($slug: String!) {
  ... QueryFragment @skip(if: true)
}

fragment QueryFragment on Query {
  productBySlug(slug: $slug) {
    name
  }
}
```

## Plan

```json
{
  "kind": "Root"
}
```

