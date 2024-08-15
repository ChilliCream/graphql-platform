# Paging_With_Default_Sorting_And_TotalCount

```json
{
  "data": {
    "products": {
      "nodes": [
        {
          "name": "Product 0-0"
        },
        {
          "name": "Product 0-1"
        },
        {
          "name": "Product 0-2"
        },
        {
          "name": "Product 0-3"
        },
        {
          "name": "Product 0-4"
        },
        {
          "name": "Product 0-5"
        },
        {
          "name": "Product 0-6"
        },
        {
          "name": "Product 0-7"
        },
        {
          "name": "Product 0-8"
        },
        {
          "name": "Product 0-9"
        }
      ],
      "pageInfo": {
        "endCursor": "MTA="
      },
      "totalCount": 10000
    }
  },
  "extensions": {
    "sql": "-- @__p_0='11'\nSELECT p.\"Id\", p.\"AvailableStock\", p.\"BrandId\", p.\"Description\", p.\"ImageFileName\", p.\"MaxStockThreshold\", p.\"Name\", p.\"OnReorder\", p.\"Price\", p.\"RestockThreshold\", p.\"TypeId\"\nFROM \"Products\" AS p\nORDER BY p.\"Id\"\nLIMIT @__p_0"
  }
}
```
