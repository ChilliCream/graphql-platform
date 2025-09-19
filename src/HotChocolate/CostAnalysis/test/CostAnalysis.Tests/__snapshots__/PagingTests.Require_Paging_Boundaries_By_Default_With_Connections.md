# Require_Paging_Boundaries_By_Default_With_Connections

## Operation

```graphql
{
  books {
    nodes {
      title
    }
  }
}
```

## Response

```json
{
  "errors": [
    {
      "message": "Exactly one slicing argument must be defined.",
      "locations": [
        {
          "line": 2,
          "column": 5
        }
      ],
      "path": [
        "books"
      ],
      "extensions": {
        "code": "HC0082"
      }
    }
  ]
}
```

