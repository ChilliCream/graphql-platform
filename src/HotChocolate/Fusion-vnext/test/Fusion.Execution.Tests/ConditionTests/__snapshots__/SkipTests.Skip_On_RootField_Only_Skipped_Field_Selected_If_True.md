# Skip_On_RootField_Only_Skipped_Field_Selected_If_True

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) @skip(if: true) {
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

