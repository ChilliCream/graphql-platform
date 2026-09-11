# VariableBatch_Should_Complete_In_Owning_Context_When_Batch_Entries_Succeed_Or_Fail

## Set 0

```json
{
  "variableIndex": 0,
  "data": {
    "productById": {
      "name": "Product 1",
      "argument": 1
    }
  }
}
```

## Set 1

```json
{
  "variableIndex": 1,
  "errors": [
    {
      "message": "Cannot return null for non-nullable field.",
      "path": [
        "productById"
      ],
      "extensions": {
        "code": "HC0018"
      }
    }
  ],
  "data": null
}
```

## Batch sizes

```json
[
  2
]
```
