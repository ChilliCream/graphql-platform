# Query_Brands_First_2_And_Products_First_2_Name_Desc_Brand_Name

## Result

```json
{
  "data": {
    "brands": {
      "nodes": [
        {
          "id": "QnJhbmQ6MTE=",
          "products": {
            "nodes": [
              {
                "id": "UHJvZHVjdDo0OA==",
                "name": "Trailblazer 45L Backpack",
                "brand": {
                  "name": "Zephyr"
                }
              },
              {
                "id": "UHJvZHVjdDoyMw==",
                "name": "Summit Pro Climbing Harness",
                "brand": {
                  "name": "Zephyr"
                }
              }
            ]
          }
        },
        {
          "id": "QnJhbmQ6MTM=",
          "products": {
            "nodes": [
              {
                "id": "UHJvZHVjdDo3Nw==",
                "name": "Survivor 2-Person Tent",
                "brand": {
                  "name": "XE"
                }
              },
              {
                "id": "UHJvZHVjdDo4MA==",
                "name": "Pathfinder GPS Watch",
                "brand": {
                  "name": "XE"
                }
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
-- @p='3'
SELECT b."Id", b."Name"
FROM "Brands" AS b
ORDER BY b."Name" DESC, b."Id"
LIMIT @p
```

## Query 2

```sql
-- @brandIds={ '11', '13' } (DbType = Object)
SELECT p1."BrandId", p3."BrandId", p3."Id", p3."Name"
FROM (
    SELECT p."BrandId"
    FROM "Products" AS p
    WHERE p."BrandId" = ANY (@brandIds)
    GROUP BY p."BrandId"
) AS p1
LEFT JOIN (
    SELECT p2."BrandId", p2."Id", p2."Name"
    FROM (
        SELECT p0."BrandId", p0."Id", p0."Name", ROW_NUMBER() OVER(PARTITION BY p0."BrandId" ORDER BY p0."Name" DESC, p0."Id") AS row
        FROM "Products" AS p0
        WHERE p0."BrandId" = ANY (@brandIds)
    ) AS p2
    WHERE p2.row <= 3
) AS p3 ON p1."BrandId" = p3."BrandId"
ORDER BY p1."BrandId", p3."BrandId", p3."Name" DESC, p3."Id"
```

## Query 3

```sql
-- @ids={ '11', '13' } (DbType = Object)
SELECT b."Id", b."Name"
FROM "Brands" AS b
WHERE b."Id" = ANY (@ids)
```

