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
      "fieldCost": 901,
      "typeCost": 52
    }
  }
}
```

