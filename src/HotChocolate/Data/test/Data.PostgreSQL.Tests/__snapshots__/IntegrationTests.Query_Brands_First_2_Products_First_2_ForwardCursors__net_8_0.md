# Query_Brands_First_2_Products_First_2_ForwardCursors

## Result

```json
{
  "data": {
    "brands": {
      "nodes": [
        {
          "name": "Zephyr",
          "products": {
            "nodes": [
              {
                "name": "Powder Pro Snowboard"
              },
              {
                "name": "Summit Pro Climbing Harness"
              }
            ],
            "pageInfo": {
              "forwardCursors": [
                {
                  "page": 2,
                  "cursor": "ezB8MXw2fTIz"
                },
                {
                  "page": 3,
                  "cursor": "ezF8MXw2fTIz"
                }
              ]
            }
          }
        }
      ]
    }
  }
}
```

## Query 1

```sql
-- @__p_0='2'
SELECT b."Name", b."Id"
FROM "Brands" AS b
ORDER BY b."Name" DESC, b."Id"
LIMIT @__p_0
```

## Query 2

```sql
-- @__brandIds_0={ '11' } (DbType = Object)
SELECT p."BrandId" AS "Key", count(*)::int AS "Count"
FROM "Products" AS p
WHERE p."BrandId" = ANY (@__brandIds_0)
GROUP BY p."BrandId"
```

## Query 3

```sql
-- @__brandIds_0={ '11' } (DbType = Object)
SELECT t."BrandId", t0."Name", t0."Id", t0."BrandId"
FROM (
    SELECT p."BrandId"
    FROM "Products" AS p
    WHERE p."BrandId" = ANY (@__brandIds_0)
    GROUP BY p."BrandId"
) AS t
LEFT JOIN (
    SELECT t1."Name", t1."Id", t1."BrandId"
    FROM (
        SELECT p0."Name", p0."Id", p0."BrandId", ROW_NUMBER() OVER(PARTITION BY p0."BrandId" ORDER BY p0."Id") AS row
        FROM "Products" AS p0
        WHERE p0."BrandId" = ANY (@__brandIds_0)
    ) AS t1
    WHERE t1.row <= 3
) AS t0 ON t."BrandId" = t0."BrandId"
ORDER BY t."BrandId", t0."BrandId", t0."Id"
```

