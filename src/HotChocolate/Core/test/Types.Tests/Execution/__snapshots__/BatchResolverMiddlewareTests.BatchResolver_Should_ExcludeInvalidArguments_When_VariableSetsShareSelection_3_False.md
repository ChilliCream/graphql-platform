# BatchResolver_Should_ExcludeInvalidArguments_When_VariableSetsShareSelection

## Set 0

```json
{
  "variableIndex": 0,
  "data": {
    "profile": {
      "displayName": "Profile 1"
    }
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

## Middleware arguments

```json
[
  [
    1,
    2
  ]
]
```

## Partition arguments

```json
[
  1,
  2
]
```

## Delegate arguments

```json
[
  [
    1,
    2
  ]
]
```

## Non-pure children

```json
[
  "Profile 1",
  "Profile 2"
]
```
