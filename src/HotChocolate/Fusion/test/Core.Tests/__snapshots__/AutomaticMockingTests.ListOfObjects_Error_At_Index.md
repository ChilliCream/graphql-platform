# ListOfObjects_Error_At_Index

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
        "objs",
        1
      ]
    }
  ],
  "data": {
    "objs": [
      {
        "id": "1"
      },
      null,
      {
        "id": "2"
      }
    ]
  }
}
```

## Request

```graphql
{
  objs {
    id
  }
}
```

