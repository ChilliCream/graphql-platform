# Paging_Next_2_With_User_Sorting

```json
{
  "data": {
    "brands": {
      "nodes": [
        {
          "name": "Brand18"
        },
        {
          "name": "Brand19"
        }
      ],
      "pageInfo": {
        "endCursor": "QnJhbmQxOToyMA=="
      }
    }
  },
  "extensions": {
    "sql": "-- @__value_0='Brand17'\n-- @__value_1='18'\n-- @__p_2='3'\nSELECT b.\"Id\", b.\"AlwaysNull\", b.\"DisplayName\", b.\"Name\", b.\"BrandDetails_Country_Name\"\nFROM \"Brands\" AS b\nWHERE b.\"Name\" > @__value_0 OR (b.\"Name\" = @__value_0 AND b.\"Id\" > @__value_1)\nORDER BY b.\"Name\", b.\"Id\"\nLIMIT @__p_2"
  }
}
```
