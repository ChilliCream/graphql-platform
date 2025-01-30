# Query_Brands_First_2_And_Products_First_2_Name_Desc_Brand_Name

## Result

```json
{
  "data": {
    "brands": {
      "nodes": [
        {
          "id": "QnJhbmQ6OQ==",
          "products": {
            "nodes": [
              {
                "id": "UHJvZHVjdDo5",
                "name": "VenturePro GPS Watch",
                "brand": {
                  "name": "AirStrider"
                }
              },
              {
                "id": "UHJvZHVjdDozNA==",
                "name": "Velocity Red Bike Helmet",
                "brand": {
                  "name": "AirStrider"
                }
              }
            ]
          }
        },
        {
          "id": "QnJhbmQ6NQ==",
          "products": {
            "nodes": [
              {
                "id": "UHJvZHVjdDo1",
                "name": "Blizzard Rider Snowboard",
                "brand": {
                  "name": "B&R"
                }
              },
              {
                "id": "UHJvZHVjdDoyMA==",
                "name": "Explorer Biking Computer",
                "brand": {
                  "name": "B&R"
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
-- @__p_0='3'
SELECT b."Id", b."Name"
FROM "Brands" AS b
ORDER BY b."Name", b."Id"
LIMIT @__p_0
```

## Query 2

```sql
-- @__brandIds_0={ '5', '9' } (DbType = Object)
SELECT t."BrandId", t0."Id", t0."Name", t0."BrandId"
FROM (
    SELECT p."BrandId"
    FROM "Products" AS p
    WHERE p."BrandId" = ANY (@__brandIds_0)
    GROUP BY p."BrandId"
) AS t
LEFT JOIN (
    SELECT t1."Id", t1."Name", t1."BrandId"
    FROM (
        SELECT p0."Id", p0."Name", p0."BrandId", ROW_NUMBER() OVER(PARTITION BY p0."BrandId" ORDER BY p0."Id") AS row
        FROM "Products" AS p0
        WHERE p0."BrandId" = ANY (@__brandIds_0)
    ) AS t1
    WHERE t1.row <= 3
) AS t0 ON t."BrandId" = t0."BrandId"
ORDER BY t."BrandId"
```

## Query 3

```sql
-- @__ids_0={ '5', '9' } (DbType = Object)
SELECT b."Id", b."Name"
FROM "Brands" AS b
WHERE b."Id" = ANY (@__ids_0)
```

