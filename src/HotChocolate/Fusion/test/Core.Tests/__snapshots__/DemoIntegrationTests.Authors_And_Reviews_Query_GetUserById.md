# Authors_And_Reviews_Query_GetUserById

## Result

```json
{
  "data": {
    "userById": {
      "id": "VXNlcjox"
    }
  }
}
```

## Request

```graphql
query GetUser {
  userById(id: "VXNlcjox") {
    id
  }
}
```

## QueryPlan Hash

```text
0F600106AE3E1472843632868459BD7535AA7659
```

## QueryPlan

```json
{
  "document": "query GetUser { userById(id: \u0022VXNlcjox\u0022) { id } }",
  "operation": "GetUser",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Accounts",
        "document": "query GetUser_1 { userById(id: \u0022VXNlcjox\u0022) { id } }",
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

