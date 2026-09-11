# PagingArguments_Should_UseClampedDefault_When_BatchReturnsPageShapes

## Omitted default

```json
{
  "variableIndex": 0,
  "data": {
    "brands": [
      {
        "products": {
          "nodes": [
            {
              "name": "Brand 1 Product 1"
            },
            {
              "name": "Brand 1 Product 2"
            }
          ]
        }
      },
      {
        "products": {
          "nodes": [
            {
              "name": "Brand 2 Product 1"
            },
            {
              "name": "Brand 2 Product 2"
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
        "products": {
          "nodes": [
            {
              "name": "Brand 1 Product 1"
            },
            {
              "name": "Brand 1 Product 2"
            }
          ]
        }
      },
      {
        "products": {
          "nodes": [
            {
              "name": "Brand 2 Product 1"
            },
            {
              "name": "Brand 2 Product 2"
            }
          ]
        }
      }
    ]
  }
}
```

## Invalid sibling

```json
{
  "variableIndex": 2,
  "errors": [
    {
      "message": "The maximum allowed items per page were exceeded.",
      "path": [
        "brands",
        0,
        "products"
      ],
      "extensions": {
        "code": "HC0051",
        "coordinate": "ConnectionBrand.products",
        "requestedItems": 3,
        "maxAllowedItems": 2
      }
    },
    {
      "message": "The maximum allowed items per page were exceeded.",
      "path": [
        "brands",
        1,
        "products"
      ],
      "extensions": {
        "code": "HC0051",
        "coordinate": "ConnectionBrand.products",
        "requestedItems": 3,
        "maxAllowedItems": 2
      }
    }
  ],
  "data": null
}
```
