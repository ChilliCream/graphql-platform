# BatchResolver_Should_PreserveSiblingChildren_When_PureChildFails

## Result

```json
{
  "errors": [
    {
      "message": "Cannot return null for non-nullable field.",
      "path": [
        "users",
        1,
        "profile",
        "check"
      ],
      "extensions": {
        "code": "HC0018"
      }
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
