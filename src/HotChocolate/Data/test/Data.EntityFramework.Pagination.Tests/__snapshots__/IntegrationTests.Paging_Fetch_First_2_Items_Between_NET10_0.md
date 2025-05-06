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
        "endCursor": "e30z",
        "startCursor": "e30y"
      }
    }
  },
  "extensions": {
    "sql": "-- @value='1'\n-- @value0='4'\n-- @p='3'\nSELECT b.\"Id\", b.\"AlwaysNull\", b.\"DisplayName\", b.\"Name\", b.\"BrandDetails_Country_Name\"\nFROM \"Brands\" AS b\nWHERE b.\"Id\" > @value AND b.\"Id\" < @value0\nORDER BY b.\"Id\"\nLIMIT @p"
  }
}
```
