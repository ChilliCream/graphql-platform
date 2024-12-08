# Skipped_Sub_Selection_That_Provides_Data_For_Lookup_On_Different_Subgraph

## Request

```graphql
query($id: ID!, $skip: Boolean!) {
  reviewById(id: $id) {
    body
    author @skip(if: $skip) {
      displayName
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
      "schema": "REVIEWS",
      "document": "query($id: ID!, $skip: Boolean!) { reviewById(id: $id) { body author @skip(if: $skip) } }",
      "nodes": [
        {
          "kind": "Operation",
          "schema": "ACCOUNTS",
          "document": "{ userById { displayName } }"
        }
      ]
    }
  ]
}
```

