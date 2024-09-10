# Authors_And_Reviews_Query_GetUserById_With_Invalid_Id_Value

## Result

```json
{
  "errors": [
    {
      "message": "The node ID string has an invalid format.",
      "extensions": {
        "originalValue": "1"
      }
    }
  ],
  "data": {
    "userById": null
  }
}
```

## Request

```graphql
query GetUser {
  userById(id: 1) {
    id
  }
}
```

## QueryPlan Hash

```text
7D1C123A297876C8F5C32BC231900D1706858A87
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

