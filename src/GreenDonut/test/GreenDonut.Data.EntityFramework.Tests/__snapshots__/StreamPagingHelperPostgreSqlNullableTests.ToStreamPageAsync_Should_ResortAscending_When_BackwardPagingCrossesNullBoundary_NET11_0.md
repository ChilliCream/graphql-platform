# ToStreamPageAsync_Should_ResortAscending_When_BackwardPagingCrossesNullBoundary

## SQL 0

```sql
-- @p='5'
SELECT r."Id", r."Date", r."String", r."Time"
FROM "Records" AS r
ORDER BY r."Date", r."Time", r."Id"
LIMIT @p
```

## SQL 1

```sql
-- @value='11/04/2017' (DbType = Date)
-- @value2='14:00' (DbType = Time)
-- @value5='62ce9d54-2345-4f01-b304-6e4d56789012'
-- @p='2'
SELECT r0."Id", r0."Date", r0."String", r0."Time", EXISTS (
    SELECT 1
    FROM "Records" AS r1
    WHERE r1."Date" < @value OR (r1."Date" IS NOT NULL AND r1."Date" = @value AND r1."Time" < @value2) OR (r1."Date" IS NOT NULL AND r1."Date" = @value AND r1."Time" IS NOT NULL AND r1."Time" = @value2 AND r1."Id" < @value5)
    ORDER BY r1."Date" DESC, r1."Time" DESC, r1."Id" DESC
    OFFSET 2) AS "HasMore"
FROM (
    SELECT r."Id", r."Date", r."String", r."Time"
    FROM "Records" AS r
    WHERE r."Date" < @value OR (r."Date" IS NOT NULL AND r."Date" = @value AND r."Time" < @value2) OR (r."Date" IS NOT NULL AND r."Date" = @value AND r."Time" IS NOT NULL AND r."Time" = @value2 AND r."Id" < @value5)
    ORDER BY r."Date" DESC, r."Time" DESC, r."Id" DESC
    LIMIT @p
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
    },
    {
      "Id": "d3b7e9f1-4567-4abc-a102-8c2b34567890",
      "Date": "2017-11-03",
      "Time": null,
      "String": null
    }
  ],
  "Cursors": [
    "e30yMDE3MTEwMzoyMTQ1MDAwMDAwMDAwOmRkOGYzYTIxLTg5YWItNGNkZS1hMjAzLTdkM2M0NTY3ODkwMQ==",
    "e30yMDE3MTEwMzpcbnVsbDpkM2I3ZTlmMS00NTY3LTRhYmMtYTEwMi04YzJiMzQ1Njc4OTA="
  ]
}
```
