# ListOfObjects_Property_Error_At_Index

## Result

```json
{
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "locations": [
        {
          "line": 3,
          "column": 5
        }
      ],
      "path": [
        "objs",
        1,
        "str"
      ]
    }
  ],
  "data": {
    "objs": [
      {
        "str": "string"
      },
      {
        "str": null
      },
      {
        "str": "string"
      }
    ]
  }
}
```

## Request

```graphql
{
  objs {
    str
  }
}
```

