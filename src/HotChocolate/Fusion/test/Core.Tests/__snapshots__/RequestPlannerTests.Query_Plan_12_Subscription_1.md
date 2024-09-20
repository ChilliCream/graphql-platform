# Query_Plan_12_Subscription_1

## UserRequest

```graphql
subscription OnNewReview {
  onNewReview {
    body
    author {
      name
    }
  }
}
```

## QueryPlan

```json
{
  "document": "subscription OnNewReview { onNewReview { body author { name } } }",
  "operation": "OnNewReview",
  "rootNode": {
    "type": "Subscribe",
    "subgraph": "Reviews",
    "document": "subscription OnNewReview_1 { onNewReview { body author { name } } }",
    "selectionSetId": 0,
    "nodes": [
      {
        "type": "Sequence",
        "nodes": [
          {
            "type": "Compose",
            "selectionSetIds": [
              0
            ]
          }
        ]
      }
    ]
  }
}
```

