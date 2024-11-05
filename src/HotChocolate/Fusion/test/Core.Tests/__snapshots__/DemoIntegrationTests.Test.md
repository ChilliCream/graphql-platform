# Test

## Result

```json
{
  "errors": [
    {
      "message": "Unexpected Execution Error"
    }
  ]
}
```

## Request

```graphql
{
  wrapper {
    id
    items {
      __typename
      ... on Item1 {
        id
        product {
          id
          name
        }
      }
      ... on Item2 {
        id
        product {
          id
          name
        }
      }
      ... on Item3 {
        id
        product {
          id
          name
        }
      }
    }
  }
}
```

