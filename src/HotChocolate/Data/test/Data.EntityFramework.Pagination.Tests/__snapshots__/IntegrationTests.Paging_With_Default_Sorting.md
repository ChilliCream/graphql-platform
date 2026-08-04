# Paging_With_Default_Sorting

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
          "name": "Brand2"
        },
        {
          "name": "Brand3"
        },
        {
          "name": "Brand4"
        },
        {
          "name": "Brand5"
        },
        {
          "name": "Brand6"
        },
        {
          "name": "Brand7"
        },
        {
          "name": "Brand8"
        },
        {
          "name": "Brand9"
        }
      ],
      "pageInfo": {
        "endCursor": "e30xMA=="
      }
    }
  },
  "extensions": {
    "sql": "-- @p='11'\nSELECT b.\"Id\", b.\"AlwaysNull\", b.\"DisplayName\", b.\"Name\", b.\"BrandDetails_Country_Name\"\nFROM \"Brands\" AS b\nORDER BY b.\"Id\"\nLIMIT @p"
  }
}
```
