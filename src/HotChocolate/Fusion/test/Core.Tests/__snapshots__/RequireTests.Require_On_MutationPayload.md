# Require_On_MutationPayload

## UserRequest

```graphql
mutation {
  createUser {
    user {
      nestedField {
        otherField
      }
    }
  }
}
```

## QueryPlan

```json
{
  "document": "mutation { createUser { user { nestedField { otherField } } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "subgraphB",
        "document": "mutation fetch_createUser_1 { createUser { user { nestedField { otherField } } } }",
        "selectionSetId": 0
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          0
        ]
      }
    ]
  }
}
```

