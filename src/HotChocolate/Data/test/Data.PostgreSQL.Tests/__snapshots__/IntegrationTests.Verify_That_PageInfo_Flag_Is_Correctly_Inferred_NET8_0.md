# Verify_That_PageInfo_Flag_Is_Correctly_Inferred

## Result

```json
{
  "data": {
    "brands": {
      "nodes": [
        {
          "products": {
            "pageInfo": {
              "endCursor": null
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
SELECT b."Id", b."Name"
FROM "Brands" AS b
ORDER BY b."Name" DESC, b."Id"
LIMIT @__p_0
```

