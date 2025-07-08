# Paging_NullableReference_Ascending_Cursor_Value_NonNull

## Result 1

```json
[
  {
    "Id": "68a5c7c2-1234-4def-bc01-9f1a23456789",
    "Date": "2017-10-28",
    "Time": "22:00:00",
    "String": "22:00:00"
  },
  {
    "Id": "dd8f3a21-89ab-4cde-a203-7d3c45678901",
    "Date": "2017-11-03",
    "Time": "21:45:00",
    "String": "21:45:00"
  }
]
```

## Result 2

```json
[
  {
    "Id": "d3b7e9f1-4567-4abc-a102-8c2b34567890",
    "Date": "2017-11-03",
    "Time": null,
    "String": null
  },
  {
    "Id": "62ce9d54-2345-4f01-b304-6e4d56789012",
    "Date": "2017-11-04",
    "Time": "14:00:00",
    "String": "14:00:00"
  }
]
```

## SQL 0

```sql
-- @__p_0='3'
SELECT r."Id", r."Date", r."String", r."Time"
FROM "Records" AS r
ORDER BY r."Date", r."String", r."Id"
LIMIT @__p_0
```

## SQL 1

```sql
-- @__value_0='11/03/2017' (DbType = Date)
-- @__value_1='21:45:00'
-- @__value_2='dd8f3a21-89ab-4cde-a203-7d3c45678901'
-- @__p_3='3'
SELECT r."Id", r."Date", r."String", r."Time"
FROM "Records" AS r
WHERE (r."Date" > @__value_0 OR r."Date" IS NULL) OR (r."Date" = @__value_0 AND (r."String" > @__value_1 OR r."String" IS NULL)) OR (r."Date" = @__value_0 AND r."String" = @__value_1 AND r."Id" > @__value_2)
ORDER BY r."Date", r."String", r."Id"
LIMIT @__p_3
```

