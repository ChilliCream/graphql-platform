# Project_Into_1to1_Relation

## Result

```json
{
  "data": {
    "products": {
      "nodes": [
        {
          "name": "Zero Gravity Ski Goggles",
          "brandName": "Gravitator"
        },
        {
          "name": "Zenith Cycling Jersey",
          "brandName": "B&R"
        }
      ]
    }
  }
}
```

## Query 1

```sql
-- @p='3'
SELECT FALSE, b."Name", p0."Name", p0."Id"
FROM (
    SELECT p."Id", p."BrandId", p."Name"
    FROM "Products" AS p
    ORDER BY p."Name" DESC, p."Id"
    LIMIT @p
) AS p0
INNER JOIN "Brands" AS b ON p0."BrandId" = b."Id"
ORDER BY p0."Name" DESC, p0."Id"
```

