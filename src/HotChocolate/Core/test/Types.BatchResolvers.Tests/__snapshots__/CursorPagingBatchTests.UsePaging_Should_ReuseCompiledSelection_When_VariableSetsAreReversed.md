# UsePaging_Should_ReuseCompiledSelection_When_VariableSetsAreReversed

## Reversed set 0 (includeTotal:true)

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

## Reversed set 1 (includeTotal:false)

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

## Total observed batch dispatch count across both runs

```json
4
```
