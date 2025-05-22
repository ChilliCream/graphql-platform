# Query_Products_Exclude_TotalCount

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
      ]
    }
  }
}
```

## Query 1

```sql
-- @p='3'
SELECT p."Name", p."Id"
FROM "Products" AS p
ORDER BY p."Name" DESC, p."Id"
LIMIT @p
```

