# DeferredBatch_Should_RegisterAllProducers_When_VariableSetsShareSelection

## Per-set payloads and errors

```json
[
  {
    "VariableIndex": 0,
    "Name": "Product 1",
    "Errors": ""
  },
  {
    "VariableIndex": 1,
    "Name": "Product 2",
    "Errors": "Product 2 failed."
  }
]
```

## Per-set arguments

```json
[
  1,
  2
]
```

## Invocations

```json
[
  2
]
```
