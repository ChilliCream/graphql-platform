# Query_Brands_With_BatchResolver_Supplier

## Result

```json
{
  "data": {
    "brands": {
      "nodes": [
        {
          "id": "QnJhbmQ6MTE=",
          "name": "Zephyr",
          "supplier": {
            "name": "Prime Distribution",
            "website": "https://primedist.example.com"
          }
        },
        {
          "id": "QnJhbmQ6MTM=",
          "name": "XE",
          "supplier": {
            "name": "Global Supply Co.",
            "website": "https://globalsupply.example.com"
          }
        },
        {
          "id": "QnJhbmQ6Mw==",
          "name": "WildRunner",
          "supplier": {
            "name": "Atlas Logistics",
            "website": "https://atlaslogistics.example.com"
          }
        },
        {
          "id": "QnJhbmQ6Nw==",
          "name": "Solstix",
          "supplier": {
            "name": "Global Supply Co.",
            "website": "https://globalsupply.example.com"
          }
        },
        {
          "id": "QnJhbmQ6Ng==",
          "name": "Raptor Elite",
          "supplier": {
            "name": "Atlas Logistics",
            "website": "https://atlaslogistics.example.com"
          }
        }
      ]
    }
  }
}
```

## Query 1

```sql
-- @p='6'
SELECT b."Id", b."Name", b."SupplierId"
FROM "Brands" AS b
ORDER BY b."Name" DESC, b."Id"
LIMIT @p
```

## Query 2

```sql
-- @supplierIds={ '2', '1', '3' } (DbType = Object)
SELECT s."Name", s."Website", s."Id"
FROM "Suppliers" AS s
WHERE s."Id" = ANY (@supplierIds)
```
