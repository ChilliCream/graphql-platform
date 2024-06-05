# Fetch_Data_Across_Subgraphs

## Result

```json
{
  "data": {
    "viewer": {
      "data": {
        "accountValue": "Account",
        "reviewsValue": "Reviews2"
      }
    }
  }
}
```

## Request

```graphql
query GetUser {
  viewer {
    data {
      accountValue
      reviewsValue
    }
  }
}
```

## QueryPlan Hash

```text
0945927521FCEB9066754DDECB9BC7D43B187B4E
```

## QueryPlan

```json
{
  "document": "query GetUser { viewer { data { accountValue reviewsValue } } }",
  "operation": "GetUser",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "Resolve",
            "subgraph": "Accounts",
            "document": "query GetUser_1 { viewer { data { accountValue } } }",
            "selectionSetId": 0
          },
          {
            "type": "Resolve",
            "subgraph": "Reviews2",
            "document": "query GetUser_2 { viewer { data { reviewsValue } } }",
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

