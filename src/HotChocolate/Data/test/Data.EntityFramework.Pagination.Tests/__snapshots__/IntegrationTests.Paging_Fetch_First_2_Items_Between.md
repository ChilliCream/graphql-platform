# Paging_Fetch_First_2_Items_Between

```json
{
  "data": {
    "brands": {
      "nodes": [
        {
          "name": "Brand1"
        },
        {
          "name": "Brand2"
        }
      ],
      "pageInfo": {
        "hasNextPage": true,
        "hasPreviousPage": true,
        "endCursor": "Mw==",
        "startCursor": "Mg=="
      }
    }
  },
  "extensions": {
    "sql": "-- @__p_0='1'\n-- @__p_1='4'\n-- @__p_2='3'\nSELECT b.\"Id\", b.\"AlwaysNull\", b.\"DisplayName\", b.\"Name\", b.\"BrandDetails_Country_Name\"\nFROM \"Brands\" AS b\nWHERE b.\"Id\" > @__p_0 AND b.\"Id\" < @__p_1\nORDER BY b.\"Id\"\nLIMIT @__p_2"
  }
}
```
