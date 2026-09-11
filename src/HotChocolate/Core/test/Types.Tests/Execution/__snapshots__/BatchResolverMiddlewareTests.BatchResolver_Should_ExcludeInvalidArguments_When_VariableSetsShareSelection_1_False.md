# BatchResolver_Should_ExcludeInvalidArguments_When_VariableSetsShareSelection

## Set 0

```json
{
  "variableIndex": 0,
  "errors": [
    {
      "message": "Cannot accept null for non-nullable input.",
      "locations": [
        {
          "line": 1,
          "column": 30
        }
      ],
      "path": [
        "profile"
      ],
      "extensions": {
        "code": "HC0018",
        "inputPath": [
          "id"
        ]
      }
    }
  ],
  "data": {
    "profile": null
  }
}
```

## Set 1

```json
{
  "variableIndex": 1,
  "data": {
    "profile": {
      "displayName": "Profile 2"
    }
  }
}
```

## Set 2

```json
{
  "variableIndex": 2,
  "data": {
    "profile": {
      "displayName": "Profile 3"
    }
  }
}
```

## Middleware arguments

```json
[
  [
    2,
    3
  ]
]
```

## Partition arguments

```json
[
  2,
  3
]
```

## Delegate arguments

```json
[
  [
    2,
    3
  ]
]
```

## Non-pure children

```json
[
  "Profile 2",
  "Profile 3"
]
```
