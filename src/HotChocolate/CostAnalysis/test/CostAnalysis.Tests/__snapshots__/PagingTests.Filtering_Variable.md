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
  "fieldCost": 32,
  "typeCost": 12
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
      "typeCost": 12
    }
  }
}
```
