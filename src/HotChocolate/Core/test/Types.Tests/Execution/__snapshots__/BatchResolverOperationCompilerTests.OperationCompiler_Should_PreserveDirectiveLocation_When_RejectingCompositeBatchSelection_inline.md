# OperationCompiler_Should_PreserveDirectiveLocation_When_RejectingCompositeBatchSelection

```text
"errors": [
  {
    "message": "The directive `@mark` cannot be applied to the field `item` on type `Query` because the field is resolved by a batch resolver and directive middleware is not supported on batch selections.",
    "locations": [
      {
        "line": 1,
        "column": 23
      }
    ],
    "extensions": {
      "code": "HC0136",
      "coordinate": "Query.item"
    }
  }
]
```
