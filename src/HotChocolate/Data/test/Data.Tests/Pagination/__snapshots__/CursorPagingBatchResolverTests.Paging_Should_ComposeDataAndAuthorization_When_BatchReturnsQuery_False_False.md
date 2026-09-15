# Paging_Should_ComposeDataAndAuthorization_When_BatchReturnsQuery

## Result

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "parents",
        0,
        "products"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ],
  "data": {
    "parents": [
      {
        "products": null
      },
      {
        "products": {
          "nodes": [
            {
              "name": "P3",
              "rank": 3
            }
          ],
          "totalCount": 2
        }
      }
    ]
  }
}
```

## Projected page items

```json
[
  {
    "Rank": 3,
    "Name": "P3",
    "Unselected": null
  }
]
```

## Authorized resolver batches

```json
[
  [
    2
  ]
]
```
