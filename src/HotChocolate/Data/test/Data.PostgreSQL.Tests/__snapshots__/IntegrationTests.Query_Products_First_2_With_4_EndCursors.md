# Query_Products_First_2_With_4_EndCursors

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
      ],
      "endCursors": [
        "ezB8MXwxMDF9WmVuaXRoIEN5Y2xpbmcgSmVyc2V5OjQ2",
        "ezF8MXwxMDF9WmVuaXRoIEN5Y2xpbmcgSmVyc2V5OjQ2",
        "ezJ8MXwxMDF9WmVuaXRoIEN5Y2xpbmcgSmVyc2V5OjQ2",
        "ezN8MXwxMDF9WmVuaXRoIEN5Y2xpbmcgSmVyc2V5OjQ2"
      ]
    }
  }
}
```

## Query 1

```sql
-- @__p_0='3'
SELECT (
    SELECT count(*)::int
    FROM "Products" AS p0) AS "TotalCount", p."Name", p."BrandId", p."Id"
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

