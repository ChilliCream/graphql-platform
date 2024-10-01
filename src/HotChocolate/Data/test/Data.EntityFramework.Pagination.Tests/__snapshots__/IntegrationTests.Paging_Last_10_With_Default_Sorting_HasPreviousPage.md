# Paging_Last_10_With_Default_Sorting_HasPreviousPage

```json
{
  "data": {
    "brands": {
      "nodes": [
        {
          "name": "Brand90"
        },
        {
          "name": "Brand91"
        },
        {
          "name": "Brand92"
        },
        {
          "name": "Brand93"
        },
        {
          "name": "Brand94"
        },
        {
          "name": "Brand95"
        },
        {
          "name": "Brand96"
        },
        {
          "name": "Brand97"
        },
        {
          "name": "Brand98"
        },
        {
          "name": "Brand99"
        }
      ],
      "pageInfo": {
        "hasNextPage": false,
        "hasPreviousPage": true,
        "endCursor": "MTAw"
      }
    }
  },
  "extensions": {
    "sql": "-- @__p_0='11'\nSELECT b.\"Id\", b.\"AlwaysNull\", b.\"DisplayName\", b.\"Name\", b.\"BrandDetails_Country_Name\"\nFROM \"Brands\" AS b\nORDER BY b.\"Id\" DESC\nLIMIT @__p_0"
  }
}
```
