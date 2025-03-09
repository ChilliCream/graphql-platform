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
-- @__Count_1='101'
-- @__p_0='3'
SELECT @__Count_1 AS "TotalCount", p."Name", p."BrandId", p."Id"
FROM "Products" AS p
ORDER BY p."Name" DESC, p."Id"
LIMIT @__p_0
```

## Query 2

```sql
-- @__ids_0={ '2', '5' } (DbType = Object)
SELECT b."Id", b."Name"
FROM "Brands" AS b
WHERE b."Id" = ANY (@__ids_0)
```

