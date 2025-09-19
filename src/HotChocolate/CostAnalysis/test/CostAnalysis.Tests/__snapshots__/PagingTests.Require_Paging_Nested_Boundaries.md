# Require_Paging_Nested_Boundaries

## Operation

```graphql
{
  books {
    nodes {
      title
      authors {
        nodes {
          name
        }
      }
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
          "line": 5,
          "column": 13
        }
      ],
      "path": [
        "books",
        "nodes",
        "authors"
      ],
      "extensions": {
        "code": "HC0082"
      }
    }
  ]
}
```

