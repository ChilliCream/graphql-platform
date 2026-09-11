# OperationCompiler_Should_Reject_When_ExecutableDirectiveHasMiddlewareOnBatchSelection

```text
"errors": [
  {
    "message": "The directive `@mark` cannot be applied to the field `value` on type `Query` because the field is resolved by a batch resolver and directive middleware is not supported on batch selections.",
    "locations": [
      {
        "line": 1,
        "column": 16
      }
    ],
    "extensions": {
      "code": "HC0136",
      "coordinate": "Query.value"
    }
  }
]
```
