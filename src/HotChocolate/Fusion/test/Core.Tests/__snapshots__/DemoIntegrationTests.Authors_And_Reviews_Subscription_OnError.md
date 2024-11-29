# Authors_And_Reviews_Subscription_OnError

## Result 1

```text
{
  "data": {
    "onError": {
      "body": "Love it!",
      "author": {
        "name": "@ada"
      }
    }
  }
}
```

## Result 2

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
        "onError"
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
subscription OnError {
  onError {
    body
    author {
      name
    }
  }
}
```

## QueryPlan Hash

```text
916BEE3A8BA7909469686D90E617234974CC01F3
```

## QueryPlan

```json
{
  "document": "subscription OnError { onError { body author { name } } }",
  "operation": "OnError",
  "rootNode": {
    "type": "Subscribe",
    "subgraph": "Reviews",
    "document": "subscription OnError_1 { onError { body author { name } } }",
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

