# Query_Products_Include_TotalCount

## Result

```json
{
  "data": {
    "productsNonRelative": {
      "nodes": [
        {
          "name": "Zero Gravity Ski Goggles"
        },
        {
          "name": "Zenith Cycling Jersey"
        }
      ],
      "totalCount": 101
    }
  }
}
```

## Query 1

```sql
-- @__Count_1='101'
-- @__p_0='3'
SELECT @__Count_1 AS "TotalCount", p."Name", p."Id"
FROM "Products" AS p
ORDER BY p."Name" DESC, p."Id"
LIMIT @__p_0
```

