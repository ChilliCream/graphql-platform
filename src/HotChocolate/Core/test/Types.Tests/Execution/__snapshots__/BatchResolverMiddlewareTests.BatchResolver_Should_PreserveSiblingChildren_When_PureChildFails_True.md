# BatchResolver_Should_PreserveSiblingChildren_When_PureChildFails

## Result

```json
{
  "errors": [
    {
      "message": "Child failed",
      "path": [
        "users",
        1,
        "profile",
        "check"
      ]
    }
  ],
  "data": {
    "users": [
      {
        "profile": {
          "displayName": "Alice",
          "check": "ok"
        }
      },
      null,
      {
        "profile": {
          "displayName": "Charlie",
          "check": "ok"
        }
      }
    ]
  }
}
```

## Executed children

```json
[
  "Alice",
  "Charlie"
]
```
