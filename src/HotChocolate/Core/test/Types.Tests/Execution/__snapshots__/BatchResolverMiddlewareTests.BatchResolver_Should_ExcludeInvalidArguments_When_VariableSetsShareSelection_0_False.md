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
    1,
    2,
    3
  ]
]
```

## Partition arguments

```json
[
  1,
  2,
  3
]
```

## Delegate arguments

```json
[
  [
    1,
    2,
    3
  ]
]
```

## Non-pure children

```json
[
  "Profile 1",
  "Profile 2",
  "Profile 3"
]
```
