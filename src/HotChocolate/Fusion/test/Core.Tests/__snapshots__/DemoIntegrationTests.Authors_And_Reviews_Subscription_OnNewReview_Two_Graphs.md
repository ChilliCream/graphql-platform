# Authors_And_Reviews_Subscription_OnNewReview_Two_Graphs

## Result 1

```text
{
  "data": {
    "onNewReview": {
      "body": "irthdate",
      "author": {
        "name": "__fu",
        "birthdate": "1815-12-10"
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
        "name": "@complete",
        "birthdate": "1912-06-23"
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
      "body": "irthdate } }\",\"v",
      "author": {
        "name": "_exp",
        "birthdate": "1815-12-10"
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
        "name": "@complete",
        "birthdate": "1912-06-23"
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
      birthdate
    }
  }
}
```

## QueryPlan Hash

```text
F3AC58D6264B6843254A09063FFB2EFB28987ECA
```

## QueryPlan

```json
{
  "document": "subscription OnNewReview { onNewReview { body author { name birthdate } } }",
  "operation": "OnNewReview",
  "rootNode": {
    "type": "Subscribe",
    "subgraph": "Reviews2",
    "document": "subscription OnNewReview_1 { onNewReview { body author { name __fusion_exports__1: id } } }",
    "selectionSetId": 0,
    "provides": [
      {
        "variable": "__fusion_exports__1"
      }
    ],
    "nodes": [
      {
        "type": "Sequence",
        "nodes": [
          {
            "type": "Compose",
            "selectionSetIds": [
              0
            ]
          },
          {
            "type": "Resolve",
            "subgraph": "Accounts",
            "document": "query OnNewReview_2($__fusion_exports__1: ID!) { userById(id: $__fusion_exports__1) { birthdate } }",
            "selectionSetId": 2,
            "path": [
              "userById"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__1"
              }
            ]
          },
          {
            "type": "Compose",
            "selectionSetIds": [
              2
            ]
          }
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "User_id"
  }
}
```

