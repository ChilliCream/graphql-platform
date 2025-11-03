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
-- @p='2'
SELECT b."Id", b."Name"
FROM "Brands" AS b
ORDER BY b."Name" DESC, b."Id"
LIMIT @p
```

## Query 2

```sql
-- @brandIds={ '11' } (DbType = Object)
SELECT p."BrandId" AS "Key", count(*)::int AS "Count"
FROM "Products" AS p
WHERE p."BrandId" = ANY (@brandIds)
GROUP BY p."BrandId"
```

## Query 3

```sql
-- @brandIds={ '11' } (DbType = Object)
SELECT p1."BrandId", p3."Name", p3."Id", p3."BrandId"
FROM (
    SELECT p."BrandId"
    FROM "Products" AS p
    WHERE p."BrandId" = ANY (@brandIds)
    GROUP BY p."BrandId"
) AS p1
LEFT JOIN (
    SELECT p2."Name", p2."Id", p2."BrandId"
    FROM (
        SELECT p0."Name", p0."Id", p0."BrandId", ROW_NUMBER() OVER(PARTITION BY p0."BrandId" ORDER BY p0."Id") AS row
        FROM "Products" AS p0
        WHERE p0."BrandId" = ANY (@brandIds)
    ) AS p2
    WHERE p2.row <= 3
) AS p3 ON p1."BrandId" = p3."BrandId"
ORDER BY p1."BrandId", p3."BrandId", p3."Id"
```

