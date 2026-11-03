# PageInfo_Should_KeepStaticCost_When_SelectedOnClassicConnection

## Operation

```graphql
{
  books(first: 1) {
    pageInfo {
      hasNextPage
      endCursor
    }
  }
}
```

## Response

```json
{
  "data": {
    "books": {
      "pageInfo": {
        "hasNextPage": false,
        "endCursor": null
      }
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
