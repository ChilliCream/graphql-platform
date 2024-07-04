# Require_Paging_Boundaries_Single_Boundary_With_Literal

## Operation

```graphql
{
  books(first: 1) {
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
      "typeCost": 3
    }
  }
}
```

