# UsePaging_Should_Coalesce_When_PagingArgumentsAreIdentical

## Omitted first

```json
{
  "variableIndex": 0,
  "data": {
    "brands": [
      {
        "name": "Brand 1",
        "products": {
          "nodes": [
            {
              "name": "Brand 1 P1"
            },
            {
              "name": "Brand 1 P2"
            }
          ]
        }
      },
      {
        "name": "Brand 2",
        "products": {
          "nodes": [
            {
              "name": "Brand 2 P1"
            },
            {
              "name": "Brand 2 P2"
            }
          ]
        }
      }
    ]
  }
}
```

## Explicit effective default

```json
{
  "variableIndex": 1,
  "data": {
    "brands": [
      {
        "name": "Brand 1",
        "products": {
          "nodes": [
            {
              "name": "Brand 1 P1"
            },
            {
              "name": "Brand 1 P2"
            }
          ]
        }
      },
      {
        "name": "Brand 2",
        "products": {
          "nodes": [
            {
              "name": "Brand 2 P1"
            },
            {
              "name": "Brand 2 P2"
            }
          ]
        }
      }
    ]
  }
}
```
