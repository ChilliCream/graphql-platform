# Authorize_Should_ReportPolicyNameAndPath_When_PolicyIsMissing

## Result

```json
{
  "errors": [
    {
      "message": "The `MISSING` authorization policy does not exist.",
      "path": [
        "secret"
      ],
      "extensions": {
        "code": "AUTH_POLICY_NOT_FOUND"
      }
    }
  ],
  "data": {
    "secret": null
  }
}
```

## Authorization calls

```json
1
```

## Resolver calls

```json
0
```
