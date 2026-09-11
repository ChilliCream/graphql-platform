# Authorize_Should_UseOwningUser_When_RequestsExecuteConcurrently

## Allowed request

```json
{
  "data": {
    "secret": "secret"
  }
}
```

## Denied request

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "secret"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHENTICATED"
      }
    }
  ],
  "data": {
    "secret": null
  }
}
```

## Authorization users

```json
[
  "allowed",
  "denied"
]
```

## Resolver users

```json
[
  [
    "allowed"
  ]
]
```
