# InlineFragment_Should_BeRejected_When_IfIsNull

```json
{
  "errors": [
    {
      "message": "The specified argument value does not match the argument type.",
      "locations": [
        {
          "line": 2,
          "column": 20
        }
      ],
      "extensions": {
        "argument": "if",
        "argumentValue": "null",
        "locationType": "Boolean!",
        "specifiedBy": "https://spec.graphql.org/September2025/#sec-Values-of-Correct-Type"
      }
    }
  ]
}
```
