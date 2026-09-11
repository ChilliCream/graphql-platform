# BatchSelection_Should_UnionIncludedMembers_When_ConditionsDiffer

## Set 0

```json
{
  "variableIndex": 0,
  "data": {
    "product": {
      "id": 1,
      "right": "right"
    }
  }
}
```

## Set 1

```json
{
  "variableIndex": 1,
  "data": {
    "product": {
      "id": 2,
      "left": "left"
    }
  }
}
```

## Skipped set

```json
{
  "variableIndex": 2,
  "data": {}
}
```

## Union binding

```json
[
  {
    "Ids": [
      1
    ],
    "Left": false,
    "Right": true,
    "Excluded": false,
    "SelectLeft": false,
    "SelectRight": true,
    "SelectExcluded": false,
    "Pattern": false,
    "SameSelection": true
  },
  {
    "Ids": [
      2
    ],
    "Left": true,
    "Right": false,
    "Excluded": false,
    "SelectLeft": true,
    "SelectRight": false,
    "SelectExcluded": false,
    "Pattern": false,
    "SameSelection": true
  }
]
```
