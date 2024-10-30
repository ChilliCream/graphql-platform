# Require_Paging_Boundaries_Single_Boundary_With_Variable

## Operation

```graphql
query($first: Int) {
  books(first: $first) {
    nodes {
      title
    }
  }
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

