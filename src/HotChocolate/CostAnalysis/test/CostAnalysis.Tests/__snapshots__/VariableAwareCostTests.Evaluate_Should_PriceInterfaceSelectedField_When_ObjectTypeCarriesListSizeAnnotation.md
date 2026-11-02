# Evaluate_Should_PriceInterfaceSelectedField_When_ObjectTypeCarriesListSizeAnnotation

## Operation

```graphql
query($first: Int) {
  library {
    books(first: $first) {
      nodes {
        title
      }
    }
  }
}
```

## Result

```text
{
  "data": {
    "library": null
  },
  "extensions": {
    "operationCost": {
      "fieldCost": 3,
      "typeCost": 6
    }
  }
}
```
