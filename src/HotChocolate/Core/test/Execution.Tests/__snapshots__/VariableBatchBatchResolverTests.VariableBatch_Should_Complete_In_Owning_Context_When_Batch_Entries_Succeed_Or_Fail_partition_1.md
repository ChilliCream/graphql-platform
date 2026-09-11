# VariableBatch_Should_Complete_In_Owning_Context_When_Batch_Entries_Succeed_Or_Fail

## Set 0

```json
{
  "variableIndex": 0,
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

## Set 1

```json
{
  "variableIndex": 1,
  "data": {
    "productById": {
      "name": "Product 2",
      "argument": 2
    }
  }
}
```

## Batch sizes

```json
[
  1
]
```
