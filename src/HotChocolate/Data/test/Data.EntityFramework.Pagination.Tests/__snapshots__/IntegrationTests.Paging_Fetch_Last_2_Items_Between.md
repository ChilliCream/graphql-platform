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
        "endCursor": "OTk=",
        "startCursor": "OTg="
      }
    }
  },
  "extensions": {
    "sql": "-- @__value_0='97'\n-- @__value_1='100'\n-- @__p_2='3'\nSELECT b.\"Id\", b.\"AlwaysNull\", b.\"DisplayName\", b.\"Name\", b.\"BrandDetails_Country_Name\"\nFROM \"Brands\" AS b\nWHERE b.\"Id\" > @__value_0 AND b.\"Id\" < @__value_1\nORDER BY b.\"Id\" DESC\nLIMIT @__p_2"
  }
}
```
