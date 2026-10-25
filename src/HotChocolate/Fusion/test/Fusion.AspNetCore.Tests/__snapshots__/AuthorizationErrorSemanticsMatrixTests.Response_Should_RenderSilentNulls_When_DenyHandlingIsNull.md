# Response_Should_RenderSilentNulls_When_DenyHandlingIsNull

## Null: Unauthenticated nullable field (anonymous) -> 200 OK

```json
{
  "data": {
    "me": null
  }
}
```

## Null: Unauthorized nullable field (member) -> 200 OK

```json
{
  "data": {
    "admin": null
  }
}
```

## Null: Anonymous principal on a scope-protected field (anonymous) -> 200 OK

```json
{
  "data": {
    "admin": null
  }
}
```

## Null: Denied policy on nullable field (reader) -> 200 OK

```json
{
  "data": {
    "guarded": null
  }
}
```

## Null: Allowed selections (reader) -> 200 OK

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

## Null: Partial data beside a denied field (anonymous) -> 200 OK

```json
{
  "data": {
    "open": "Query",
    "me": null
  }
}
```

## Null: Denied non-null field under a nullable parent (reader) -> 200 OK

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

## Null: Denied non-null field under a non-null parent (reader) -> 200 OK

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

## Null: Denied nullable field in every list element (member) -> 200 OK

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

## Null: Denied non-null field in nullable list elements, one error per element (reader) -> 200 OK

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

## Null: Denied non-null field in non-null list elements, one error at the first violated index and the list is nulled (reader) -> 200 OK

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
