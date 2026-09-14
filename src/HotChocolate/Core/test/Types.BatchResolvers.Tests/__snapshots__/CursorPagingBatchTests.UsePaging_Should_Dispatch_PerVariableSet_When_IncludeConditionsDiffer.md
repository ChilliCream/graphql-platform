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
        "pagedProducts": {
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
              "name": "Brand 1 P1"
            },
            {
              "name": "Brand 1 P2"
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
              "name": "Brand 2 P1"
            },
            {
              "name": "Brand 2 P2"
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
