# UsePaging_Should_Dispatch_PerVariableSet_When_IncludeConditionsDiffer

## Without totalCount

```json
{
  "variableIndex": 0,
  "data": {
    "brands": [
      {
        "name": "Brand 1",
        "pagedProducts": {
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
        "name": "Brand 2",
        "pagedProducts": {
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

## With totalCount

```json
{
  "variableIndex": 1,
  "data": {
    "brands": [
      {
        "name": "Brand 1",
        "pagedProducts": {
          "nodes": [
            {
              "name": "Brand 1 Product 1"
            },
            {
              "name": "Brand 1 Product 2"
            }
          ],
          "totalCount": 2
        }
      },
      {
        "name": "Brand 2",
        "pagedProducts": {
          "nodes": [
            {
              "name": "Brand 2 Product 1"
            },
            {
              "name": "Brand 2 Product 2"
            }
          ],
          "totalCount": 2
        }
      }
    ]
  }
}
```

## Observed batch dispatch count

```json
2
```
