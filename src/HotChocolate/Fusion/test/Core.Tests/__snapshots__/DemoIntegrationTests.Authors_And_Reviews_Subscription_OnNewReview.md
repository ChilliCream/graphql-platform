# Authors_And_Reviews_Subscription_OnNewReview

## Result 1

```text
{
  "data": {
    "onNewReview": {
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
  "data": {
    "onNewReview": {
      "body": "Too expensive.",
      "author": {
        "name": "@complete"
      }
    }
  }
}
```

## Result 3

```text
{
  "data": {
    "onNewReview": {
      "body": "Could be better.",
      "author": {
        "name": "@ada"
      }
    }
  }
}
```

## Result 4

```text
{
  "data": {
    "onNewReview": {
      "body": "Prefer something else.",
      "author": {
        "name": "@complete"
      }
    }
  }
}
```

## Request

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

## QueryPlan Hash

```text
F823835BFACD5B49C03063C112F1293EAB40DE1E
```

## QueryPlan

```json
{
  "document": "subscription OnNewReview { onNewReview { body author { name } } }",
  "operation": "OnNewReview",
  "rootNode": {
    "type": "Subscribe",
    "subgraph": "Reviews2",
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

