# Filtering_Specific_Expensive_Filter_Used

## Operation

```graphql
{
  books(where: { title: { contains: "abc" } }) {
    nodes {
      title
    }
  }
}
```

## Expected

```json
{
  "fieldCost": 10,
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
      "fieldCost": 42,
      "typeCost": 52
    }
  }
}
```

