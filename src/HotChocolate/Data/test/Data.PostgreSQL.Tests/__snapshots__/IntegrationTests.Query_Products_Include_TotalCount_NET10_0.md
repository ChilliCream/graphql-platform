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
-- @p='3'
SELECT (
    SELECT count(*)::int
    FROM "Products" AS p0) AS "TotalCount", p."Name", p."Id"
FROM "Products" AS p
ORDER BY p."Name" DESC, p."Id"
LIMIT @p
```

