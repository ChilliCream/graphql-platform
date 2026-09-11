# BatchResolver_Should_MatchFormatterOrderAndAfterNext_When_ContextsShortCircuit

## Regular result

```json
{
  "data": {
    "users": [
      {
        "value": "A(B(Alice))"
      },
      {
        "value": null
      },
      {
        "value": "A(B(Charlie))"
      }
    ]
  }
}
```

## Regular dispatched

```json
[
  1,
  3
]
```

## Regular formatters

```json
[
  "1:B",
  "1:A",
  "3:B",
  "3:A"
]
```

## Regular after next

```json
[
  "1:A(B(Alice))",
  "2:null",
  "3:A(B(Charlie))"
]
```

## Batch result

```json
{
  "data": {
    "users": [
      {
        "value": "A(B(Alice))"
      },
      {
        "value": null
      },
      {
        "value": "A(B(Charlie))"
      }
    ]
  }
}
```

## Batch dispatched

```json
[
  1,
  3
]
```

## Batch formatters

```json
[
  "1:B",
  "1:A",
  "3:B",
  "3:A"
]
```

## Batch after next

```json
[
  "1:A(B(Alice))",
  "2:null",
  "3:A(B(Charlie))"
]
```
