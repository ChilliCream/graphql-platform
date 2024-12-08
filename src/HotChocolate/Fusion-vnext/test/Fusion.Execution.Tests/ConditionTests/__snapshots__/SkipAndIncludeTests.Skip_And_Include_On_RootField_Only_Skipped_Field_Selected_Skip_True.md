# Skip_And_Include_On_RootField_Only_Skipped_Field_Selected_Skip_True

## Request

```graphql
query GetProduct($id: ID!, $include: Boolean!) {
  productById(id: $id) @include(if: $include) @skip(if: true) {
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

