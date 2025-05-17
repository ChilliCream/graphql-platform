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
-- @value='Zenith Cycling Jersey'
-- @value1='46'
-- @p0='3'
-- @p='6'
SELECT p."Name", p."BrandId", p."Id"
FROM "Products" AS p
WHERE p."Name" < @value OR (p."Name" = @value AND p."Id" > @value1)
ORDER BY p."Name" DESC, p."Id"
LIMIT @p0 OFFSET @p
```

## Query 2

```sql
-- @ids={ '6' } (DbType = Object)
SELECT b."Id", b."Name"
FROM "Brands" AS b
WHERE b."Id" = ANY (@ids)
```

