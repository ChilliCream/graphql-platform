# ToStreamPageAsync_Should_ResortAscending_When_BackwardPagingStartsFromNullCursor

## SQL 0

```sql
-- @__p_0='4'
SELECT r."Id", r."Date", r."String", r."Time"
FROM "Records" AS r
ORDER BY r."Date", r."Time", r."Id"
LIMIT @__p_0
```

## SQL 1

```sql
-- @__value_0='11/03/2017' (DbType = Date)
-- @__value_1='d3b7e9f1-4567-4abc-a102-8c2b34567890'
-- @__p_2='1'
SELECT r0."Id", r0."Date", r0."String", r0."Time", EXISTS (
    SELECT 1
    FROM "Records" AS r1
    WHERE r1."Date" < @__value_0 OR (r1."Date" IS NOT NULL AND r1."Date" = @__value_0 AND r1."Time" IS NOT NULL) OR (r1."Date" IS NOT NULL AND r1."Date" = @__value_0 AND r1."Time" IS NULL AND r1."Id" < @__value_1)
    ORDER BY r1."Date" DESC, r1."Time" DESC, r1."Id" DESC
    OFFSET 1) AS "HasMore"
FROM (
    SELECT r."Id", r."Date", r."String", r."Time"
    FROM "Records" AS r
    WHERE r."Date" < @__value_0 OR (r."Date" IS NOT NULL AND r."Date" = @__value_0 AND r."Time" IS NOT NULL) OR (r."Date" IS NOT NULL AND r."Date" = @__value_0 AND r."Time" IS NULL AND r."Id" < @__value_1)
    ORDER BY r."Date" DESC, r."Time" DESC, r."Id" DESC
    LIMIT @__p_2
) AS r0
ORDER BY r0."Date", r0."Time", r0."Id"
```

## Result 3

```json
{
  "Index": null,
  "TotalCount": null,
  "HasNextPage": true,
  "HasPreviousPage": true,
  "Items": [
    {
      "Id": "dd8f3a21-89ab-4cde-a203-7d3c45678901",
      "Date": "2017-11-03",
      "Time": "21:45:00",
      "String": "21:45:00"
    }
  ],
  "Cursors": [
    "e30yMDE3MTEwMzoyMTQ1MDAwMDAwMDAwOmRkOGYzYTIxLTg5YWItNGNkZS1hMjAzLTdkM2M0NTY3ODkwMQ=="
  ]
}
```
