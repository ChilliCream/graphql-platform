# Response_Should_RenderAttributionOnlyOnAuthorizationErrors_When_AttributionIsOn

## Error with attribution: Unauthenticated nullable field (anonymous) -> 200 OK

```json
{
  "errors": [
    {
      "message": "The current user is not authenticated.",
      "path": [
        "me"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHENTICATED",
        "directive": "authenticated"
      }
    }
  ],
  "data": {
    "me": null
  }
}
```

## Error with attribution: Unauthorized nullable field (member) -> 200 OK

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
    "admin": null
  }
}
```

## Error with attribution: Anonymous principal on a scope-protected field (anonymous) -> 200 OK

```json
{
  "errors": [
    {
      "message": "The current user is not authenticated.",
      "path": [
        "admin"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHENTICATED",
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
    "admin": null
  }
}
```

## Error with attribution: Denied policy on nullable field (reader) -> 200 OK

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "guarded"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED",
        "directive": "policy",
        "policy": "finance"
      }
    }
  ],
  "data": {
    "guarded": null
  }
}
```

## Error with attribution: Allowed selections (reader) -> 200 OK

```json
{
  "data": {
    "product": {
      "id": "UHJvZHVjdDox",
      "name": "Product: UHJvZHVjdDox",
      "price": 123
    }
  }
}
```

## Error with attribution: Partial data beside a denied field (anonymous) -> 200 OK

```json
{
  "errors": [
    {
      "message": "The current user is not authenticated.",
      "path": [
        "me"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHENTICATED",
        "directive": "authenticated"
      }
    }
  ],
  "data": {
    "open": "Query",
    "me": null
  }
}
```

## Error with attribution: Denied non-null field under a nullable parent (reader) -> 200 OK

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

## Error with attribution: Denied non-null field under a non-null parent (reader) -> 200 OK

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
        "code": "AUTH_NOT_AUTHORIZED",
        "directive": "policy",
        "policy": "finance"
      }
    }
  ],
  "data": null
}
```

## Error with attribution: Denied nullable field in every list element (member) -> 200 OK

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
        "code": "AUTH_NOT_AUTHORIZED",
        "directive": "requiresScopes",
        "requiredScopes": [
          [
            "read"
          ]
        ]
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
        "code": "AUTH_NOT_AUTHORIZED",
        "directive": "requiresScopes",
        "requiredScopes": [
          [
            "read"
          ]
        ]
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
        "code": "AUTH_NOT_AUTHORIZED",
        "directive": "requiresScopes",
        "requiredScopes": [
          [
            "read"
          ]
        ]
      }
    }
  ],
  "data": {
    "products": [
      {
        "id": "UHJvZHVjdDox",
        "price": null
      },
      {
        "id": "UHJvZHVjdDoy",
        "price": null
      },
      {
        "id": "UHJvZHVjdDoz",
        "price": null
      }
    ]
  }
}
```

## Error with attribution: Denied non-null field in nullable list elements, one error per element (reader) -> 200 OK

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
        "code": "AUTH_NOT_AUTHORIZED",
        "directive": "policy",
        "policy": "finance"
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
        "code": "AUTH_NOT_AUTHORIZED",
        "directive": "policy",
        "policy": "finance"
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
        "code": "AUTH_NOT_AUTHORIZED",
        "directive": "policy",
        "policy": "finance"
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

## Error with attribution: Denied non-null field in non-null list elements, one error at the first violated index and the list is nulled (reader) -> 200 OK

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
        "code": "AUTH_NOT_AUTHORIZED",
        "directive": "policy",
        "policy": "finance"
      }
    }
  ],
  "data": {
    "strictProducts": null
  }
}
```

## Null with attribution: Unauthenticated nullable field (anonymous) -> 200 OK

```json
{
  "data": {
    "me": null
  }
}
```

## Null with attribution: Unauthorized nullable field (member) -> 200 OK

```json
{
  "data": {
    "admin": null
  }
}
```

## Null with attribution: Anonymous principal on a scope-protected field (anonymous) -> 200 OK

```json
{
  "data": {
    "admin": null
  }
}
```

## Null with attribution: Denied policy on nullable field (reader) -> 200 OK

```json
{
  "data": {
    "guarded": null
  }
}
```

## Null with attribution: Allowed selections (reader) -> 200 OK

```json
{
  "data": {
    "product": {
      "id": "UHJvZHVjdDox",
      "name": "Product: UHJvZHVjdDox",
      "price": 123
    }
  }
}
```

## Null with attribution: Partial data beside a denied field (anonymous) -> 200 OK

```json
{
  "data": {
    "open": "Query",
    "me": null
  }
}
```

## Null with attribution: Denied non-null field under a nullable parent (reader) -> 200 OK

```json
{
  "errors": [
    {
      "message": "Cannot return null for non-nullable field.",
      "path": [
        "product",
        "cost"
      ],
      "extensions": {
        "code": "HC0018"
      }
    }
  ],
  "data": {
    "product": null
  }
}
```

## Null with attribution: Denied non-null field under a non-null parent (reader) -> 200 OK

```json
{
  "errors": [
    {
      "message": "Cannot return null for non-nullable field.",
      "path": [
        "strictProduct",
        "cost"
      ],
      "extensions": {
        "code": "HC0018"
      }
    }
  ],
  "data": null
}
```

## Null with attribution: Denied nullable field in every list element (member) -> 200 OK

```json
{
  "data": {
    "products": [
      {
        "id": "UHJvZHVjdDox",
        "price": null
      },
      {
        "id": "UHJvZHVjdDoy",
        "price": null
      },
      {
        "id": "UHJvZHVjdDoz",
        "price": null
      }
    ]
  }
}
```

## Null with attribution: Denied non-null field in nullable list elements, one error per element (reader) -> 200 OK

```json
{
  "errors": [
    {
      "message": "Cannot return null for non-nullable field.",
      "path": [
        "products",
        0,
        "cost"
      ],
      "extensions": {
        "code": "HC0018"
      }
    },
    {
      "message": "Cannot return null for non-nullable field.",
      "path": [
        "products",
        1,
        "cost"
      ],
      "extensions": {
        "code": "HC0018"
      }
    },
    {
      "message": "Cannot return null for non-nullable field.",
      "path": [
        "products",
        2,
        "cost"
      ],
      "extensions": {
        "code": "HC0018"
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

## Null with attribution: Denied non-null field in non-null list elements, one error at the first violated index and the list is nulled (reader) -> 200 OK

```json
{
  "errors": [
    {
      "message": "Cannot return null for non-nullable field.",
      "path": [
        "strictProducts",
        0,
        "cost"
      ],
      "extensions": {
        "code": "HC0018"
      }
    }
  ],
  "data": {
    "strictProducts": null
  }
}
```
