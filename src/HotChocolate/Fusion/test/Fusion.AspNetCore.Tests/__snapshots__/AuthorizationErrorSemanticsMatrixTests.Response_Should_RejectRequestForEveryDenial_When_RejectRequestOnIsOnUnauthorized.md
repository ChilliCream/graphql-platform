# Response_Should_RejectRequestForEveryDenial_When_RejectRequestOnIsOnUnauthorized

## OnUnauthorized: Unauthenticated denial (anonymous) -> 401 Unauthorized, WWW-Authenticate: Bearer

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

## OnUnauthorized: Unauthorized denial (member) -> 403 Forbidden

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

## OnUnauthorized: Denied non-null field (reader) -> 403 Forbidden

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

## OnUnauthorized: Nothing denied (reader) -> 200 OK

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

## OnUnauthorized: Unprotected operation (anonymous) -> 200 OK

```json
{
  "data": {
    "open": "Query"
  }
}
```

## OnUnauthorized with attribution: Unauthenticated denial (anonymous) -> 401 Unauthorized, WWW-Authenticate: Bearer

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

## OnUnauthorized with attribution: Unauthorized denial (member) -> 403 Forbidden

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED",
        "coordinate": "Query.admin",
        "directive": "requiresScopes",
        "requiredScopes": [
          [
            "admin"
          ]
        ]
      }
    }
  ]
}
```

## OnUnauthorized with attribution: Denied non-null field (reader) -> 403 Forbidden

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED",
        "coordinate": "Product.cost",
        "directive": "policy",
        "policy": "finance"
      }
    }
  ]
}
```

## OnUnauthorized with attribution: Nothing denied (reader) -> 200 OK

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

## OnUnauthorized with attribution: Unprotected operation (anonymous) -> 200 OK

```json
{
  "data": {
    "open": "Query"
  }
}
```

## OnUnauthorized with null deny handling: Unauthenticated denial (anonymous) -> 401 Unauthorized, WWW-Authenticate: Bearer

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

## OnUnauthorized with null deny handling: Unauthorized denial (member) -> 403 Forbidden

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

## OnUnauthorized with null deny handling: Denied non-null field (reader) -> 403 Forbidden

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

## OnUnauthorized with null deny handling: Nothing denied (reader) -> 200 OK

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

## OnUnauthorized with null deny handling: Unprotected operation (anonymous) -> 200 OK

```json
{
  "data": {
    "open": "Query"
  }
}
```
