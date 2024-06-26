# Filtering_Not_Used

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

## Expected

```json
{
  "fieldCost": 6,
  "typeCost": 52
}
```

## Response

```json
{
  "data": {
    "books": {
      "nodes": []
    }
  },
  "extensions": {
    "operationCost": {
      "fieldCost": 11,
      "typeCost": 52
    }
  }
}
```

