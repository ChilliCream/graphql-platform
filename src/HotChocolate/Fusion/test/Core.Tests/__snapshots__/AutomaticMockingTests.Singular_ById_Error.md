# Singular_ById_Error

## Result

```json
{
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "locations": [
        {
          "line": 2,
          "column": 3
        }
      ],
      "path": [
        "productById"
      ]
    }
  ],
  "data": {
    "productById": null
  }
}
```

## Request

```graphql
{
  productById(id: "5") {
    id
  }
}
```

