# Skip_And_Include_On_RootField_Only_Skipped_Field_Selected_Skip_True_Include_False

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) @skip(if: true) @include(if: false) {
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

