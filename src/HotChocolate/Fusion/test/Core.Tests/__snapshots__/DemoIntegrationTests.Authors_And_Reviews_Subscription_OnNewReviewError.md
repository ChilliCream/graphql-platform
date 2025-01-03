# Authors_And_Reviews_Subscription_OnNewReviewError

## Result 1

```text
{
  "errors": [
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 2,
          "column": 5
        }
      ],
      "path": [
        "onNewReviewError"
      ],
      "extensions": {
        "code": "HC0018"
      }
    }
  ],
  "data": null
}
```

## Request

```graphql
subscription OnNewReview {
  onNewReviewError {
    body
    author {
      name
    }
  }
}
```

## QueryPlan Hash

```text
0A82A531BB6C8D6C44B690B4509A1667A89A3C2C
```

## QueryPlan

```json
{
  "document": "subscription OnNewReview { onNewReviewError { body author { name } } }",
  "operation": "OnNewReview",
  "rootNode": {
    "type": "Subscribe",
    "subgraph": "Reviews2",
    "document": "subscription OnNewReview_1 { onNewReviewError { body author { name } } }",
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

