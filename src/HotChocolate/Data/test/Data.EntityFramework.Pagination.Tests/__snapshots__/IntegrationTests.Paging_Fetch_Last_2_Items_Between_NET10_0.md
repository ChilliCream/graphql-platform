# Paging_Fetch_Last_2_Items_Between

```json
{
  "data": {
    "brands": {
      "nodes": [
        {
          "name": "Brand97"
        },
        {
          "name": "Brand98"
        }
      ],
      "pageInfo": {
        "hasNextPage": true,
        "hasPreviousPage": true,
        "endCursor": "e305OQ==",
        "startCursor": "e305OA=="
      }
    }
  },
  "extensions": {
    "sql": "-- @value='97'\n-- @value0='100'\n-- @p='3'\nSELECT b.\"Id\", b.\"AlwaysNull\", b.\"DisplayName\", b.\"Name\", b.\"BrandDetails_Country_Name\"\nFROM \"Brands\" AS b\nWHERE b.\"Id\" > @value AND b.\"Id\" < @value0\nORDER BY b.\"Id\" DESC\nLIMIT @p"
  }
}
```
