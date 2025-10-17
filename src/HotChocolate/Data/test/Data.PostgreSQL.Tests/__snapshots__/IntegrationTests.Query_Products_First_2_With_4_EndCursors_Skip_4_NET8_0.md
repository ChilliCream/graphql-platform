# Query_Products_First_2_With_4_EndCursors_Skip_4

## Result

```json
{
  "data": {
    "products": {
      "nodes": [
        {
          "name": "Venture 2022 Snowboard",
          "brand": {
            "name": "Raptor Elite"
          }
        },
        {
          "name": "VelociX 2000 Bike Helmet",
          "brand": {
            "name": "Raptor Elite"
          }
        }
      ],
      "endCursors": [
        "ezB8NHwxMDF9VmVsb2NpWCAyMDAwIEJpa2UgSGVsbWV0OjU4",
        "ezF8NHwxMDF9VmVsb2NpWCAyMDAwIEJpa2UgSGVsbWV0OjU4",
        "ezJ8NHwxMDF9VmVsb2NpWCAyMDAwIEJpa2UgSGVsbWV0OjU4",
        "ezN8NHwxMDF9VmVsb2NpWCAyMDAwIEJpa2UgSGVsbWV0OjU4"
      ]
    }
  }
}
```

## Query 1

```sql
-- @__value_0='Zenith Cycling Jersey'
-- @__value_1='46'
-- @__p_3='3'
-- @__p_2='6'
SELECT p."BrandId", p."Name", p."Id"
FROM "Products" AS p
WHERE p."Name" < @__value_0 OR (p."Name" = @__value_0 AND p."Id" > @__value_1)
ORDER BY p."Name" DESC, p."Id"
LIMIT @__p_3 OFFSET @__p_2
```

## Query 2

```sql
-- @__ids_0={ '6' } (DbType = Object)
SELECT b."Id", b."Name"
FROM "Brands" AS b
WHERE b."Id" = ANY (@__ids_0)
```

