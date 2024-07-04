# Require_Paging_Boundaries_Two_Boundaries_With_Variable

## Operation

```graphql
query($first: Int, $last: Int) {
  books(first: $first, last: $last) {
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

