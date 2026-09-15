# ResolveNodeBatchWith_Should_Deny_Method_Policy_When_Not_Allowed

## Result

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "node"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ],
  "data": {
    "node": null
  }
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
