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
  "data": null
}
```

## Set 1

```json
{
  "variableIndex": 1,
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
  "data": null
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
    3
  ]
]
```

## Partition arguments

```json
[]
```

## Delegate arguments

```json
[
  [
    3
  ]
]
```

## Non-pure children

```json
[
  "Profile 3"
]
```
