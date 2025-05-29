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
        "endCursor": "e31CcmFuZDE5OjIw"
      }
    }
  },
  "extensions": {
    "sql": "-- @value='Brand17'\n-- @value1='18'\n-- @p='3'\nSELECT b.\"Id\", b.\"AlwaysNull\", b.\"DisplayName\", b.\"Name\", b.\"BrandDetails_Country_Name\"\nFROM \"Brands\" AS b\nWHERE b.\"Name\" > @value OR (b.\"Name\" = @value AND b.\"Id\" > @value1)\nORDER BY b.\"Name\", b.\"Id\"\nLIMIT @p"
  }
}
```
