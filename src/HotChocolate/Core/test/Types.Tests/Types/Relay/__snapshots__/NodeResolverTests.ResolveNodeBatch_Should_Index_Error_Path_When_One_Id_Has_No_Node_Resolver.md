# ResolveNodeBatch_Should_Index_Error_Path_When_One_Id_Has_No_Node_Resolver

## Result

```json
{
  "errors": [
    {
      "message": "There is no node resolver registered for type `Query`.",
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
1
```
