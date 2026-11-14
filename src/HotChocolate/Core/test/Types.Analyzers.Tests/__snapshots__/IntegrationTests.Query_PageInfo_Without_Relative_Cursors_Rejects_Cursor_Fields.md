# Query_PageInfo_Without_Relative_Cursors_Rejects_Cursor_Fields

```json
{
  "errors": [
    {
      "message": "The field `forwardCursors` does not exist on the type `PageInfo`.",
      "locations": [
        {
          "line": 24,
          "column": 5
        }
      ],
      "extensions": {
        "type": "PageInfo",
        "field": "forwardCursors",
        "responseName": "forwardCursors",
        "specifiedBy": "https://spec.graphql.org/September2025/#sec-Field-Selections"
      }
    },
    {
      "message": "The field `backwardCursors` does not exist on the type `PageInfo`.",
      "locations": [
        {
          "line": 27,
          "column": 5
        }
      ],
      "extensions": {
        "type": "PageInfo",
        "field": "backwardCursors",
        "responseName": "backwardCursors",
        "specifiedBy": "https://spec.graphql.org/September2025/#sec-Field-Selections"
      }
    }
  ]
}
```
