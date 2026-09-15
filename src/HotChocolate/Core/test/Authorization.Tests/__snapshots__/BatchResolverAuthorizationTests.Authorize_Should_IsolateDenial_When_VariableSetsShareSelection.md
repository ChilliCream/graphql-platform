# Authorize_Should_IsolateDenial_When_VariableSetsShareSelection

## Set 0

```json
{
  "variableIndex": 0,
  "data": {
    "secret": "secret-1"
  }
}
```

## Set 1

```json
{
  "variableIndex": 1,
  "errors": [
    {
      "message": "The default authorization policy does not exist.",
      "path": [
        "secret"
      ],
      "extensions": {
        "code": "AUTH_NO_DEFAULT_POLICY"
      }
    }
  ],
  "data": {
    "secret": null
  }
}
```

## Set 2

```json
{
  "variableIndex": 2,
  "data": {
    "secret": "secret-3"
  }
}
```

## Authorization calls

```json
[
  "FIRST:1",
  "FIRST:2",
  "FIRST:3",
  "SECOND:1",
  "SECOND:3"
]
```

## Resolver batches

```json
[
  [
    1,
    3
  ]
]
```
