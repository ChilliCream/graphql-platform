# UsePaging_Should_Map_PagingArguments_PerSelection_When_ParameterIsPagingArguments

## Result 1

```json
{
  "data": {
    "brands": [
      {
        "name": "Brand 1",
        "small": {
          "nodes": [
            {
              "name": "Brand 1 P1"
            }
          ]
        },
        "large": {
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
        "small": {
          "nodes": [
            {
              "name": "Brand 2 P1"
            }
          ]
        },
        "large": {
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

## Observed PagingArguments per dispatch

```json
[
  {
    "First": 1,
    "After": null,
    "Last": null,
    "Before": null
  },
  {
    "First": 2,
    "After": null,
    "Last": null,
    "Before": null
  }
]
```
