# Filtering_Specific_Filter_Used

## Operation

```graphql
{
  books(where: { title: { eq: "abc" } }) {
    nodes {
      title
    }
  }
}
```

## Expected

```json
{
  "fieldCost": 9,
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
      "fieldCost": 32,
      "typeCost": 52
    }
  }
}
```

