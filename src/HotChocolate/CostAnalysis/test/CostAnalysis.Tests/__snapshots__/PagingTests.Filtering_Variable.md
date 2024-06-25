# Filtering_Variable

## Operation

```graphql
query($where: BookFilterInput) {
  books(where: $where) {
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
    "cost": {
      "fieldCost": 31,
      "typeCost": 52
    }
  }
}
```

