# Paging_First_10_With_Default_Sorting_HasNextPage

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
        "hasNextPage": true,
        "hasPreviousPage": false,
        "endCursor": "MTA="
      }
    }
  },
  "extensions": {
    "sql": "-- @__p_0='11'\nSELECT b.\"Id\", b.\"AlwaysNull\", b.\"DisplayName\", b.\"Name\", b.\"BrandDetails_Country_Name\"\nFROM \"Brands\" AS b\nORDER BY b.\"Id\"\nLIMIT @__p_0"
  }
}
```
