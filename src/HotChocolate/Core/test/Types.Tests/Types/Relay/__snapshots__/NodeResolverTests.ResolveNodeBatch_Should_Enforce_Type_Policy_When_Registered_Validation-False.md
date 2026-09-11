# ResolveNodeBatch_Should_Enforce_Type_Policy_When_Registered

## Result

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ]
}
```

## Authorization

```json
{
  "InvocationCount": 0,
  "Policies": [
    "read-node"
  ]
}
```
