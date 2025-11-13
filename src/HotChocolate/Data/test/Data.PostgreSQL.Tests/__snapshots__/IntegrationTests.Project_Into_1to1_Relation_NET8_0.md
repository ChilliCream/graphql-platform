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
-- @__p_0='3'
SELECT FALSE, b."Name", t."Name", t."Id"
FROM (
    SELECT p."Id", p."BrandId", p."Name"
    FROM "Products" AS p
    ORDER BY p."Name" DESC, p."Id"
    LIMIT @__p_0
) AS t
INNER JOIN "Brands" AS b ON t."BrandId" = b."Id"
ORDER BY t."Name" DESC, t."Id"
```

