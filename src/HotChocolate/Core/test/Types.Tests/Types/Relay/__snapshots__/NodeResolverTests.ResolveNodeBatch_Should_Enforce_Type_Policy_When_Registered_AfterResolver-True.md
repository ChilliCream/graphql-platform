# ResolveNodeBatch_Should_Enforce_Type_Policy_When_Registered

## Result

```json
{
  "data": {
    "nodes": [
      {
        "name": "x"
      },
      {
        "name": "y"
      }
    ]
  }
}
```

## Authorization

```json
{
  "InvocationCount": 1,
  "Policies": [
    "read-node",
    "read-node"
  ]
}
```
