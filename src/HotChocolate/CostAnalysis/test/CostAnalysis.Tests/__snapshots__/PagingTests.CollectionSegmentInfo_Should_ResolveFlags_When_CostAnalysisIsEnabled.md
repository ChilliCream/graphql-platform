# CollectionSegmentInfo_Should_ResolveFlags_When_CostAnalysisIsEnabled

## Operation

```graphql
{
  pagedBooks(skip: 1, take: 1) {
    items {
      title
    }
    pageInfo {
      hasNextPage
      hasPreviousPage
    }
  }
}
```

## Response

```json
{
  "data": {
    "pagedBooks": {
      "items": [
        {
          "title": "B"
        }
      ],
      "pageInfo": {
        "hasNextPage": true,
        "hasPreviousPage": true
      }
    }
  },
  "extensions": {
    "operationCost": {
      "fieldCost": 3,
      "typeCost": 4
    }
  }
}
```
