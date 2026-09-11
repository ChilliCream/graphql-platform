# ResolveNodeBatch_Should_Enforce_Type_Policy_When_Registered

## Result

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "nodes",
        0
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    },
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "nodes",
        1
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ],
  "data": {
    "nodes": [
      null,
      null
    ]
  }
}
```

## Authorization

```json
{
  "InvocationCount": 0,
  "Policies": [
    "read-node",
    "read-node"
  ]
}
```
