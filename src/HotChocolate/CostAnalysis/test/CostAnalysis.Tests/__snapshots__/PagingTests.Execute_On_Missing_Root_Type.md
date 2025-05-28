# Execute_On_Missing_Root_Type

## Operation

```graphql
subscription {
  books {
    nodes {
      title
    }
  }
}
```

## Expected

```json
{
  "fieldCost": 6,
  "typeCost": 12
}
```

## Response

```json
{
  "errors": [
    {
      "message": "This GraphQL schema does not support `Subscription` operations."
    }
  ]
}
```

