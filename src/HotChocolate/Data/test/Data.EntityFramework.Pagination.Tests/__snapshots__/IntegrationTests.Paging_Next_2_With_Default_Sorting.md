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
    "sql": "-- @__value_0='10'\n-- @__p_1='3'\nSELECT b.\"Id\", b.\"AlwaysNull\", b.\"DisplayName\", b.\"Name\", b.\"BrandDetails_Country_Name\"\nFROM \"Brands\" AS b\nWHERE b.\"Id\" > @__value_0\nORDER BY b.\"Id\"\nLIMIT @__p_1"
  }
}
```
