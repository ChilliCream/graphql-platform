# Authorize_Should_EnforcePolicy_When_BatchFieldExecutes

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

## Authorization calls

```json
[
  "Validation"
]
```

## Resolver batches

```json
[]
```
