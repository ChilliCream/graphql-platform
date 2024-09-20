# Paging_With_User_Sorting

```json
{
  "data": {
    "brands": {
      "nodes": [
        {
          "name": "Brand0"
        },
        {
          "name": "Brand1"
        },
        {
          "name": "Brand10"
        },
        {
          "name": "Brand11"
        },
        {
          "name": "Brand12"
        },
        {
          "name": "Brand13"
        },
        {
          "name": "Brand14"
        },
        {
          "name": "Brand15"
        },
        {
          "name": "Brand16"
        },
        {
          "name": "Brand17"
        }
      ],
      "pageInfo": {
        "endCursor": "QnJhbmQxNzoxOA=="
      }
    }
  },
  "extensions": {
    "sql": "-- @__p_0='11'\nSELECT b.\"Id\", b.\"AlwaysNull\", b.\"DisplayName\", b.\"Name\", b.\"BrandDetails_Country_Name\"\nFROM \"Brands\" AS b\nORDER BY b.\"Name\", b.\"Id\"\nLIMIT @__p_0"
  }
}
```
