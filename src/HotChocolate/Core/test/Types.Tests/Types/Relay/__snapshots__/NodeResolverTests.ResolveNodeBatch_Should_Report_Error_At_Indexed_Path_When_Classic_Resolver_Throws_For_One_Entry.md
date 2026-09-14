# ResolveNodeBatch_Should_Report_Error_At_Indexed_Path_When_Classic_Resolver_Throws_For_One_Entry

## Result

```json
{
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "path": [
        "nodes",
        1
      ]
    }
  ],
  "data": {
    "nodes": [
      {
        "name": "x"
      },
      null
    ]
  }
}
```

## Calls

```json
2
```
