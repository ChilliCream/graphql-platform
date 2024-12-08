# Skip_On_RootFragment_Only_Skipped_Fragment_Selected_If_True

## Request

```graphql
query GetProduct($id: ID!) {
  ... QueryFragment @skip(if: true)
}

fragment QueryFragment on Query {
  productById(id: $id) {
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

