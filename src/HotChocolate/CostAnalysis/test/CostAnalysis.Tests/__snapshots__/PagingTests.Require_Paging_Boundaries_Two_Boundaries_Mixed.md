# Require_Paging_Boundaries_Two_Boundaries_Mixed

## Operation

```graphql
query($first: Int) {
  books(first: $first, last: 1) {
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

