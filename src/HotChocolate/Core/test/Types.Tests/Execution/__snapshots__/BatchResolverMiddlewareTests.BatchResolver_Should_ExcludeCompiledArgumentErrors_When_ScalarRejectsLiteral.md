# BatchResolver_Should_ExcludeCompiledArgumentErrors_When_ScalarRejectsLiteral

## Result

```json
{
  "errors": [
    {
      "message": "RejectingString cannot coerce the given literal of type `StringValue` to a runtime value.",
      "path": [
        "users",
        0,
        "greeting"
      ],
      "extensions": {
        "inputPath": [
          "text"
        ],
        "coordinate": "BatchUser.greeting(text:)",
        "fieldType": "RejectingString"
      }
    },
    {
      "message": "RejectingString cannot coerce the given literal of type `StringValue` to a runtime value.",
      "path": [
        "users",
        1,
        "greeting"
      ],
      "extensions": {
        "inputPath": [
          "text"
        ],
        "coordinate": "BatchUser.greeting(text:)",
        "fieldType": "RejectingString"
      }
    }
  ],
  "data": {
    "users": [
      {
        "name": "Alice",
        "greeting": null
      },
      {
        "name": "Bob",
        "greeting": null
      }
    ]
  }
}
```

## Delegate calls

```json
0
```
