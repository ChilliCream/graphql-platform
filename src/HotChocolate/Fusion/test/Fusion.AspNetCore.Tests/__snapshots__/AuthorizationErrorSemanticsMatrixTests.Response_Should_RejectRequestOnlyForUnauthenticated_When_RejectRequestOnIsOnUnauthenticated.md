# Response_Should_RejectRequestOnlyForUnauthenticated_When_RejectRequestOnIsOnUnauthenticated

## OnUnauthenticated: Unauthenticated denial (anonymous) -> 401 Unauthorized, WWW-Authenticate: Bearer

```json
{
  "errors": [
    {
      "message": "The current user is not authenticated.",
      "extensions": {
        "code": "AUTH_NOT_AUTHENTICATED"
      }
    }
  ]
}
```

## OnUnauthenticated: Unauthorized denial (member) -> 200 OK

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "admin"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ],
  "data": {
    "open": "Query",
    "admin": null
  }
}
```

## OnUnauthenticated: Denied non-null field (reader) -> 200 OK

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "product",
        "cost"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ],
  "data": {
    "product": null
  }
}
```

## OnUnauthenticated: Nothing denied (reader) -> 200 OK

```json
{
  "data": {
    "open": "Query",
    "product": {
      "id": "UHJvZHVjdDox",
      "price": 123
    }
  }
}
```

## OnUnauthenticated: Unprotected operation (anonymous) -> 200 OK

```json
{
  "data": {
    "open": "Query"
  }
}
```

## OnUnauthenticated with attribution: Unauthenticated denial (anonymous) -> 401 Unauthorized, WWW-Authenticate: Bearer

```json
{
  "errors": [
    {
      "message": "The current user is not authenticated.",
      "extensions": {
        "code": "AUTH_NOT_AUTHENTICATED",
        "coordinate": "Query.me",
        "directive": "authenticated"
      }
    }
  ]
}
```

## OnUnauthenticated with attribution: Unauthorized denial (member) -> 200 OK

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "admin"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED",
        "directive": "requiresScopes",
        "requiredScopes": [
          [
            "admin"
          ]
        ]
      }
    }
  ],
  "data": {
    "open": "Query",
    "admin": null
  }
}
```

## OnUnauthenticated with attribution: Denied non-null field (reader) -> 200 OK

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "product",
        "cost"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED",
        "directive": "policy",
        "policy": "finance"
      }
    }
  ],
  "data": {
    "product": null
  }
}
```

## OnUnauthenticated with attribution: Nothing denied (reader) -> 200 OK

```json
{
  "data": {
    "open": "Query",
    "product": {
      "id": "UHJvZHVjdDox",
      "price": 123
    }
  }
}
```

## OnUnauthenticated with attribution: Unprotected operation (anonymous) -> 200 OK

```json
{
  "data": {
    "open": "Query"
  }
}
```
