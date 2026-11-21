# Response_Should_RenderIndexedFieldErrors_When_DenyHandlingIsError

## Error: Unauthenticated nullable field (anonymous) -> 200 OK

```json
{
  "errors": [
    {
      "message": "The current user is not authenticated.",
      "path": [
        "me"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHENTICATED"
      }
    }
  ],
  "data": {
    "me": null
  }
}
```

## Error: Unauthorized nullable field (member) -> 200 OK

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
    "admin": null
  }
}
```

## Error: Anonymous principal on a scope-protected field (anonymous) -> 200 OK

```json
{
  "errors": [
    {
      "message": "The current user is not authenticated.",
      "path": [
        "admin"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHENTICATED"
      }
    }
  ],
  "data": {
    "admin": null
  }
}
```

## Error: Denied policy on nullable field (reader) -> 200 OK

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "guarded"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ],
  "data": {
    "guarded": null
  }
}
```

## Error: Allowed selections (reader) -> 200 OK

```json
{
  "data": {
    "product": {
      "id": "UHJvZHVjdDoxMDQzMzExNzcz",
      "name": "Product: UHJvZHVjdDoxMDQzMzExNzcz",
      "price": 123
    }
  }
}
```

## Error: Partial data beside a denied field (anonymous) -> 200 OK

```json
{
  "errors": [
    {
      "message": "The current user is not authenticated.",
      "path": [
        "me"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHENTICATED"
      }
    }
  ],
  "data": {
    "open": "Query",
    "me": null
  }
}
```

## Error: Denied non-null field under a nullable parent (reader) -> 200 OK

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

## Error: Denied non-null field under a non-null parent (reader) -> 200 OK

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "strictProduct",
        "cost"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ],
  "data": null
}
```

## Error: Denied nullable field in every list element (member) -> 200 OK

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "products",
        0,
        "price"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    },
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "products",
        1,
        "price"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    },
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "products",
        2,
        "price"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ],
  "data": {
    "products": [
      {
        "id": "UHJvZHVjdDoxOTc0MTQ1MDk=",
        "price": null
      },
      {
        "id": "UHJvZHVjdDozOTM1NzEyMDU5",
        "price": null
      },
      {
        "id": "UHJvZHVjdDoyMjE1MjQ2OTE2",
        "price": null
      }
    ]
  }
}
```

## Error: Denied non-null field in nullable list elements, one error per element (reader) -> 200 OK

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "products",
        0,
        "cost"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    },
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "products",
        1,
        "cost"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    },
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "products",
        2,
        "cost"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ],
  "data": {
    "products": [
      null,
      null,
      null
    ]
  }
}
```

## Error: Denied non-null field in non-null list elements, one error at the first violated index and the list is nulled (reader) -> 200 OK

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "strictProducts",
        0,
        "cost"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ],
  "data": {
    "strictProducts": null
  }
}
```
