# BatchResolver_Should_MatchFormatterOrderAndAfterNext_When_ContextsShortCircuit

## Regular result

```json
{
  "data": {
    "users": [
      {
        "value": "cached"
      },
      {
        "value": "cached"
      },
      {
        "value": "cached"
      }
    ]
  }
}
```

## Regular dispatched

```json
[]
```

## Regular formatters

```json
[]
```

## Regular after next

```json
[
  "1:cached",
  "2:cached",
  "3:cached"
]
```

## Batch result

```json
{
  "data": {
    "users": [
      {
        "value": "cached"
      },
      {
        "value": "cached"
      },
      {
        "value": "cached"
      }
    ]
  }
}
```

## Batch dispatched

```json
[]
```

## Batch formatters

```json
[]
```

## Batch after next

```json
[
  "1:cached",
  "2:cached",
  "3:cached"
]
```
