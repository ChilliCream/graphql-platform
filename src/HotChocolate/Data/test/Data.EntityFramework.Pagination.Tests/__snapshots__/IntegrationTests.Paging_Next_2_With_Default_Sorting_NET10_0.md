# Paging_Next_2_With_Default_Sorting

```json
{
  "data": {
    "brands": {
      "nodes": [
        {
          "name": "Brand10"
        },
        {
          "name": "Brand11"
        }
      ],
      "pageInfo": {
        "endCursor": "e30xMg=="
      }
    }
  },
  "extensions": {
    "sql": "-- @value='10'\n-- @p='3'\nSELECT b.\"Id\", b.\"AlwaysNull\", b.\"DisplayName\", b.\"Name\", b.\"BrandDetails_Country_Name\"\nFROM \"Brands\" AS b\nWHERE b.\"Id\" > @value\nORDER BY b.\"Id\"\nLIMIT @p"
  }
}
```
