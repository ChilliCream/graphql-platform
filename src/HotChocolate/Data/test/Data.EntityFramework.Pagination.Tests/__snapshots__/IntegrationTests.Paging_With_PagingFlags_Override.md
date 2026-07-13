# Paging_With_PagingFlags_Override

```json
{
  "data": {
    "products": {
      "pageCount": 10
    }
  },
  "extensions": {
    "sql": "-- @p='11'\nSELECT p.\"Id\", p.\"AvailableStock\", p.\"BrandId\", p.\"Description\", p.\"ImageFileName\", p.\"MaxStockThreshold\", p.\"Name\", p.\"OnReorder\", p.\"Price\", p.\"RestockThreshold\", p.\"TypeId\"\nFROM \"Products\" AS p\nORDER BY p.\"Id\"\nLIMIT @p"
  }
}
```
