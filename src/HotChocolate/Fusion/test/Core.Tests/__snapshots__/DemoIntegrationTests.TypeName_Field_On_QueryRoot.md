# TypeName_Field_On_QueryRoot

## Result

```json
{
  "data": {
    "__typename": "Query"
  }
}
```

## Request

```graphql
query Introspect {
  __typename
}
```

## QueryPlan Hash

```text
697698C162C0AB8A3054708CABD070BFABE80B8B
```

## QueryPlan

```json
{
  "document": "query Introspect { __typename }",
  "operation": "Introspect",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Introspect",
        "document": "{ __typename }"
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

