# Evaluate_Should_ChargeOneBranch_When_IncludeAndSkipAreComplementary

## Operation

```graphql
query($x: Boolean!) {
  a: books(first: 3) @include(if: $x) {
    nodes {
      title
    }
  }
  b: books(first: 3) @skip(if: $x) {
    nodes {
      title
    }
  }
}
```

## Result

```text
{
  "data": {
    "a": {
      "nodes": []
    }
  },
  "extensions": {
    "operationCost": {
      "fieldCost": 11,
      "typeCost": 5
    }
  }
}
```
