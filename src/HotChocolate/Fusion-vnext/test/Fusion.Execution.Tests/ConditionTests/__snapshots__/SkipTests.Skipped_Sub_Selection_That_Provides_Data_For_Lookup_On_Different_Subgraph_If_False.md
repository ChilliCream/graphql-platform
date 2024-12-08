# Skipped_Sub_Selection_That_Provides_Data_For_Lookup_On_Different_Subgraph_If_False

## Request

```graphql
query($id: ID!) {
  reviewById(id: $id) {
    body
    author @skip(if: false) {
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
      "document": "query($id: ID!) { reviewById(id: $id) { body author } }",
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

