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
      "message": "Unexpected Execution Error",
      "path": [
        "productById"
      ]
    }
  ],
  "data": null
}
```

## Batch sizes

```json
[
  1
]
```
