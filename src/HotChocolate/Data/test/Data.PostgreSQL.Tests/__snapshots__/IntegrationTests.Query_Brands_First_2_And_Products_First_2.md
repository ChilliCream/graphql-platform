# Query_Brands_First_2_And_Products_First_2

## Result

```json
{
  "data": {
    "brands": {
      "nodes": [
        {
          "id": "QnJhbmQ6MTE=",
          "name": "Zephyr",
          "products": {
            "nodes": [
              {
                "id": "UHJvZHVjdDoxMg==",
                "name": "Powder Pro Snowboard"
              },
              {
                "id": "UHJvZHVjdDoyMw==",
                "name": "Summit Pro Climbing Harness"
              }
            ]
          }
        },
        {
          "id": "QnJhbmQ6MTM=",
          "name": "XE",
          "products": {
            "nodes": [
              {
                "id": "UHJvZHVjdDo3Nw==",
                "name": "Survivor 2-Person Tent"
              },
              {
                "id": "UHJvZHVjdDo4MA==",
                "name": "Pathfinder GPS Watch"
              }
            ]
          }
        }
      ]
    }
  }
}
```

## Query 1

```sql
-- @__p_0='3'
SELECT b."Id", b."Name"
FROM "Brands" AS b
ORDER BY b."Name" DESC, b."Id"
LIMIT @__p_0
```

## Query 2

```sql
-- @__brandIds_0={ '11', '13' } (DbType = Object)
SELECT p1."BrandId", p3."Id", p3."Name", p3."BrandId"
FROM (
    SELECT p."BrandId"
    FROM "Products" AS p
    WHERE p."BrandId" = ANY (@__brandIds_0)
    GROUP BY p."BrandId"
) AS p1
LEFT JOIN (
    SELECT p2."Id", p2."Name", p2."BrandId"
    FROM (
        SELECT p0."Id", p0."Name", p0."BrandId", ROW_NUMBER() OVER(PARTITION BY p0."BrandId" ORDER BY p0."Id") AS row
        FROM "Products" AS p0
        WHERE p0."BrandId" = ANY (@__brandIds_0)
    ) AS p2
    WHERE p2.row <= 3
) AS p3 ON p1."BrandId" = p3."BrandId"
ORDER BY p1."BrandId"
```

