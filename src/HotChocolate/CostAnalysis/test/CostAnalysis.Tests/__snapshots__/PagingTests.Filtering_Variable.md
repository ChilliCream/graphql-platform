# Filtering_Variable

## Operation

```graphql
query(
  $where: BookFilterInput
) {
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
      "fieldCost": 901,
      "typeCost": 12
    }
  }
}
```

