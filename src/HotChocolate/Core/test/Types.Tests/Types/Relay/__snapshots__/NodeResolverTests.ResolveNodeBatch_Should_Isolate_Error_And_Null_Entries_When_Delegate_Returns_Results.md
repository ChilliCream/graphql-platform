# ResolveNodeBatch_Should_Isolate_Error_And_Null_Entries_When_Delegate_Returns_Results

## Result

```json
{
  "errors": [
    {
      "message": "missing node",
      "path": [
        "nodes"
      ]
    }
  ],
  "data": {
    "nodes": [
      {
        "name": "x"
      },
      null,
      null,
      {
        "name": "x"
      }
    ]
  }
}
```

## Calls

```json
1
```
