# Query_Plan_17_Multi_Completion

## UserRequest

```graphql
query GetUser {
  users {
    birthdate
  }
  reviews {
    body
  }
  __schema {
    types {
      name
    }
  }
}
```

## QueryPlan

```json
{
  "document": "query GetUser { users { birthdate } reviews { body } __schema { types { name } } }",
  "operation": "GetUser",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "Introspect",
            "document": "{ __schema { types { name } } }"
          },
          {
            "type": "Resolve",
            "subgraph": "Accounts",
            "document": "query GetUser_1 { users { birthdate } }",
            "selectionSetId": 0
          },
          {
            "type": "Resolve",
            "subgraph": "Reviews",
            "document": "query GetUser_2 { reviews { body } }",
            "selectionSetId": 0
          }
        ]
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

