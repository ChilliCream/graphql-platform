# Query_Products_First_2_And_Brand

## Result

```json
{
  "data": {
    "products": {
      "nodes": [
        {
          "name": "Zero Gravity Ski Goggles",
          "brand": {
            "name": "Gravitator"
          }
        },
        {
          "name": "Zenith Cycling Jersey",
          "brand": {
            "name": "B&R"
          }
        }
      ]
    }
  }
}
```

## Query 1

```sql
-- @p='3'
SELECT p."Name", p."BrandId", p."Id"
FROM "Products" AS p
ORDER BY p."Name" DESC, p."Id"
LIMIT @p
```

## Query 2

```sql
-- @ids={ '2', '5' } (DbType = Object)
SELECT b."Id", b."Name"
FROM "Brands" AS b
WHERE b."Id" = ANY (@ids)
```

