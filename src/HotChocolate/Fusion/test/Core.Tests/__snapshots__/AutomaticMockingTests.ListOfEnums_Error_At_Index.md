# ListOfEnums_Error_At_Index

## Result

```json
{
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "locations": [
        {
          "line": 2,
          "column": 3
        }
      ],
      "path": [
        "enums",
        1
      ]
    }
  ],
  "data": {
    "enums": [
      "VALUE",
      null,
      "VALUE"
    ]
  }
}
```

## Request

```graphql
{
  enums
}
```

