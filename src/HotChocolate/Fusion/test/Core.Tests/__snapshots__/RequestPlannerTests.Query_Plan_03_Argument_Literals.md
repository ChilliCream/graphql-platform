# Query_Plan_03_Argument_Literals

## UserRequest

```graphql
query GetUser {
  userById(id: 1) {
    id
  }
}
```

## QueryPlan

```json
{
  "document": "query GetUser { userById(id: 1) { id } }",
  "operation": "GetUser",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Accounts",
        "document": "query GetUser_1 { userById(id: 1) { id } }",
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

